using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace RepoPulse.API.Infrastructure
{
    // In .NET 8, implementing IExceptionHandler is the cleanest way to catch all unhandled server errors
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // 1. Log the actual error safely to the server console so you can debug it
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

            // 2. Create a standard ProblemDetails response to send to the frontend
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Server Error",
                Detail = "An unexpected error occurred while processing your request. Our engineering team has been notified.",
                Instance = httpContext.Request.Path
            };

            // 3. Write the JSON response
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            // Return true to tell .NET that we successfully handled the exception
            return true;
        }
    }
}