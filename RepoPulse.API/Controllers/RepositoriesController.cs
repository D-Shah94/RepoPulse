using Microsoft.AspNetCore.Mvc;
using RepoPulse.API.DTOs;
using RepoPulse.API.Services;

namespace RepoPulse.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RepositoriesController : ControllerBase
{
    private readonly IRepositoryService _repositoryService;
    private readonly ILogger<RepositoriesController> _logger;

    public RepositoriesController(IRepositoryService repositoryService, ILogger<RepositoriesController> logger)
    {
        _repositoryService = repositoryService;
        _logger = logger;
    }

    // GET: api/repositories
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RepositoryDto>>> GetAll()
    {
        var repos = await _repositoryService.GetAllAsync();
        return Ok(repos); // Returns 200 OK
    }

    // GET: api/repositories/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RepositoryDto>> GetById(int id)
    {
        var repo = await _repositoryService.GetByIdAsync(id);

        if (repo == null)
        {
            return NotFound($"Repository with ID {id} not found."); // Returns 404
        }

        return Ok(repo);
    }

    // POST: api/repositories
    [HttpPost]
    public async Task<ActionResult<RepositoryDto>> Create([FromBody] CreateRepositoryDto dto)
    {
        var createdRepo = await _repositoryService.CreateAsync(dto);

        // Returns a 201 Created status code, along with a Location header pointing to the new resource
        return CreatedAtAction(nameof(GetById), new { id = createdRepo.Id }, createdRepo);
    }

    // DELETE: api/repositories/5
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var deleted = await _repositoryService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound($"Repository with ID {id} not found.");
        }

        return NoContent(); // Standard 204 response for a successful deletion with no return data
    }

    // POST: api/repositories/5/fetch
    [HttpPost("{id:int}/fetch")]
    public async Task<ActionResult<DependencyFetchResultDto>> FetchDependencies(int id)
    {
        _logger.LogInformation("Manual dependency fetch triggered for repository ID {Id}", id);

        var result = await _repositoryService.FetchDependenciesAsync(id);

        if (result == null)
        {
            return NotFound($"Repository with ID {id} not found.");
        }

        if (!result.Manifests.Any())
        {
            _logger.LogInformation("Fetch completed for repo {Id} but no supported manifests were found.", id);
        }

        return Ok(result);
    }
}
