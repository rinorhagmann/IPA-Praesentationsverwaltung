using System.Diagnostics;
using IPA_Praesentationsverwaltung.Infrastructure;
using IPA_Praesentationsverwaltung.Models;
using Microsoft.AspNetCore.Mvc;

namespace IPA_Praesentationsverwaltung.Controllers;

/// <summary>
/// Entry point that routes visitors to the correct start page depending on
/// whether and how they are authenticated.
/// </summary>
public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Login", "Auth");
        }

        return User.IsInRole(RoleNames.Admin)
            ? RedirectToAction("Dashboard", "Admin")
            : RedirectToAction("Presentations", "Student");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
