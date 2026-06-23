using System.Globalization;
using IPA_Praesentationsverwaltung.Infrastructure;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Models.ViewModels;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IPA_Praesentationsverwaltung.Controllers;

/// <summary>Administrative CRUD operations for observer registrations.</summary>
[Authorize(Roles = RoleNames.Admin)]
public class RegistrationController : Controller
{
    private static readonly CultureInfo Swiss = CultureInfo.GetCultureInfo("de-CH");

    private readonly IRegistrationService _registrationService;
    private readonly IStudentService _studentService;
    private readonly IPresentationService _presentationService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<RegistrationController> _logger;

    public RegistrationController(
        IRegistrationService registrationService,
        IStudentService studentService,
        IPresentationService presentationService,
        INotificationService notificationService,
        ILogger<RegistrationController> logger)
    {
        _registrationService = registrationService;
        _studentService = studentService;
        _presentationService = presentationService;
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        IReadOnlyList<Registration> registrations = await _registrationService.GetAllRegistrationsAsync();
        return View(registrations);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new RegistrationFormViewModel();
        await PopulateOptionsAsync(model);
        return View("RegistrationForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegistrationFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View("RegistrationForm", model);
        }

        try
        {
            await _registrationService.CreateRegistrationAsync(model.StudentId, model.PresentationId);
            await NotifyStudentOfAdminChangeAsync(model.StudentId);
            TempData["Success"] = "Die Eintragung wurde erstellt.";
            return RedirectToAction(nameof(Index));
        }
        catch (RegistrationNotAllowedException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateOptionsAsync(model);
            return View("RegistrationForm", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        Registration? registration = await _registrationService.GetRegistrationByIdAsync(id);
        if (registration is null)
        {
            return NotFound();
        }

        var model = new RegistrationFormViewModel
        {
            Id = registration.Id,
            StudentId = registration.StudentId,
            PresentationId = registration.PresentationId
        };
        await PopulateOptionsAsync(model);
        return View("RegistrationForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RegistrationFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View("RegistrationForm", model);
        }

        Registration? previous = await _registrationService.GetRegistrationByIdAsync(model.Id);
        int? previousStudentId = previous?.StudentId;

        await _registrationService.UpdateRegistrationAsync(model.Id, model.StudentId, model.PresentationId);

        // Notify the new owner; if the assignment moved between students, the
        // previous owner is also informed because their selection just shrank.
        await NotifyStudentOfAdminChangeAsync(model.StudentId);
        if (previousStudentId.HasValue && previousStudentId.Value != model.StudentId)
        {
            await NotifyStudentOfAdminChangeAsync(previousStudentId.Value);
        }

        TempData["Success"] = "Die Eintragung wurde aktualisiert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        Registration? registration = await _registrationService.GetRegistrationByIdAsync(id);
        int? studentId = registration?.StudentId;

        await _registrationService.DeleteRegistrationAsync(id);

        if (studentId.HasValue)
        {
            await NotifyStudentOfAdminChangeAsync(studentId.Value);
        }

        TempData["Success"] = "Die Eintragung wurde gelöscht.";
        return RedirectToAction(nameof(Index));
    }

    // Loads the student together with their remaining registrations and sends a
    // notification e-mail. Failures are logged but do not abort the admin flow.
    private async Task NotifyStudentOfAdminChangeAsync(int studentId)
    {
        try
        {
            Student? student = await _studentService.GetStudentByIdAsync(studentId);
            if (student is null)
            {
                return;
            }

            IReadOnlyList<Registration> registrations =
                await _registrationService.GetRegistrationsByStudentAsync(studentId);

            List<Presentation> presentations = registrations
                .Where(r => r.Presentation is not null)
                .Select(r => r.Presentation!)
                .ToList();

            await _notificationService.SendSelectionChangedByAdminAsync(student, presentations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send selection-change notification to student {StudentId}.", studentId);
        }
    }

    // Fills the student and presentation drop-downs for the form.
    private async Task PopulateOptionsAsync(RegistrationFormViewModel model)
    {
        IReadOnlyList<Student> students = await _studentService.GetAllStudentsAsync();
        IReadOnlyList<Presentation> presentations = await _presentationService.GetAllPresentationsAsync();

        model.StudentOptions = students.Select(s => new SelectListItem
        {
            Value = s.Id.ToString(),
            Text = $"{s.LastName} {s.FirstName} ({s.Email})",
            Selected = s.Id == model.StudentId
        });

        model.PresentationOptions = presentations.Select(p => new SelectListItem
        {
            Value = p.Id.ToString(),
            Text = $"{p.StartsAt.ToString("dd.MM.yyyy HH:mm", Swiss)} – {p.Topic} (Raum {p.Room?.Name})",
            Selected = p.Id == model.PresentationId
        });
    }
}
