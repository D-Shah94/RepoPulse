using RepoPulse.API.DTOs;

namespace RepoPulse.API.Services;

public interface IRepositoryService
{
    Task<IReadOnlyList<ApiDtos>> GetAllAsync();
    Task<ApiDtos?> GetByIdAsync(int id);
    Task<ApiDtos> CreateAsync(CreateRepositoryDto dto);
    Task<bool> DeleteAsync(int id);
    Task<DependencyFetchResultDto?> FetchDependenciesAsync(int repositoryId);
}