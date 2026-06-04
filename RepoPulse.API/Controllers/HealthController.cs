using Microsoft.AspNetCore.Mvc;
using RepoPulse.API.Services;

namespace RepoPulse.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IGitHubService _gitHubService;

    public HealthController(IGitHubService gitHubService)
    {
        _gitHubService = gitHubService;
    }

    [HttpGet]
    public async Task<ActionResult<GitHubHealthStatus>> GetHealth()
    {
        var status = await _gitHubService.GetApiHealthStatusAsync();

        if (!status.IsReachable)
        {
            // 503 Service Unavailable is the correct RESTful HTTP code 
            // when an upstream dependency (like GitHub) is down.
            return StatusCode(StatusCodes.Status503ServiceUnavailable, status);
        }

        return Ok(status); // 200 OK
    }
}