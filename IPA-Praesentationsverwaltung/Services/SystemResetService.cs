using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace IPA_Praesentationsverwaltung.Services;

/// <summary>
/// Clears all year-specific data so the application can be reused for a new
/// school year. Administrator accounts are kept so the system stays accessible.
/// </summary>
public sealed class SystemResetService : ISystemResetService
{
    private readonly ApplicationDbContext _dbContext;

    public SystemResetService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ResetSchoolYearAsync(CancellationToken cancellationToken = default)
    {
        // Remove dependent rows first so the foreign-key constraints stay satisfied.
        // RemoveRange is used (instead of bulk ExecuteDelete) to keep the operation
        // provider-agnostic; the data volume here is small (a few hundred rows).
        _dbContext.Registrations.RemoveRange(await _dbContext.Registrations.ToListAsync(cancellationToken));
        _dbContext.Presentations.RemoveRange(await _dbContext.Presentations.ToListAsync(cancellationToken));

        // Only the Student rows of the TPH Users table are removed; admins remain.
        _dbContext.Students.RemoveRange(await _dbContext.Students.ToListAsync(cancellationToken));
        _dbContext.Rooms.RemoveRange(await _dbContext.Rooms.ToListAsync(cancellationToken));

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
