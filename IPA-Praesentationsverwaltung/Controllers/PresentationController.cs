using IPA_Praesentationsverwaltung.Infrastructure;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Models.ViewModels;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IPA_Praesentationsverwaltung.Controllers;

/// <summary>Administrative CRUD operations for presentations.</summary>
[Authorize(Roles = RoleNames.Admin)]
public class PresentationController : Controller
{
    private readonly IPresentationService _presentationService;

    public PresentationController(IPresentationService presentationService)
    {
        _presentationService = presentationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        IReadOnlyList<Presentation> presentations = await _presentationService.GetAllPresentationsAsync();
        return View(presentations);
    }

    [HttpGet]
    public IActionResult Create() => View("PresentationForm", new PresentationFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PresentationFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("PresentationForm", model);
        }

        await _presentationService.CreatePresentationAsync(
            new Presentation
            {
                Topic = model.Topic,
                StartsAt = model.StartsAt,
                MaxObservers = model.MaxObservers
            },
            model.RoomName);

        TempData["Success"] = "Die Präsentation wurde erstellt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        Presentation? presentation = await _presentationService.GetPresentationByIdAsync(id);
        if (presentation is null)
        {
            return NotFound();
        }

        return View("PresentationForm", new PresentationFormViewModel
        {
            Id = presentation.Id,
            Topic = presentation.Topic,
            StartsAt = presentation.StartsAt,
            RoomName = presentation.Room?.Name ?? string.Empty,
            MaxObservers = presentation.MaxObservers
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PresentationFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("PresentationForm", model);
        }

        await _presentationService.UpdatePresentationAsync(
            new Presentation
            {
                Id = model.Id,
                Topic = model.Topic,
                StartsAt = model.StartsAt,
                MaxObservers = model.MaxObservers
            },
            model.RoomName);

        TempData["Success"] = "Die Präsentation wurde aktualisiert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _presentationService.DeletePresentationAsync(id);
        TempData["Success"] = "Die Präsentation wurde gelöscht.";
        return RedirectToAction(nameof(Index));
    }
}
