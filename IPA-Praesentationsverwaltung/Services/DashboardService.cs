using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.ViewModels;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace IPA_Praesentationsverwaltung.Services;

/// <summary>Computes the administration dashboard statistics from the database.</summary>
public sealed class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _dbContext;

    public DashboardService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        int totalStudents = await _dbContext.Students.CountAsync(cancellationToken);
        int totalPresentations = await _dbContext.Presentations.CountAsync(cancellationToken);

        int studentsWithoutRegistration = await _dbContext.Students
            .CountAsync(s => !s.Registrations.Any(), cancellationToken);

        int presentationsWithoutObservers = await _dbContext.Presentations
            .CountAsync(p => !p.Registrations.Any(), cancellationToken);

        return new DashboardViewModel
        {
            TotalStudents = totalStudents,
            TotalPresentations = totalPresentations,
            StudentsWithoutRegistration = studentsWithoutRegistration,
            PresentationsWithoutObservers = presentationsWithoutObservers
        };
    }
}
