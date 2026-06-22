using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Services;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using IPA_Praesentationsverwaltung.Services.Infrastructure;
using IPA_Praesentationsverwaltung.Tests.TestSupport;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Services;

public class AuthServiceTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    private AuthService CreateSut(ApplicationDbContext context) =>
        new(context, _hasher, new MemoryLoginThrottleService(new MemoryCache(new MemoryCacheOptions())));

    private async Task<ApplicationDbContext> SeedUserAsync(bool isActive = true)
    {
        ApplicationDbContext context = InMemoryDbContextFactory.Create();
        context.Students.Add(new Student
        {
            Email = "anna@wgbs.ch",
            FirstName = "Anna",
            LastName = "Muster",
            PasswordHash = _hasher.Hash("Secret123!"),
            IsActive = isActive
        });
        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task LoginAsync_succeeds_with_correct_credentials()
    {
        using ApplicationDbContext context = await SeedUserAsync();
        AuthService sut = CreateSut(context);

        LoginOutcome outcome = await sut.LoginAsync("anna@wgbs.ch", "Secret123!");

        Assert.True(outcome.Succeeded);
        Assert.Equal(LoginResult.Success, outcome.Result);
        Assert.NotNull(outcome.User);
    }

    [Fact]
    public async Task LoginAsync_is_case_insensitive_for_the_email()
    {
        using ApplicationDbContext context = await SeedUserAsync();
        AuthService sut = CreateSut(context);

        LoginOutcome outcome = await sut.LoginAsync("ANNA@WGBS.CH", "Secret123!");

        Assert.True(outcome.Succeeded);
    }

    [Fact]
    public async Task LoginAsync_rejects_a_wrong_password()
    {
        using ApplicationDbContext context = await SeedUserAsync();
        AuthService sut = CreateSut(context);

        LoginOutcome outcome = await sut.LoginAsync("anna@wgbs.ch", "wrong");

        Assert.Equal(LoginResult.InvalidCredentials, outcome.Result);
    }

    [Fact]
    public async Task LoginAsync_rejects_an_unknown_user()
    {
        using ApplicationDbContext context = await SeedUserAsync();
        AuthService sut = CreateSut(context);

        LoginOutcome outcome = await sut.LoginAsync("ghost@wgbs.ch", "whatever");

        Assert.Equal(LoginResult.InvalidCredentials, outcome.Result);
    }

    [Fact]
    public async Task LoginAsync_reports_disabled_accounts()
    {
        using ApplicationDbContext context = await SeedUserAsync(isActive: false);
        AuthService sut = CreateSut(context);

        LoginOutcome outcome = await sut.LoginAsync("anna@wgbs.ch", "Secret123!");

        Assert.Equal(LoginResult.Disabled, outcome.Result);
    }

    [Fact]
    public async Task LoginAsync_locks_out_after_repeated_failures()
    {
        using ApplicationDbContext context = await SeedUserAsync();
        AuthService sut = CreateSut(context);

        // Five wrong attempts trip the brute-force protection.
        for (int i = 0; i < 5; i++)
        {
            await sut.LoginAsync("anna@wgbs.ch", "wrong");
        }

        LoginOutcome outcome = await sut.LoginAsync("anna@wgbs.ch", "Secret123!");

        Assert.Equal(LoginResult.LockedOut, outcome.Result);
        Assert.True(outcome.LockoutRemaining > TimeSpan.Zero);
    }
}
