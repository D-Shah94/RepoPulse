using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RepoPulse.API.Options;
using System.Text;
using System.Text.Json;

namespace RepoPulse.API.Services
{
    public class GitHubService : IGitHubService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly GitHubApiOptions _options;
        private readonly ILogger<GitHubService> _logger;

        public GitHubService(HttpClient httpClient, IMemoryCache cache, IOptions<GitHubApiOptions> options, ILogger<GitHubService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string?> GetFileContentsAsync(string owner, string repo, string filePath)
        {
            var cacheKey = $"contents_{owner}_{repo}_{filePath}";

            if (_cache.TryGetValue(cacheKey, out string? cachedContent))
            {
                _logger.LogDebug("Cache HIT for {Owner}/{Repo}/{FilePath}. Fetching...", owner, repo, filePath);
                return cachedContent;
            }

            _logger.LogInformation("Cache MISS for {Owner}/{Repo}/{FilePath}. Fetching...", owner, repo, filePath);

            var response = await _httpClient.GetAsync($"repos/{owner}/{repo}/contents/{filePath}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty("content", out var contentElement))
            {
                return null;
            }

            // GitHub Base64 strings often contain line breaks which break the C# decoder
            var base64 = (contentElement.GetString() ?? string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\r", string.Empty);

            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64));

            _cache.Set(
                cacheKey,
                decoded,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheTtlMinutes)
                });

            return decoded;
        }

        public async Task<IReadOnlyList<string>> GetRepositoryRootFilesAsync(string owner, string repo)
        {
            var response = await _httpClient.GetAsync($"repos/{owner}/{repo}/contents");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch root files for {Owner}/{Repo}", owner, repo);
                return Array.Empty<string>();
            }

            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);

            var files = new List<string>();

            // GitHub returns a JSON array of file/folder objects for directory contents
            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (element.TryGetProperty("name", out var nameElement))
                {
                    var fileName = nameElement.GetString();
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        files.Add(fileName);
                    }
                }
            }
            return files;
        }

        public async Task<IReadOnlyList<string>> GetRepositoryFilesRecursiveAsync(string owner, string repo)
        {
            var cacheKey = $"tree_{owner}_{repo}";

            if (_cache.TryGetValue(cacheKey, out IReadOnlyList<string>? cachedTree) && cachedTree != null)
            {
                _logger.LogDebug("Cache HIT for recursive tree {Owner}/{Repo}", owner, repo);
                return cachedTree;
            }

            _logger.LogInformation("Cache MISS for recursive tree {Owner}/{Repo}. Fetching...", owner, repo);

            try
            {
                // 1. Get default branch (main or master)
                var repoResponse = await _httpClient.GetAsync($"repos/{owner}/{repo}");
                if (!repoResponse.IsSuccessStatusCode) return Array.Empty<string>();

                var repoInfo = await repoResponse.Content.ReadFromJsonAsync<JsonElement>();
                if (!repoInfo.TryGetProperty("default_branch", out var branchProp)) return Array.Empty<string>();
                var defaultBranch = branchProp.GetString();

                // 2. Fetch entire tree recursively in ONE request
                var treeResponse = await _httpClient.GetAsync($"repos/{owner}/{repo}/git/trees/{defaultBranch}?recursive=1");
                if (!treeResponse.IsSuccessStatusCode) return Array.Empty<string>();

                var treeData = await treeResponse.Content.ReadFromJsonAsync<JsonElement>();
                var files = new List<string>();

                if (treeData.TryGetProperty("tree", out var treeArray))
                {
                    foreach (var item in treeArray.EnumerateArray())
                    {
                        // We only care about files (blob), not folders (tree)
                        if (item.TryGetProperty("type", out var type) && type.GetString() == "blob")
                        {
                            var path = item.GetProperty("path").GetString();
                            if (!string.IsNullOrEmpty(path)) files.Add(path);
                        }
                    }
                }

                var readOnlyFiles = files.AsReadOnly();

                // Cache the tree to protect the rate limit!
                _cache.Set(cacheKey, readOnlyFiles, TimeSpan.FromMinutes(_options.CacheTtlMinutes));

                return readOnlyFiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch recursive tree for {Owner}/{Repo}", owner, repo);
                return Array.Empty<string>();
            }
        }

        public async Task<GitHubHealthStatus> GetApiHealthStatusAsync()
        {
            try
            {
                // Assuming we just hit the rate_limit endpoint
                var response = await _httpClient.GetAsync("rate_limit");

                var limitTotal = 0;
                var limitRemaining = 0;

                // GitHub sends rate limit data in the response headers
                if (response.Headers.TryGetValues("X-RateLimit-Limit", out var limitValues)
                    && int.TryParse(limitValues.FirstOrDefault(), out var total))
                {
                    limitTotal = total;
                }

                if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues)
                    && int.TryParse(remainingValues.FirstOrDefault(), out var remaining))
                {
                    limitRemaining = remaining;
                }

                return new GitHubHealthStatus(
                    IsReachable: response.IsSuccessStatusCode,
                    RateLimitRemaining: limitRemaining,
                    RateLimitTotal: limitTotal,
                    Detail: response.IsSuccessStatusCode ? "GitHub API is reachable" : $"GitHub API returned {response.StatusCode}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reach GitHub API for health check.");
                return new GitHubHealthStatus(false, 0, 0, "Failed to connect to GitHub API");
            }
        }
    }
}
