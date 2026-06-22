using IPA_Praesentationsverwaltung.Infrastructure;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Models.ViewModels;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IPA_Praesentationsverwaltung.Controllers;

/// <summary>
/// Student facing flow: browse presentations, select two of them, review the
/// selection and confirm it.
/// </summary>
[Authorize(Roles = RoleNames.Student)]
public class StudentController : Controller
{
    private readonly IStudentService _studentService;
    private readonly IPresentationService _presentationService;
    private readonly IRegistrationService _registrationService;
    private readonly INotificationService _notificationService;

    public StudentController(
        IStudentService studentService,
        IPresentationService presentationService,
        IRegistrationService registrationService,
        INotificationService notificationService)
    {
        _studentService = studentService;
        _presentationService = presentationService;
        _registrationService = registrationService;
        _notificationService = notificationService;
    }

    public IActionResult Index() => RedirectToAction(nameof(Presentations));

    [HttpGet]
    public async Task<IActionResult> Presentations()
    {
        int studentId = CurrentStudentId();

        IReadOnlyList<Presentation> presentations = await _presentationService.GetAllPresentationsAsync();
        HashSet<int> selectedIds = (await _registrationService.GetRegistrationsByStudentAsync(studentId))
            .Select(r => r.PresentationId)
            .ToHashSet();

        var model = new StudentSelectionViewModel
        {
            StudentId = studentId,
            StudentName = User.GetDisplayName(),
            AvailablePresentations = presentations
                .Select(p => PresentationListItemViewModel.FromDomain(p, selectedIds.Contains(p.Id)))
                .ToList(),
            RemainingSelections = Student.RequiredSelectionCount - selectedIds.Count
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Select(int presentationId)
    {
        int studentId = CurrentStudentId();

        try
        {
            await _registrationService.CreateRegistrationAsync(studentId, presentationId);
            TempData["Success"] = "Die Präsentation wurde zu Ihrer Auswahl hinzugefügt.";
        }
        catch (RegistrationNotAllowedException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Presentations));
    }

    [HttpGet]
    public async Task<IActionResult> MySelection()
    {
        StudentSelectionViewModel model = await BuildSelectionModelAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveSelection(int registrationId)
    {
        int studentId = CurrentStudentId();

        // Guard: a student may only remove their own registrations.
        Registration? registration = await _registrationService.GetRegistrationByIdAsync(registrationId);
        if (registration is null || registration.StudentId != studentId)
        {
            return Forbid();
        }

        await _registrationService.DeleteRegistrationAsync(registrationId);
        TempData["Success"] = "Die Präsentation wurde aus Ihrer Auswahl entfernt.";
        return RedirectToAction(nameof(MySelection));
    }

    [HttpGet]
    public async Task<IActionResult> Confirm()
    {
        StudentSelectionViewModel model = await BuildSelectionModelAsync();
        if (!model.HasCompletedSelection)
        {
            TempData["Error"] = $"Bitte wählen Sie zuerst {Student.RequiredSelectionCount} Präsentationen aus.";
            return RedirectToAction(nameof(Presentations));
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmSelection()
    {
        int studentId = CurrentStudentId();

        Student? student = await _studentService.GetStudentByIdAsync(studentId);
        if (student is null)
        {
            return Forbid();
        }

        IReadOnlyList<Registration> registrations = await _registrationService.GetRegistrationsByStudentAsync(studentId);
        if (registrations.Count < Student.RequiredSelectionCount)
        {
            TempData["Error"] = $"Bitte wählen Sie zuerst {Student.RequiredSelectionCount} Präsentationen aus.";
            return RedirectToAction(nameof(Presentations));
        }

        List<Presentation> presentations = registrations
            .Where(r => r.Presentation is not null)
            .Select(r => r.Presentation!)
            .ToList();

        await _notificationService.SendConfirmationAsync(student, presentations);
        return RedirectToAction(nameof(Success));
    }

    [HttpGet]
    public IActionResult Success() => View();

    private async Task<StudentSelectionViewModel> BuildSelectionModelAsync()
    {
        int studentId = CurrentStudentId();
        IReadOnlyList<Registration> registrations = await _registrationService.GetRegistrationsByStudentAsync(studentId);

        return new StudentSelectionViewModel
        {
            StudentId = studentId,
            StudentName = User.GetDisplayName(),
            CurrentSelections = registrations
                .Where(r => r.Presentation is not null)
                .Select(r => new PresentationListItemViewModel
                {
                    Id = r.Presentation!.Id,
                    Topic = r.Presentation.Topic,
                    StartsAt = r.Presentation.StartsAt,
                    RoomName = r.Presentation.Room?.Name ?? string.Empty,
                    MaxObservers = r.Presentation.MaxObservers,
                    TakenSeats = r.Presentation.Registrations.Count,
                    IsSelectedByStudent = true
                })
                .ToList(),
            RemainingSelections = Student.RequiredSelectionCount - registrations.Count
        };
    }

    // The student id is taken from the authenticated principal, never from input.
    private int CurrentStudentId() =>
        User.GetUserId() ?? throw new InvalidOperationException("Authenticated student id is missing.");
}
