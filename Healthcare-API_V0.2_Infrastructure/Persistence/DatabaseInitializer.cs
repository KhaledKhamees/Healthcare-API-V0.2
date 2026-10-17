using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Healthcare_API_V0._2_Infrastructure.Persistence
{
    public static class DatabaseInitializer
    {
        private const int DefaultMaxAttempts = 12;
        private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Applies pending migrations, retrying while the database server is still
        /// starting up or temporarily unreachable.
        /// </summary>
        /// <param name="serviceProvider">
        /// A scoped (or root) service provider used to resolve <see cref="HealthcareDbContext"/>.
        /// </param>
        /// <param name="logger">Logger for startup diagnostics.</param>
        /// <param name="maxAttempts">Total number of attempts before the exception is rethrown.</param>
        /// <param name="retryDelay">Delay between attempts.</param>
        public static async Task MigrateAsync(
            IServiceProvider serviceProvider,
            ILogger logger,
            int maxAttempts = DefaultMaxAttempts,
            TimeSpan? retryDelay = null)
        {
            var delay = retryDelay ?? DefaultRetryDelay;

            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<HealthcareDbContext>();

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    logger.LogInformation(
                        "Applying database migrations (attempt {Attempt}/{MaxAttempts})...",
                        attempt,
                        maxAttempts);

                    // Creates the database if missing, then applies only pending migrations.
                    await dbContext.Database.MigrateAsync();

                    logger.LogInformation("Database migrations applied successfully.");
                    return;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    // Expected on first run: SQL Server may still be starting, or the
                    // target database may not exist yet (MigrateAsync creates it).
                    logger.LogWarning(
                        ex,
                        "Database is not ready or migration failed (attempt {Attempt}/{MaxAttempts}). Retrying in {DelaySeconds}s...",
                        attempt,
                        maxAttempts,
                        delay.TotalSeconds);

                    await Task.Delay(delay);
                }
            }
        }
    }
}
