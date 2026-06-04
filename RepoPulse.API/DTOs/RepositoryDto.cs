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
    string Owner,
    string RepoName,
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