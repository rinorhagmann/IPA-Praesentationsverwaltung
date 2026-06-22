using IPA_Praesentationsverwaltung.Data;
using Microsoft.EntityFrameworkCore;

namespace IPA_Praesentationsverwaltung.Tests.TestSupport;

/// <summary>Creates isolated in-memory <see cref="ApplicationDbContext"/> instances for tests.</summary>
public static class InMemoryDbContextFactory
{
    public static ApplicationDbContext Create()
    {
        // A unique database name per call keeps the tests fully isolated.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid()}")
            .EnableSensitiveDataLogging()
            .Options;

        return new ApplicationDbContext(options);
    }
}
