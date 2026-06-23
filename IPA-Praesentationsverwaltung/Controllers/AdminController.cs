using IPA_Praesentationsverwaltung.Infrastructure;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Models.ViewModels;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IPA_Praesentationsverwaltung.Controllers;

/// <summary>
/// Administration area: dashboard, system reset and administrator management.
/// </summary>
[Authorize(Roles = RoleNames.Admin)]
public class AdminController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly ISystemResetService _systemResetService;
    private readonly IAdminAccountService _adminAccountService;

    public AdminController(
        IDashboardService dashboardService,
        ISystemResetService systemResetService,
        IAdminAccountService adminAccountService)
    {
        _dashboardService = dashboardService;
        _systemResetService = systemResetService;
        _adminAccountService = adminAccountService;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        DashboardViewModel model = await _dashboardService.GetDashboardAsync();
        return View(model);
    }

    // ----- System reset -----------------------------------------------------

    [HttpGet]
    public IActionResult ResetSystem() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetSystem(bool confirmed)
    {
        await _systemResetService.ResetSchoolYearAsync();
        TempData["Success"] = "Das System wurde zurückgesetzt. Alle Präsentationen, Schüler/innen und Eintragungen wurden gelöscht.";
        return RedirectToAction(nameof(Dashboard));
    }

    // ----- Administrator management -----------------------------------------

    [HttpGet]
    public async Task<IActionResult> ManageAdmins()
    {
        IReadOnlyList<Admin> admins = await _adminAccountService.GetAllAdminsAsync();
        return View(admins);
    }

    [HttpGet]
    public IActionResult CreateAdmin() => View("AdminForm", new AdminFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAdmin(AdminFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Bitte vergeben Sie ein Passwort.");
        }

        if (!ModelState.IsValid)
        {
            return View("AdminForm", model);
        }

        await _adminAccountService.CreateAdminAsync(
            new Admin { Email = model.Email, FirstName = model.FirstName, LastName = model.LastName },
            model.Password!);

        TempData["Success"] = "Der Administrator-Login wurde erstellt.";
        return RedirectToAction(nameof(ManageAdmins));
    }

    [HttpGet]
    public async Task<IActionResult> EditAdmin(int id)
    {
        Admin? admin = await _adminAccountService.GetAdminByIdAsync(id);
        if (admin is null)
        {
            return NotFound();
        }

        return View("AdminForm", new AdminFormViewModel
        {
            Id = admin.Id,
            Email = admin.Email,
            FirstName = admin.FirstName,
            LastName = admin.LastName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAdmin(AdminFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("AdminForm", model);
        }

        await _adminAccountService.UpdateAdminAsync(
            new Admin { Id = model.Id, Email = model.Email, FirstName = model.FirstName, LastName = model.LastName },
            model.Password);

        TempData["Success"] = "Der Administrator-Login wurde aktualisiert.";
        return RedirectToAction(nameof(ManageAdmins));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAdmin(int id)
    {
        int? currentUserId = User.GetUserId();
        if (currentUserId == id)
        {
            TempData["Error"] = "Sie können Ihren eigenen Administrator-Login nicht löschen.";
            return RedirectToAction(nameof(ManageAdmins));
        }

        bool deleted = await _adminAccountService.DeleteAdminAsync(id);
        TempData[deleted ? "Success" : "Error"] = deleted
            ? "Der Administrator-Login wurde gelöscht."
            : "Der letzte Administrator-Login kann nicht gelöscht werden.";

        return RedirectToAction(nameof(ManageAdmins));
    }
}
