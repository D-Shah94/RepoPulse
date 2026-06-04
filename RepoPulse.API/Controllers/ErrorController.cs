using Microsoft.AspNetCore.Mvc;

namespace RepoPulse.API.Controllers;

// We use this attribute to hide this controller from Swagger.
// It is an internal operational route, not a public API endpoint.
[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorController : ControllerBase
{
    [Route("/error")]
    public IActionResult HandleError()
    {
        // This returns a standardized RFC 7807 Problem Details JSON response.
        // It safely tells the client something went wrong without leaking 
        // database table names, file paths, or line numbers to potential hackers.
        return Problem(
            title: "An unexpected error occurred processing your request.",
            detail: "Please check the server logs for more information.",
            statusCode: StatusCodes.Status500InternalServerError
        );
    }
}