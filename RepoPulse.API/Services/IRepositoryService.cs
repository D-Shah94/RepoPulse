using RepoPulse.API.DTOs;

namespace RepoPulse.API.Services;

public interface IRepositoryService
{
    Task<IReadOnlyList<RepositoryDto>> GetAllAsync();
    Task<RepositoryDto?> GetByIdAsync(int id);
    Task<RepositoryDto> CreateAsync(CreateRepositoryDto dto);
    Task<bool> DeleteAsync(int id);
    Task<DependencyFetchResultDto?> FetchDependenciesAsync(int repositoryId);
}