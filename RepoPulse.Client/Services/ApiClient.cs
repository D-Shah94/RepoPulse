using System.Net.Http.Json;
using RepoPulse.Client.Models;

namespace RepoPulse.Client.Services;

/// <summary>
/// A dedicated service for communicating with the RepoPulse backend API.
/// This abstracts raw HTTP calls away from the UI components.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    // ── Health ─────────────────────────────────────────────────────────────
    public async Task<GitHubHealthStatus?> GetHealthAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<GitHubHealthStatus>("api/health");
        }
        catch
        {
            // If the API is completely down, return a degraded state rather than crashing the UI
            return new GitHubHealthStatus(false, 0, 0, "API is unreachable");
        }
    }

    // ── Repositories ───────────────────────────────────────────────────────
    public async Task<IReadOnlyList<RepositoryDto>> GetRepositoriesAsync()
    {
        return await _http.GetFromJsonAsync<IReadOnlyList<RepositoryDto>>("api/repositories")
               ?? Array.Empty<RepositoryDto>();
    }

    public async Task<RepositoryDto?> GetRepositoryByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<RepositoryDto>($"api/repositories/{id}");
    }

    public async Task<RepositoryDto?> CreateRepositoryAsync(CreateRepositoryDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/repositories", dto);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RepositoryDto>();
        }

        return null; // In a production app, we would parse and return the ProblemDetails here
    }

    public async Task<bool> DeleteRepositoryAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/repositories/{id}");
        return response.IsSuccessStatusCode;
    }

    // ── Dependencies ───────────────────────────────────────────────────────
    public async Task<DependencyFetchResultDto?> FetchDependenciesAsync(int repositoryId)
    {
        var response = await _http.PostAsync($"api/repositories/{repositoryId}/fetch", null);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<DependencyFetchResultDto>();
        }

        return null;
    }
}