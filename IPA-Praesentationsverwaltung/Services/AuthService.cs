using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using IPA_Praesentationsverwaltung.Services.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IPA_Praesentationsverwaltung.Services;

/// <summary>
/// Default authentication service. Combines a constant-time password check with
/// the brute-force throttle so repeated failures temporarily lock an account.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILoginThrottleService _loginThrottle;

    public AuthService(
        ApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        ILoginThrottleService loginThrottle)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _loginThrottle = loginThrottle;
    }

    public async Task<LoginOutcome> LoginAsync(
        string email, string password, CancellationToken cancellationToken = default)
    {
        string normalizedEmail = NormalizeEmail(email);

        if (_loginThrottle.IsLockedOut(normalizedEmail))
        {
            return new LoginOutcome(
                LoginResult.LockedOut, User: null, _loginThrottle.GetRemainingLockout(normalizedEmail));
        }

        User? user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        // Always run the hash verification, even when the user is unknown, to keep
        // the response time uniform and avoid leaking which accounts exist.
        bool passwordMatches =
            user is not null && VerifyPassword(password, user.PasswordHash);

        if (user is null || !passwordMatches)
        {
            RegisterFailedLogin(normalizedEmail);
            return new LoginOutcome(LoginResult.InvalidCredentials, User: null, TimeSpan.Zero);
        }

        if (!user.IsActive)
        {
            return new LoginOutcome(LoginResult.Disabled, User: null, TimeSpan.Zero);
        }

        _loginThrottle.Reset(normalizedEmail);
        return new LoginOutcome(LoginResult.Success, user, TimeSpan.Zero);
    }

    public string HashPassword(string password) => _passwordHasher.Hash(password);

    public bool VerifyPassword(string password, string hash) => _passwordHasher.Verify(password, hash);

    public void RegisterFailedLogin(string email) =>
        _loginThrottle.RegisterFailedAttempt(NormalizeEmail(email));

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
