using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Services;
using IPA_Praesentationsverwaltung.Services.Infrastructure;
using IPA_Praesentationsverwaltung.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Services;

public class StudentServiceTests
{
    private static StudentService CreateSut(ApplicationDbContext context) =>
        new(context, new Pbkdf2PasswordHasher());

    private static Student NewStudent(string email = "anna@wgbs.ch", string first = "Anna", string last = "Muster") =>
        new() { Email = email, FirstName = first, LastName = last };

    [Fact]
    public async Task CreateStudent_generates_password_normalises_email_and_sets_role()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentService sut = CreateSut(context);

        string password = await sut.CreateStudentAsync(NewStudent(email: " Anna@WGBS.CH "));

        Student stored = await context.Students.SingleAsync();
        Assert.Equal(10, password.Length);
        Assert.Equal("anna@wgbs.ch", stored.Email);
        Assert.Equal(UserRole.Student, stored.Role);
        Assert.NotEqual(password, stored.PasswordHash);
    }

    [Fact]
    public async Task CreateStudent_uses_supplied_password()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentService sut = CreateSut(context);

        string password = await sut.CreateStudentAsync(NewStudent(), "Chosen123");

        Assert.Equal("Chosen123", password);
    }

    [Fact]
    public async Task GetAllStudents_orders_by_last_then_first_name()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentService sut = CreateSut(context);
        await sut.CreateStudentAsync(NewStudent("z@wgbs.ch", "Anna", "Zwygart"));
        await sut.CreateStudentAsync(NewStudent("a@wgbs.ch", "Bea", "Amsler"));

        IReadOnlyList<Student> all = await sut.GetAllStudentsAsync();

        Assert.Equal("Amsler", all[0].LastName);
    }

    [Fact]
    public async Task GetStudentById_returns_match_or_null()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentService sut = CreateSut(context);
        await sut.CreateStudentAsync(NewStudent());
        Student created = await context.Students.SingleAsync();

        Assert.NotNull(await sut.GetStudentByIdAsync(created.Id));
        Assert.Null(await sut.GetStudentByIdAsync(9999));
    }

    [Fact]
    public async Task UpdateStudent_changes_fields_and_keeps_password_when_blank()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentService sut = CreateSut(context);
        await sut.CreateStudentAsync(NewStudent(), "orig");
        Student created = await context.Students.SingleAsync();
        string originalHash = created.PasswordHash;

        await sut.UpdateStudentAsync(
            new Student { Id = created.Id, Email = "NEW@wgbs.ch", FirstName = "N", LastName = "N" });

        Student updated = await context.Students.SingleAsync();
        Assert.Equal("new@wgbs.ch", updated.Email);
        Assert.Equal(originalHash, updated.PasswordHash);
    }

    [Fact]
    public async Task UpdateStudent_replaces_password_when_supplied()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentService sut = CreateSut(context);
        await sut.CreateStudentAsync(NewStudent(), "orig");
        Student created = await context.Students.SingleAsync();
        string originalHash = created.PasswordHash;

        await sut.UpdateStudentAsync(
            new Student { Id = created.Id, Email = "anna@wgbs.ch", FirstName = "Anna", LastName = "Muster" }, "changed");

        Assert.NotEqual(originalHash, (await context.Students.SingleAsync()).PasswordHash);
    }

    [Fact]
    public async Task UpdateStudent_throws_when_missing()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentService sut = CreateSut(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.UpdateStudentAsync(new Student { Id = 99, Email = "x@y.ch", FirstName = "A", LastName = "B" }));
    }

    [Fact]
    public async Task DeleteStudent_removes_existing_and_ignores_missing()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentService sut = CreateSut(context);
        await sut.CreateStudentAsync(NewStudent());
        Student created = await context.Students.SingleAsync();

        await sut.DeleteStudentAsync(9999);
        Assert.Equal(1, await context.Students.CountAsync());

        await sut.DeleteStudentAsync(created.Id);
        Assert.Equal(0, await context.Students.CountAsync());
    }

    [Fact]
    public void GenerateInitialPassword_has_fixed_length_and_avoids_ambiguous_characters()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentService sut = CreateSut(context);

        for (int i = 0; i < 50; i++)
        {
            string pw = sut.GenerateInitialPassword();
            Assert.Equal(10, pw.Length);
            Assert.DoesNotContain(pw, c => "O0Il1".Contains(c));
        }
    }

    [Fact]
    public async Task GetStudentsWithoutSentCredentials_filters_and_orders()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentService sut = CreateSut(context);
        await sut.CreateStudentAsync(NewStudent("a@wgbs.ch", "Anna", "Amsler"));
        await sut.CreateStudentAsync(NewStudent("b@wgbs.ch", "Bea", "Berger"));
        Student sent = await context.Students.FirstAsync(s => s.LastName == "Berger");
        sent.InitialPasswordSent = true;
        await context.SaveChangesAsync();

        IReadOnlyList<Student> pending = await sut.GetStudentsWithoutSentCredentialsAsync();

        Student only = Assert.Single(pending);
        Assert.Equal("Amsler", only.LastName);
    }

    [Fact]
    public async Task ResetPassword_sets_new_hash_and_returns_plaintext()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentService sut = CreateSut(context);
        await sut.CreateStudentAsync(NewStudent(), "orig");
        Student created = await context.Students.SingleAsync();
        string oldHash = created.PasswordHash;

        string pw = await sut.ResetPasswordAsync(created.Id);

        Assert.Equal(10, pw.Length);
        Assert.NotEqual(oldHash, (await context.Students.SingleAsync()).PasswordHash);
    }

    [Fact]
    public async Task ResetPassword_throws_when_missing()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentService sut = CreateSut(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ResetPasswordAsync(99));
    }

    [Fact]
    public async Task MarkCredentialsSent_sets_flag()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentService sut = CreateSut(context);
        await sut.CreateStudentAsync(NewStudent());
        Student created = await context.Students.SingleAsync();

        await sut.MarkCredentialsSentAsync(created.Id);

        Assert.True((await context.Students.SingleAsync()).InitialPasswordSent);
    }

    [Fact]
    public async Task MarkCredentialsSent_throws_when_missing()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentService sut = CreateSut(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.MarkCredentialsSentAsync(99));
    }

    [Fact]
    public async Task MarkSelectionConfirmed_sets_timestamp_once_and_is_idempotent()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentService sut = CreateSut(context);
        await sut.CreateStudentAsync(NewStudent());
        Student created = await context.Students.SingleAsync();

        await sut.MarkSelectionConfirmedAsync(created.Id);
        DateTime? first = (await context.Students.SingleAsync()).SelectionConfirmedAt;
        Assert.NotNull(first);

        await sut.MarkSelectionConfirmedAsync(created.Id);
        DateTime? second = (await context.Students.SingleAsync()).SelectionConfirmedAt;
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task MarkSelectionConfirmed_throws_when_missing()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentService sut = CreateSut(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.MarkSelectionConfirmedAsync(99));
    }
}
