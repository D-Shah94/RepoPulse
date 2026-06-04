using Microsoft.EntityFrameworkCore;
using RepoPulse.API.Data;
using RepoPulse.API.DTOs;
using RepoPulse.API.Models;

namespace RepoPulse.API.Services;

/// <summary>
/// Handles all persistence operations for tracked repositories
/// and orchestrates dependency fetching via DependencyParser.
/// </summary>
public sealed class RepositoryService : IRepositoryService
{
    private readonly AppDbContext _db;
    private readonly DependencyParser _parser;
    private readonly ILogger<RepositoryService> _logger;

    public RepositoryService(AppDbContext db, DependencyParser parser, ILogger<RepositoryService> logger)
    {
        _db = db;
        _parser = parser;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ApiDtos>> GetAllAsync()
    {
        var repos = await _db.TrackedRepositories
            .AsNoTracking()
            .OrderBy(r => r.Owner)
            .ThenBy(r => r.RepoName)
            .ToListAsync();

        return repos.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task<ApiDtos?> GetByIdAsync(int id)
    {
        var repo = await _db.TrackedRepositories
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        return repo is null ? null : MapToDto(repo);
    }

    public async Task<ApiDtos> CreateAsync(CreateRepositoryDto dto)
    {
        var repo = new TrackedRepository
        {
            Owner = dto.Owner.Trim().ToLowerInvariant(),
            RepoName = dto.RepoName.Trim().ToLowerInvariant(),
            Description = dto.Description?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.TrackedRepositories.Add(repo);
        await _db.SaveChangesAsync();

        return MapToDto(repo);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var repo = await _db.TrackedRepositories.FindAsync(id);
        if (repo is null) return false;

        _db.TrackedRepositories.Remove(repo);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<DependencyFetchResultDto?> FetchDependenciesAsync(int repositoryId)
    {
        var repo = await _db.TrackedRepositories.FindAsync(repositoryId);
        if (repo is null)
        {
            _logger.LogWarning("FetchDependencies called for non-existent repository ID {Id}", repositoryId);
            return null;
        }

        // Use the internal parser which returns internal DTOs (ParsedManifest)
        var parsedManifests = await _parser.ParseRepositoryAsync(repo.Owner, repo.RepoName);

        var fetchedAt = DateTime.UtcNow;
        var manifestGroups = new List<ManifestGroupDto>();

        foreach (var parsed in parsedManifests)
        {
            var snapshot = new DependencySnapshot
            {
                TrackedRepository = repo,
                ManifestFile = parsed.ManifestFile,
                FetchedAt = fetchedAt,
                Entries = new List<DependencyEntry>()
            };

            foreach (var dto in parsed.Dependencies)
            {
                snapshot.Entries.Add(new DependencyEntry
                {
                    PackageName = dto.PackageName,
                    Version = dto.Version,
                    PackageType = dto.PackageType,
                    DependencySnapshot = snapshot
                });
            }

            _db.DependencySnapshots.Add(snapshot);

            // Convert to the external API DTOs (ManifestGroupDto)
            manifestGroups.Add(new ManifestGroupDto(
                ManifestFile: parsed.ManifestFile,
                DependencyCount: parsed.Dependencies.Count,
                Dependencies: parsed.Dependencies.Select(d => new DependencyEntryDto(
                    Id: 0,
                    PackageName: d.PackageName,
                    Version: d.Version,
                    PackageType: d.PackageType
                )).ToList().AsReadOnly()
            ));
        }

        repo.LastFetchedAt = fetchedAt;
        await _db.SaveChangesAsync();

        return new DependencyFetchResultDto(
            RepositoryId: repositoryId,
            Owner: repo.Owner,
            RepoName: repo.RepoName,
            FetchedAt: fetchedAt,
            Manifests: manifestGroups.AsReadOnly()
        );
    }

    private static ApiDtos MapToDto(TrackedRepository repo) =>
        new(
            Id: repo.Id,
            Owner: repo.Owner,
            RepoName: repo.RepoName,
            Description: repo.Description,
            LastFetchedAt: repo.LastFetchedAt,
            CreatedAt: repo.CreatedAt,
            FullName: $"{repo.Owner}/{repo.RepoName}"
        );
}