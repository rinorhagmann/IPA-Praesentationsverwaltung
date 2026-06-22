using Microsoft.AspNetCore.Http;

namespace IPA_Praesentationsverwaltung.Models.ViewModels;

/// <summary>Upload model used by both CSV import drop zones.</summary>
public class CsvImportViewModel
{
    public IFormFile? CsvFile { get; set; }
}
