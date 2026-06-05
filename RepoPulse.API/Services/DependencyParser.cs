using RepoPulse.API.DTOs;
using System.Text.Json;
using System.Xml.Linq;

namespace RepoPulse.API.Services;

public class DependencyParser
{
    private readonly IGitHubService _gitHubService;
    private readonly ILogger<DependencyParser> _logger;

    private static readonly string[] KnownManifestFiles = ["package.json", "requirements.txt"];

    public DependencyParser(IGitHubService gitHubService, ILogger<DependencyParser> logger)
    {
        _gitHubService = gitHubService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ParsedManifest>> ParseRepositoryAsync(string owner, string repo)
    {
        var results = new List<ParsedManifest>();
        var rootFiles = await _gitHubService.GetRepositoryRootFilesAsync(owner, repo);

        var csprojFiles = rootFiles.Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)).ToList();
        var manifestsToAttempt = KnownManifestFiles.Concat(csprojFiles).ToList();

        foreach (var manifestFile in manifestsToAttempt)
        {
            if (!rootFiles.Contains(manifestFile, StringComparer.OrdinalIgnoreCase)) continue;

            _logger.LogInformation("Parsing manifest {ManifestFile} for {Owner}/{Repo}", manifestFile, owner, repo);

            var content = await _gitHubService.GetFileContentsAsync(owner, repo, manifestFile);
            if (string.IsNullOrWhiteSpace(content)) continue;

            var dependencies = ParseManifest(manifestFile, content);

            if (dependencies.Count > 0)
            {
                results.Add(new ParsedManifest(manifestFile, dependencies));
            }
        }

        return results.AsReadOnly();
    }

    public IReadOnlyList<ParsedDependencyDto> ParseManifest(string fileName, string content)
    {
        try
        {
            return fileName.ToLowerInvariant() switch
            {
                "package.json" => ParsePackageJson(content),
                "requirements.txt" => ParseRequirementsTxt(content),
                _ when fileName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) => ParseCsproj(content),
                _ => []
            };
        }
        catch (Exception ex)
        {
            // Catch Block Answer: A generic catch here covers all parsing failures safely
            _logger.LogWarning(ex, "Failed to parse manifest file {FileName}. The file may be malformed.", fileName);
            return [];
        }
    }

    private IReadOnlyList<ParsedDependencyDto> ParsePackageJson(string content)
    {
        var entries = new List<ParsedDependencyDto>();
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            foreach (var sectionName in new[] { "dependencies", "devDependencies" })
            {
                if (!root.TryGetProperty(sectionName, out var section)) continue;

                foreach (var dependency in section.EnumerateObject())
                {
                    entries.Add(new ParsedDependencyDto(dependency.Name, dependency.Value.GetString() ?? "unknown", "npm"));
                }
            }
        }
        catch (JsonException ex)
        {
            // Catch Block Answer: Log specific JSON errors
            _logger.LogWarning(ex, "Invalid JSON structure in package.json");
        }
        return entries.AsReadOnly();
    }

    private IReadOnlyList<ParsedDependencyDto> ParseRequirementsTxt(string content)
    {
        var entries = new List<ParsedDependencyDto>();

        // Python parsing is just string manipulation, so a general try/catch in the parent method protects it
        foreach (var rawLine in content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Split('#')[0].Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('-')) continue;

            var operators = new[] { "==", ">=", "<=", "~=", "!=" };
            string packageName = line;
            string version = "unspecified";

            foreach (var op in operators)
            {
                var idx = line.IndexOf(op, StringComparison.Ordinal);
                if (idx <= 0) continue;

                packageName = line[..idx].Trim();
                version = line[idx..].Trim();
                break;
            }

            entries.Add(new ParsedDependencyDto(packageName, version, "pip"));
        }
        return entries.AsReadOnly();
    }

    private IReadOnlyList<ParsedDependencyDto> ParseCsproj(string content)
    {
        var entries = new List<ParsedDependencyDto>();
        try
        {
            var xdoc = XDocument.Parse(content);
            var packageRefs = xdoc.Descendants("PackageReference");

            foreach (var packageRef in packageRefs)
            {
                var name = packageRef.Attribute("Include")?.Value;
                var version = packageRef.Attribute("Version")?.Value ?? packageRef.Element("Version")?.Value ?? "unspecified";

                if (string.IsNullOrWhiteSpace(name)) continue;
                entries.Add(new ParsedDependencyDto(name, version, "NuGet"));
            }
        }
        catch (System.Xml.XmlException ex)
        {
            // Catch Block Answer: Log specific XML errors
            _logger.LogWarning(ex, "Invalid XML structure in .csproj file");
        }
        return entries.AsReadOnly();
    }
}