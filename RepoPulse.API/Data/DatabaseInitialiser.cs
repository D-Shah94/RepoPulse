using Microsoft.EntityFrameworkCore;

namespace RepoPulse.API.Data
{
    /// <summary>
    /// Provides a startup helper to automatically applying pending EF Core migrations.
    /// </summary>
    /// <remarks>
    /// For this local MVP, calling MigrateAsync() on startup is convenient to ensure
    /// the database is always up to date. In a production enterprise environment,
    /// migration execution should be separated from application startup (e.g. a dedicated
    /// migration job in a CI/CD pipeline) to avoid race conditions across mult-instance deployments.
    /// </remarks>
    public class DatabaseInitialiser
    {
        public static async Task InitialiseAsync(IServiceProvider serviceProvider)
        {
            // A scope must be explicitly created to resolve Scoped services (like AppDbContext)
            // outside of a standard HTTP request pipeline.

            using var scope = serviceProvider.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

            try
            {
                logger.LogInformation("Applying database migrations...");
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully.");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "A critical error occurred whilst applying database migrations. The application cannot start.");
                throw;
            }
        }
    }
}
