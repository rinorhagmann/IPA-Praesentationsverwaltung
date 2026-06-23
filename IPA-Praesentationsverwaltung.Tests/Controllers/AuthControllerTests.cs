using IPA_Praesentationsverwaltung.Controllers;
using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Models.ViewModels;
using IPA_Praesentationsverwaltung.Services;
using IPA_Praesentationsverwaltung.Services.Infrastructure;
using IPA_Praesentationsverwaltung.Tests.TestSupport;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Controllers;

public class AuthControllerTests
{
    private static AuthController CreateSut(ApplicationDbContext context)
    {
        var hasher = new Pbkdf2PasswordHasher();
        var throttle = new MemoryLoginThrottleService(new MemoryCache(new MemoryCacheOptions()));
        return new AuthController(new AuthService(context, hasher, throttle));
    }

    private static IServiceProvider AuthServices(FakeAuthenticationService fake) =>
        new ServiceCollection()
            .AddSingleton<IAuthenticationService>(fake)
            .AddSingleton<IUrlHelperFactory, UrlHelperFactory>()
            .BuildServiceProvider();

    [Fact]
    public void Login_get_shows_form_for_anonymous_user()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        AuthController sut = CreateSut(context).WithContext(MvcTestHarness.Anonymous());

        var view = Assert.IsType<ViewResult>(sut.Login(returnUrl: null));
        Assert.IsType<LoginViewModel>(view.Model);
    }

    [Fact]
    public void Login_get_redirects_authenticated_admin_to_dashboard()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        AuthController sut = CreateSut(context).WithContext(MvcTestHarness.Admin());

        var redirect = Assert.IsType<RedirectToActionResult>(sut.Login(returnUrl: null));
        Assert.Equal("Dashboard", redirect.ActionName);
    }

    [Fact]
    public async Task Login_post_invalid_modelstate_returns_view()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        AuthController sut = CreateSut(context).WithContext(MvcTestHarness.Anonymous());
        sut.ModelState.AddModelError("Email", "required");

        Assert.IsType<ViewResult>(await sut.Login(new LoginViewModel()));
    }

    [Fact]
    public async Task Login_post_wrong_credentials_adds_model_error()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        AuthController sut = CreateSut(context).WithContext(MvcTestHarness.Anonymous());

        var result = await sut.Login(new LoginViewModel { Email = "ghost@wgbs.ch", Password = "nope" });

        Assert.IsType<ViewResult>(result);
        Assert.False(sut.ModelState.IsValid);
    }

    [Fact]
    public async Task Login_post_success_signs_in_and_redirects_admin()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        var hasher = new Pbkdf2PasswordHasher();
        context.Admins.Add(new Admin
        {
            Email = "admin@wgbs.ch",
            FirstName = "Sys",
            LastName = "Admin",
            PasswordHash = hasher.Hash("Secret123!"),
            Role = UserRole.Admin
        });
        await context.SaveChangesAsync();

        var fake = new FakeAuthenticationService();
        AuthController sut = CreateSut(context).WithContext(MvcTestHarness.Anonymous(), AuthServices(fake));

        var result = await sut.Login(new LoginViewModel { Email = "admin@wgbs.ch", Password = "Secret123!" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Dashboard", redirect.ActionName);
        Assert.NotNull(fake.SignedInPrincipal);
    }

    [Fact]
    public async Task Logout_signs_out_and_redirects_to_login()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        var fake = new FakeAuthenticationService();
        AuthController sut = CreateSut(context).WithContext(MvcTestHarness.Admin(), AuthServices(fake));

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.Logout());
        Assert.Equal(nameof(AuthController.Login), redirect.ActionName);
        Assert.True(fake.SignedOut);
    }
}
