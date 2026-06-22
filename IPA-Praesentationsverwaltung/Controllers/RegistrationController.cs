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

    public RegistrationController(
        IRegistrationService registrationService,
        IStudentService studentService,
        IPresentationService presentationService)
    {
        _registrationService = registrationService;
        _studentService = studentService;
        _presentationService = presentationService;
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

        await _registrationService.UpdateRegistrationAsync(model.Id, model.StudentId, model.PresentationId);
        TempData["Success"] = "Die Eintragung wurde aktualisiert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _registrationService.DeleteRegistrationAsync(id);
        TempData["Success"] = "Die Eintragung wurde gelöscht.";
        return RedirectToAction(nameof(Index));
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
