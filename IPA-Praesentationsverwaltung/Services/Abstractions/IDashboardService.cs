using IPA_Praesentationsverwaltung.Models.ViewModels;

namespace IPA_Praesentationsverwaltung.Services.Abstractions;

/// <summary>Aggregates the figures shown on the administration dashboard.</summary>
public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);
}
