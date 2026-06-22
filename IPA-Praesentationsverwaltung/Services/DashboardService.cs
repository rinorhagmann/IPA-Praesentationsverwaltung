using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Domain;
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

        // Number of registrations per student, used to derive completion figures.
        List<int> registrationsPerStudent = await _dbContext.Students
            .Select(s => s.Registrations.Count)
            .ToListAsync(cancellationToken);

        int completed = registrationsPerStudent.Count(count => count >= Student.RequiredSelectionCount);

        // Outstanding registrations: how many selections are still missing overall.
        int pending = registrationsPerStudent.Sum(count => Math.Max(0, Student.RequiredSelectionCount - count));

        return new DashboardViewModel
        {
            TotalStudents = totalStudents,
            StudentsWithCompleteSelection = completed,
            TotalPresentations = totalPresentations,
            PendingRegistrations = pending
        };
    }
}
