using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace IPA_Praesentationsverwaltung.Services;

/// <summary>
/// Coordinates observer registrations and enforces the assignment rules through
/// <see cref="IAssignmentRuleService"/> before any data is written.
/// </summary>
public sealed class RegistrationService : IRegistrationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAssignmentRuleService _ruleService;

    public RegistrationService(ApplicationDbContext dbContext, IAssignmentRuleService ruleService)
    {
        _dbContext = dbContext;
        _ruleService = ruleService;
    }

    public async Task<IReadOnlyList<Registration>> GetRegistrationsByStudentAsync(
        int studentId, CancellationToken cancellationToken = default) =>
        await _dbContext.Registrations
            .AsNoTracking()
            .Include(r => r.Presentation!).ThenInclude(p => p.Room)
            .Where(r => r.StudentId == studentId)
            .OrderBy(r => r.Presentation!.StartsAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Registration>> GetAllRegistrationsAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Registrations
            .AsNoTracking()
            .Include(r => r.Student)
            .Include(r => r.Presentation!).ThenInclude(p => p.Room)
            .OrderBy(r => r.Presentation!.StartsAt)
            .ToListAsync(cancellationToken);

    public Task<Registration?> GetRegistrationByIdAsync(int registrationId, CancellationToken cancellationToken = default) =>
        _dbContext.Registrations
            .Include(r => r.Student)
            .Include(r => r.Presentation!).ThenInclude(p => p.Room)
            .FirstOrDefaultAsync(r => r.Id == registrationId, cancellationToken);

    public async Task CreateRegistrationAsync(
        int studentId, int presentationId, CancellationToken cancellationToken = default)
    {
        Student student = await LoadStudentWithSelectionsAsync(studentId, cancellationToken)
            ?? throw new RegistrationNotAllowedException("Die Schülerin / der Schüler wurde nicht gefunden.");

        Presentation presentation = await _dbContext.Presentations
            .Include(p => p.Registrations)
            .FirstOrDefaultAsync(p => p.Id == presentationId, cancellationToken)
            ?? throw new RegistrationNotAllowedException("Die Präsentation wurde nicht gefunden.");

        // Translate each violated rule into a user-facing German message.
        if (!_ruleService.HasMaximumTwoRegistrations(student))
        {
            throw new RegistrationNotAllowedException(
                "Sie haben bereits die maximale Anzahl von zwei Präsentationen ausgewählt.");
        }

        if (!_ruleService.HasFreeSeats(presentation))
        {
            throw new RegistrationNotAllowedException(
                "Diese Präsentation ist bereits ausgebucht.");
        }

        if (!_ruleService.HasNoTimeConflict(student, presentation))
        {
            throw new RegistrationNotAllowedException(
                "Sie sind zur gleichen Zeit bereits für eine andere Präsentation angemeldet.");
        }

        bool alreadyRegistered = student.Registrations.Any(r => r.PresentationId == presentationId);
        if (alreadyRegistered)
        {
            throw new RegistrationNotAllowedException(
                "Sie sind für diese Präsentation bereits angemeldet.");
        }

        _dbContext.Registrations.Add(new Registration
        {
            StudentId = studentId,
            PresentationId = presentationId,
            RegisteredAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRegistrationAsync(
        int registrationId, int studentId, int presentationId, CancellationToken cancellationToken = default)
    {
        Registration registration =
            await _dbContext.Registrations.FirstOrDefaultAsync(r => r.Id == registrationId, cancellationToken)
            ?? throw new RegistrationNotAllowedException("Die Eintragung wurde nicht gefunden.");

        registration.StudentId = studentId;
        registration.PresentationId = presentationId;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRegistrationAsync(int registrationId, CancellationToken cancellationToken = default)
    {
        Registration? registration =
            await _dbContext.Registrations.FirstOrDefaultAsync(r => r.Id == registrationId, cancellationToken);
        if (registration is null)
        {
            return;
        }

        _dbContext.Registrations.Remove(registration);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public bool CanRegister(Student student, Presentation presentation)
    {
        ArgumentNullException.ThrowIfNull(student);
        ArgumentNullException.ThrowIfNull(presentation);

        return _ruleService.HasMaximumTwoRegistrations(student)
               && _ruleService.HasFreeSeats(presentation)
               && _ruleService.HasNoTimeConflict(student, presentation);
    }

    // Loads a student together with the presentations behind each registration,
    // which the rule service needs to detect time conflicts.
    private Task<Student?> LoadStudentWithSelectionsAsync(int studentId, CancellationToken cancellationToken) =>
        _dbContext.Students
            .Include(s => s.Registrations)
            .ThenInclude(r => r.Presentation)
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);
}
