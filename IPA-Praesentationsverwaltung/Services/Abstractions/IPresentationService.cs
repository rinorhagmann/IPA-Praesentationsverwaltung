using IPA_Praesentationsverwaltung.Models.Domain;

namespace IPA_Praesentationsverwaltung.Services.Abstractions;

/// <summary>CRUD operations and queries for presentations.</summary>
public interface IPresentationService
{
    /// <summary>All presentations including their room and registration counts.</summary>
    Task<IReadOnlyList<Presentation>> GetAllPresentationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Presentations the given student may still select: they have free seats,
    /// are not already selected and do not clash in time with an existing choice.
    /// </summary>
    Task<IReadOnlyList<Presentation>> GetAvailablePresentationsAsync(Student student, CancellationToken cancellationToken = default);

    Task<Presentation?> GetPresentationByIdAsync(int id, CancellationToken cancellationToken = default);

    Task CreatePresentationAsync(Presentation presentation, string roomName, CancellationToken cancellationToken = default);

    Task UpdatePresentationAsync(Presentation presentation, string roomName, CancellationToken cancellationToken = default);

    Task DeletePresentationAsync(int id, CancellationToken cancellationToken = default);
}
