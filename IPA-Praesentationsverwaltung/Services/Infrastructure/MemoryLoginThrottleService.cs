using Microsoft.Extensions.Caching.Memory;

namespace IPA_Praesentationsverwaltung.Services.Infrastructure;

/// <summary>
/// In-memory implementation of <see cref="ILoginThrottleService"/>. Failure
/// counters live in <see cref="IMemoryCache"/> and expire automatically, so a
/// lockout lifts on its own after the configured window.
/// </summary>
public sealed class MemoryLoginThrottleService : ILoginThrottleService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IMemoryCache _cache;

    public MemoryLoginThrottleService(IMemoryCache cache)
    {
        _cache = cache;
    }

    private sealed class AttemptState
    {
        public int FailedAttempts { get; set; }
        public DateTimeOffset? LockedUntil { get; set; }
    }

    public bool IsLockedOut(string email) => GetRemainingLockout(email) > TimeSpan.Zero;

    public void RegisterFailedAttempt(string email)
    {
        string key = BuildKey(email);
        AttemptState state = _cache.GetOrCreate(key, entry =>
        {
            entry.SlidingExpiration = LockoutDuration;
            return new AttemptState();
        })!;

        state.FailedAttempts++;
        if (state.FailedAttempts >= MaxFailedAttempts)
        {
            state.LockedUntil = DateTimeOffset.UtcNow.Add(LockoutDuration);
        }

        // Refresh the entry so the sliding expiration restarts on every attempt.
        _cache.Set(key, state, LockoutDuration);
    }

    public void Reset(string email) => _cache.Remove(BuildKey(email));

    public TimeSpan GetRemainingLockout(string email)
    {
        if (_cache.TryGetValue(BuildKey(email), out AttemptState? state) &&
            state?.LockedUntil is { } lockedUntil &&
            lockedUntil > DateTimeOffset.UtcNow)
        {
            return lockedUntil - DateTimeOffset.UtcNow;
        }

        return TimeSpan.Zero;
    }

    // Normalises the e-mail so the throttle is case-insensitive.
    private static string BuildKey(string email) =>
        $"login-throttle::{email.Trim().ToLowerInvariant()}";
}
