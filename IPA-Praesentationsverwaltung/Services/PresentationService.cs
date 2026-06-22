using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace IPA_Praesentationsverwaltung.Services;

/// <summary>Entity Framework backed implementation of <see cref="IPresentationService"/>.</summary>
public sealed class PresentationService : IPresentationService
{
    private readonly ApplicationDbContext _dbContext;

    public PresentationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Presentation>> GetAllPresentationsAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Presentations
            .AsNoTracking()
            .Include(p => p.Room)
            .Include(p => p.Registrations)
            .OrderBy(p => p.StartsAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Presentation>> GetAvailablePresentationsAsync(
        Student student, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(student);

        // Start times of the presentations the student has already chosen, used
        // to filter out anything that would collide in time.
        List<DateTime> selectedStartTimes = await _dbContext.Registrations
            .Where(r => r.StudentId == student.Id)
            .Select(r => r.Presentation!.StartsAt)
            .ToListAsync(cancellationToken);

        HashSet<int> selectedPresentationIds = await _dbContext.Registrations
            .Where(r => r.StudentId == student.Id)
            .Select(r => r.PresentationId)
            .ToHashSetAsync(cancellationToken);

        List<Presentation> presentations = await _dbContext.Presentations
            .AsNoTracking()
            .Include(p => p.Room)
            .Include(p => p.Registrations)
            .OrderBy(p => p.StartsAt)
            .ToListAsync(cancellationToken);

        return presentations
            .Where(p => !selectedPresentationIds.Contains(p.Id))
            .Where(p => p.HasFreeSeats())
            .Where(p => !selectedStartTimes.Contains(p.StartsAt))
            .ToList();
    }

    public Task<Presentation?> GetPresentationByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.Presentations
            .Include(p => p.Room)
            .Include(p => p.Registrations)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task CreatePresentationAsync(
        Presentation presentation, string roomName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(presentation);

        presentation.RoomId = (await GetOrCreateRoomAsync(roomName, cancellationToken)).Id;
        presentation.Room = null; // Avoid inserting a duplicate room via the navigation.

        _dbContext.Presentations.Add(presentation);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePresentationAsync(
        Presentation presentation, string roomName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(presentation);

        Presentation existing = await _dbContext.Presentations
            .FirstOrDefaultAsync(p => p.Id == presentation.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Presentation {presentation.Id} was not found.");

        existing.Topic = presentation.Topic;
        existing.StartsAt = presentation.StartsAt;
        existing.MaxObservers = presentation.MaxObservers;
        existing.RoomId = (await GetOrCreateRoomAsync(roomName, cancellationToken)).Id;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeletePresentationAsync(int id, CancellationToken cancellationToken = default)
    {
        Presentation? presentation = await _dbContext.Presentations.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (presentation is null)
        {
            return;
        }

        _dbContext.Presentations.Remove(presentation);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Returns the room with the given name, creating it on first use so room
    /// names are stored exactly once (third normal form).
    /// </summary>
    private async Task<Room> GetOrCreateRoomAsync(string roomName, CancellationToken cancellationToken)
    {
        string normalized = (roomName ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("A room name is required.", nameof(roomName));
        }

        Room? room = await _dbContext.Rooms.FirstOrDefaultAsync(r => r.Name == normalized, cancellationToken);
        if (room is not null)
        {
            return room;
        }

        room = new Room { Name = normalized };
        _dbContext.Rooms.Add(room);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return room;
    }
}
