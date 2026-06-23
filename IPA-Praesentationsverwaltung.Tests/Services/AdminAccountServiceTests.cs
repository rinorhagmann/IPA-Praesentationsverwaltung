using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Services;
using IPA_Praesentationsverwaltung.Services.Infrastructure;
using IPA_Praesentationsverwaltung.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Services;

public class AdminAccountServiceTests
{
    private static AdminAccountService CreateSut(ApplicationDbContext context) =>
        new(context, new Pbkdf2PasswordHasher());

    private static Admin NewAdmin(string email = "admin@wgbs.ch", string first = "Sys", string last = "Admin") =>
        new() { Email = email, FirstName = first, LastName = last };

    [Fact]
    public async Task CreateAdmin_normalises_email_sets_role_and_hashes_password()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        AdminAccountService sut = CreateSut(context);

        await sut.CreateAdminAsync(NewAdmin(email: "  Admin@WGBS.CH "), "Secret123!");

        Admin stored = await context.Admins.SingleAsync();
        Assert.Equal("admin@wgbs.ch", stored.Email);
        Assert.Equal(UserRole.Admin, stored.Role);
        Assert.NotEqual("Secret123!", stored.PasswordHash);
        Assert.NotEmpty(stored.PasswordHash);
    }

    [Fact]
    public async Task CreateAdmin_rejects_blank_password()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        AdminAccountService sut = CreateSut(context);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateAdminAsync(NewAdmin(), "   "));
    }

    [Fact]
    public async Task GetAllAdmins_orders_by_last_then_first_name()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        AdminAccountService sut = CreateSut(context);
        await sut.CreateAdminAsync(NewAdmin("b@wgbs.ch", "Anna", "Zürcher"), "x");
        await sut.CreateAdminAsync(NewAdmin("a@wgbs.ch", "Bea", "Aebi"), "x");

        IReadOnlyList<Admin> admins = await sut.GetAllAdminsAsync();

        Assert.Equal("Aebi", admins[0].LastName);
        Assert.Equal("Zürcher", admins[1].LastName);
    }

    [Fact]
    public async Task GetAdminById_returns_match_or_null()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        AdminAccountService sut = CreateSut(context);
        await sut.CreateAdminAsync(NewAdmin(), "x");
        Admin created = await context.Admins.SingleAsync();

        Assert.NotNull(await sut.GetAdminByIdAsync(created.Id));
        Assert.Null(await sut.GetAdminByIdAsync(9999));
    }

    [Fact]
    public async Task UpdateAdmin_changes_fields_and_keeps_password_when_blank()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        AdminAccountService sut = CreateSut(context);
        await sut.CreateAdminAsync(NewAdmin(), "original");
        Admin created = await context.Admins.SingleAsync();
        string originalHash = created.PasswordHash;

        await sut.UpdateAdminAsync(
            new Admin { Id = created.Id, Email = "NEW@wgbs.ch", FirstName = "Neu", LastName = "Name" },
            newPlainPassword: null);

        Admin updated = await context.Admins.SingleAsync();
        Assert.Equal("new@wgbs.ch", updated.Email);
        Assert.Equal("Neu", updated.FirstName);
        Assert.Equal(originalHash, updated.PasswordHash);
    }

    [Fact]
    public async Task UpdateAdmin_replaces_password_when_supplied()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        AdminAccountService sut = CreateSut(context);
        await sut.CreateAdminAsync(NewAdmin(), "original");
        Admin created = await context.Admins.SingleAsync();
        string originalHash = created.PasswordHash;

        await sut.UpdateAdminAsync(
            new Admin { Id = created.Id, Email = "admin@wgbs.ch", FirstName = "Sys", LastName = "Admin" },
            newPlainPassword: "changed");

        Assert.NotEqual(originalHash, (await context.Admins.SingleAsync()).PasswordHash);
    }

    [Fact]
    public async Task UpdateAdmin_throws_when_missing()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        AdminAccountService sut = CreateSut(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.UpdateAdminAsync(new Admin { Id = 123, Email = "x@y.ch", FirstName = "A", LastName = "B" }, null));
    }

    [Fact]
    public async Task DeleteAdmin_refuses_to_remove_the_last_admin()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        AdminAccountService sut = CreateSut(context);
        await sut.CreateAdminAsync(NewAdmin(), "x");
        Admin only = await context.Admins.SingleAsync();

        bool deleted = await sut.DeleteAdminAsync(only.Id);

        Assert.False(deleted);
        Assert.Equal(1, await context.Admins.CountAsync());
    }

    [Fact]
    public async Task DeleteAdmin_removes_when_more_than_one_exists()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        AdminAccountService sut = CreateSut(context);
        await sut.CreateAdminAsync(NewAdmin("a@wgbs.ch"), "x");
        await sut.CreateAdminAsync(NewAdmin("b@wgbs.ch"), "x");
        Admin victim = await context.Admins.FirstAsync();

        bool deleted = await sut.DeleteAdminAsync(victim.Id);

        Assert.True(deleted);
        Assert.Equal(1, await context.Admins.CountAsync());
    }

    [Fact]
    public async Task DeleteAdmin_returns_false_for_unknown_id()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        AdminAccountService sut = CreateSut(context);
        await sut.CreateAdminAsync(NewAdmin("a@wgbs.ch"), "x");
        await sut.CreateAdminAsync(NewAdmin("b@wgbs.ch"), "x");

        bool deleted = await sut.DeleteAdminAsync(9999);

        Assert.False(deleted);
        Assert.Equal(2, await context.Admins.CountAsync());
    }
}
