using System.Security.Claims;
using IPA_Praesentationsverwaltung.Infrastructure;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Models.ViewModels;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IPA_Praesentationsverwaltung.Controllers;

/// <summary>Handles login and logout for both administrators and students.</summary>
public class AuthController : Controller
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        // Already authenticated users are sent straight to their start page.
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToStart();
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        LoginOutcome outcome = await _authService.LoginAsync(model.Email, model.Password);

        if (!outcome.Succeeded)
        {
            // Deliberately generic messages to avoid disclosing account details.
            ModelState.AddModelError(string.Empty, BuildErrorMessage(outcome));
            return View(model);
        }

        await SignInAsync(outcome.User!);
        return RedirectAfterLogin(outcome.User!, model.ReturnUrl);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private async Task SignInAsync(User user)
    {
        // Persist the minimum identifying claims needed for authorization.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.GetFullName()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);
    }

    private IActionResult RedirectAfterLogin(User user, string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return user.Role == UserRole.Admin
            ? RedirectToAction("Dashboard", "Admin")
            : RedirectToAction("Presentations", "Student");
    }

    private IActionResult RedirectToStart() =>
        User.IsInRole(RoleNames.Admin)
            ? RedirectToAction("Dashboard", "Admin")
            : RedirectToAction("Presentations", "Student");

    private static string BuildErrorMessage(LoginOutcome outcome) => outcome.Result switch
    {
        LoginResult.LockedOut =>
            $"Zu viele fehlgeschlagene Versuche. Bitte versuchen Sie es in {Math.Ceiling(outcome.LockoutRemaining.TotalMinutes)} Minuten erneut.",
        LoginResult.Disabled => "Dieses Konto ist deaktiviert.",
        _ => "E-Mail-Adresse oder Passwort ist falsch."
    };
}
