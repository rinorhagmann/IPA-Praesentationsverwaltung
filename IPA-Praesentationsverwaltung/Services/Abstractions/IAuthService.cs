using IPA_Praesentationsverwaltung.Models.Domain;

namespace IPA_Praesentationsverwaltung.Services.Abstractions;

/// <summary>Result of an authentication attempt.</summary>
public enum LoginResult
{
    Success,
    InvalidCredentials,
    Disabled,
    LockedOut
}

/// <summary>Outcome of <see cref="IAuthService.LoginAsync"/>.</summary>
public sealed record LoginOutcome(LoginResult Result, User? User, TimeSpan LockoutRemaining)
{
    public bool Succeeded => Result == LoginResult.Success && User is not null;
}

/// <summary>
/// Authenticates users, hashes and verifies passwords and applies brute-force
/// protection on top of the credential check.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Validates the credentials, honouring account state and lockout. Never
    /// reveals whether the e-mail exists to avoid user enumeration.
    /// </summary>
    Task<LoginOutcome> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    string HashPassword(string password);

    bool VerifyPassword(string password, string hash);

    void RegisterFailedLogin(string email);
}
