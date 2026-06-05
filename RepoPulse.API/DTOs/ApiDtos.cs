using System.ComponentModel.DataAnnotations;

namespace RepoPulse.API.DTOs;

public sealed record RepositoryDto(
    int Id,
    string Owner,
    string RepoName,
    string? Description,
    DateTime? LastFetchedAt,
    DateTime CreatedAt,
    string FullName // Computed convenience property
);

public sealed record CreateRepositoryDto(
    [Required(ErrorMessage = "Repository owner is required.")]
    [MaxLength(100, ErrorMessage = "Owner name cannot exceed 100 characters.")]
    string Owner,

    [Required(ErrorMessage = "Repository name is required.")]
    [MaxLength(100, ErrorMessage = "Repository name cannot exceed 100 characters.")]
    string RepoName,

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    string? Description
);

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