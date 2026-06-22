namespace IPA_Praesentationsverwaltung.Services.Infrastructure;

/// <summary>
/// Protects the login endpoint against brute-force attacks by tracking failed
/// attempts per account and temporarily locking it after too many failures.
/// </summary>
public interface ILoginThrottleService
{
    /// <summary>True while the account is locked out due to repeated failures.</summary>
    bool IsLockedOut(string email);

    /// <summary>Records a failed login attempt and locks the account when the limit is reached.</summary>
    void RegisterFailedAttempt(string email);

    /// <summary>Clears the failure counter after a successful login.</summary>
    void Reset(string email);

    /// <summary>Remaining lockout time, or <see cref="TimeSpan.Zero"/> if not locked.</summary>
    TimeSpan GetRemainingLockout(string email);
}
