using System.ComponentModel.DataAnnotations;

namespace RepoPulse.Client.Models;

public sealed record RepositoryDto(
    int Id,
    string Owner,
    string RepoName,
    string? Description,
    DateTime? LastFetchedAt,
    DateTime CreatedAt,
    string FullName,
    DependencyFetchResultDto? LatestSnapshot
);

public class CreateRepositoryDto
{
    [Required(ErrorMessage = "Repository owner is required.")]
    public string Owner { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repository name is required.")]
    public string RepoName { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public sealed record DependencyFetchResultDto(
    int RepositoryId,
    string Owner,
    string RepoName,
    DateTime FetchedAt,
    IReadOnlyList<ManifestGroupDto> Manifests
);

public sealed record ManifestGroupDto(
    string ManifestFile,
    int DependencyCount,
    IReadOnlyList<DependencyEntryDto> Dependencies
);

public sealed record DependencyEntryDto(
    int Id,
    string PackageName,
    string Version,
    string PackageType
);

public record GitHubHealthStatus(
    bool IsReachable,
    int RateLimitRemaining,
    int RateLimitTotal,
    string Detail
);
