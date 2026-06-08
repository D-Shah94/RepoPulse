namespace RepoPulse.API.Services
{
    /// <summary>
    /// Defines the contract for interacting with the GitHub REST API.
    /// Abstracting this behind an interface is fundamental for testability.
    /// </summary>
    public interface IGitHubService
    {
        /// <summary>
        /// Fetches the raw text content of a specific file from a GitHub repository.
        /// Responses are cached using a TTL strategy to respect GitHub's API rate limits.
        /// </summary>
        Task<string?> GetFileContentsAsync(string owner, string repo, string filePath);


        /// <summary>
        /// Fetches the root-level directory listing for a GitHub repository.
        /// Used to discover which dependency manifest files are present.
        /// </summary>
        Task<IReadOnlyList<string>> GetRepositoryRootFilesAsync(string owner, string repo);


        /// <summary>
        /// Fetches the recursive tree for a GitHub repository.
        /// Used to discover which dependency manifest files are present.
        /// </summary>
        Task<IReadOnlyList<string>> GetRepositoryFilesRecursiveAsync(string owner, string repo);

        /// <summary>
        /// Checks whether the GitHub API is currently reachable and returns
        /// the remaining rate limit for unauthenticated requests.
        /// </summary>
        Task<GitHubHealthStatus> GetApiHealthStatusAsync();
    }

    /// <summary>
    /// A simple immutable value object returned by the health check operation.
    /// </summary>
    public record GitHubHealthStatus(
        bool IsReachable,
        int RateLimitRemaining,
        int RateLimitTotal,
        string Detail
    );
}
