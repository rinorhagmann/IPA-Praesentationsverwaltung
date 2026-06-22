using IPA_Praesentationsverwaltung.Infrastructure;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Models.Dtos;
using IPA_Praesentationsverwaltung.Models.ViewModels;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IPA_Praesentationsverwaltung.Controllers;

/// <summary>
/// CSV import area and dispatch of the generated student access credentials.
/// </summary>
[Authorize(Roles = RoleNames.Admin)]
public class ImportController : Controller
{
    private const long MaxUploadBytes = 5 * 1024 * 1024; // 5 MB safety limit.

    private readonly ICsvImportService _csvImportService;
    private readonly IStudentService _studentService;
    private readonly INotificationService _notificationService;

    public ImportController(
        ICsvImportService csvImportService,
        IStudentService studentService,
        INotificationService notificationService)
    {
        _csvImportService = csvImportService;
        _studentService = studentService;
        _notificationService = notificationService;
    }

    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> ImportPresentations(CsvImportViewModel model)
    {
        if (!TryValidateUpload(model, out string? error))
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }

        await using Stream stream = model.CsvFile!.OpenReadStream();
        ImportResult result = await _csvImportService.ImportPresentationsAsync(stream);
        ReportImport(result, "Präsentationen");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> ImportStudents(CsvImportViewModel model)
    {
        if (!TryValidateUpload(model, out string? error))
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }

        await using Stream stream = model.CsvFile!.OpenReadStream();
        ImportResult result = await _csvImportService.ImportStudentsAsync(stream);
        ReportImport(result, "Schüler/innen");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendCredentials()
    {
        IReadOnlyList<Student> pending = await _studentService.GetStudentsWithoutSentCredentialsAsync();
        int sent = 0;

        foreach (Student student in pending)
        {
            // Generate a fresh password, store only its hash, then mail the plaintext.
            string password = await _studentService.ResetPasswordAsync(student.Id);
            await _notificationService.SendCredentialsAsync(student, password);
            await _studentService.MarkCredentialsSentAsync(student.Id);
            sent++;
        }

        TempData["Success"] = sent > 0
            ? $"Zugangsdaten wurden an {sent} Schüler/innen versendet."
            : "Es gibt keine Schüler/innen ohne versendete Zugangsdaten.";

        return RedirectToAction(nameof(Index));
    }

    private static bool TryValidateUpload(CsvImportViewModel model, out string? error)
    {
        if (model.CsvFile is null || model.CsvFile.Length == 0)
        {
            error = "Bitte wählen Sie eine CSV-Datei aus.";
            return false;
        }

        string extension = Path.GetExtension(model.CsvFile.FileName);
        if (!extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            error = "Es werden nur CSV-Dateien unterstützt.";
            return false;
        }

        error = null;
        return true;
    }

    private void ReportImport(ImportResult result, string entityLabel)
    {
        TempData["Success"] = $"{result.ImportedCount} {entityLabel} wurden importiert.";
        if (result.HasErrors)
        {
            // Join the per-row issues into a single message for display.
            TempData["Error"] = string.Join(" \n", result.Errors);
        }
    }
}
