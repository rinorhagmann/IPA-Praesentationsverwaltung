using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Services.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IPA_Praesentationsverwaltung.Data;

/// <summary>
/// Applies pending migrations and seeds a default administrator account so the
/// freshly provisioned system is immediately usable. The seed credentials are
/// read from configuration (see appsettings) and the password is hashed.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = services.CreateScope();
        IServiceProvider provider = scope.ServiceProvider;

        var dbContext = provider.GetRequiredService<ApplicationDbContext>();
        var configuration = provider.GetRequiredService<IConfiguration>();
        var passwordHasher = provider.GetRequiredService<IPasswordHasher>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        // Bring the schema up to date. Retried briefly because the SQL Server
        // container may still be starting up when the web app boots.
        await MigrateWithRetryAsync(dbContext, logger, cancellationToken);

        await SeedDefaultAdminAsync(dbContext, configuration, passwordHasher, logger, cancellationToken);
    }

    private static async Task MigrateWithRetryAsync(
        ApplicationDbContext dbContext, ILogger logger, CancellationToken cancellationToken)
    {
        const int maxAttempts = 10;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex,
                    "Database not ready (attempt {Attempt}/{Max}). Retrying in 5s...", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private static async Task SeedDefaultAdminAsync(
        ApplicationDbContext dbContext,
        IConfiguration configuration,
        IPasswordHasher passwordHasher,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Admins.AnyAsync(cancellationToken))
        {
            return;
        }

        string email = (configuration["DefaultAdmin:Email"] ?? "admin@wgbs.ch").Trim().ToLowerInvariant();
        string password = configuration["DefaultAdmin:Password"] ?? "Admin123!";
        string firstName = configuration["DefaultAdmin:FirstName"] ?? "System";
        string lastName = configuration["DefaultAdmin:LastName"] ?? "Administrator";

        dbContext.Admins.Add(new Admin
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = passwordHasher.Hash(password),
            CanManageSystem = true,
            IsActive = true
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded default administrator account '{Email}'.", email);
    }
}
