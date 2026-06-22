using IPA_Praesentationsverwaltung.Infrastructure;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Models.ViewModels;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IPA_Praesentationsverwaltung.Controllers;

/// <summary>
/// Administrative CRUD operations for G3 students. Separate from
/// <see cref="StudentController"/>, which serves the student-facing flow.
/// </summary>
[Authorize(Roles = RoleNames.Admin)]
public class StudentManagementController : Controller
{
    private readonly IStudentService _studentService;

    public StudentManagementController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        IReadOnlyList<Student> students = await _studentService.GetAllStudentsAsync();
        return View(students);
    }

    [HttpGet]
    public IActionResult Create() => View("StudentForm", new StudentFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("StudentForm", model);
        }

        string password = await _studentService.CreateStudentAsync(
            new Student { Email = model.Email, FirstName = model.FirstName, LastName = model.LastName },
            string.IsNullOrWhiteSpace(model.Password) ? null : model.Password);

        // Surface the generated password once so the admin can communicate it.
        TempData["Success"] = $"Die Schülerin / der Schüler wurde erstellt. Initiales Passwort: {password}";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        Student? student = await _studentService.GetStudentByIdAsync(id);
        if (student is null)
        {
            return NotFound();
        }

        return View("StudentForm", new StudentFormViewModel
        {
            Id = student.Id,
            Email = student.Email,
            FirstName = student.FirstName,
            LastName = student.LastName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(StudentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("StudentForm", model);
        }

        await _studentService.UpdateStudentAsync(
            new Student
            {
                Id = model.Id,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName
            },
            string.IsNullOrWhiteSpace(model.Password) ? null : model.Password);

        TempData["Success"] = "Die Schülerin / der Schüler wurde aktualisiert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _studentService.DeleteStudentAsync(id);
        TempData["Success"] = "Die Schülerin / der Schüler wurde gelöscht.";
        return RedirectToAction(nameof(Index));
    }
}
