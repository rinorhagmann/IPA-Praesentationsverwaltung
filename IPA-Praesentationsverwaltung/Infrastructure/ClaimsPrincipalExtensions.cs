using System.Security.Claims;

namespace IPA_Praesentationsverwaltung.Infrastructure;

/// <summary>Convenience accessors for the claims stored in the auth cookie.</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Returns the authenticated user's database id, or null if absent.</summary>
    public static int? GetUserId(this ClaimsPrincipal principal)
    {
        string? value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out int id) ? id : null;
    }

    /// <summary>Returns the authenticated user's display name.</summary>
    public static string GetDisplayName(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
}
