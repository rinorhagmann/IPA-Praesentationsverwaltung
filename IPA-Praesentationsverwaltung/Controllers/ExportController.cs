using IPA_Praesentationsverwaltung.Infrastructure;
using IPA_Praesentationsverwaltung.Models.Dtos;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IPA_Praesentationsverwaltung.Controllers;

/// <summary>
/// Export area: produces the room and observer overviews as a print view, CSV
/// download or PDF download.
/// </summary>
[Authorize(Roles = RoleNames.Admin)]
public class ExportController : Controller
{
    private const string CsvContentType = "text/csv";
    private const string PdfContentType = "application/pdf";

    private readonly IExportService _exportService;

    public ExportController(IExportService exportService)
    {
        _exportService = exportService;
    }

    [HttpGet]
    public IActionResult Index() => View();

    // ----- Room lists (grouped by room) -------------------------------------

    [HttpGet]
    public async Task<IActionResult> PrintRooms()
    {
        RoomObserverList list = await _exportService.CreatePrintListAsync(ListOrder.ByRoom);
        ViewData["Title"] = "Raumlisten";
        return View("PrintList", list);
    }

    [HttpGet]
    public async Task<IActionResult> RoomsCsv()
    {
        RoomObserverList list = await _exportService.CreatePrintListAsync(ListOrder.ByRoom);
        return File(_exportService.ExportAsCsv(list), CsvContentType, "raumlisten.csv");
    }

    [HttpGet]
    public async Task<IActionResult> RoomsPdf()
    {
        RoomObserverList list = await _exportService.CreatePrintListAsync(ListOrder.ByRoom);
        return File(_exportService.ExportAsPdf(list, "Raumlisten"), PdfContentType, "raumlisten.pdf");
    }

    // ----- Observer lists (chronological) -----------------------------------

    [HttpGet]
    public async Task<IActionResult> PrintObservers()
    {
        RoomObserverList list = await _exportService.CreatePrintListAsync(ListOrder.ByTime);
        ViewData["Title"] = "Zuseherlisten";
        return View("PrintList", list);
    }

    [HttpGet]
    public async Task<IActionResult> ObserversCsv()
    {
        RoomObserverList list = await _exportService.CreatePrintListAsync(ListOrder.ByTime);
        return File(_exportService.ExportAsCsv(list), CsvContentType, "zuseherlisten.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ObserversPdf()
    {
        RoomObserverList list = await _exportService.CreatePrintListAsync(ListOrder.ByTime);
        return File(_exportService.ExportAsPdf(list, "Zuseherlisten"), PdfContentType, "zuseherlisten.pdf");
    }
}
