using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Services;
using IPA_Praesentationsverwaltung.Tests.TestSupport;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Services;

public class NotificationServiceTests
{
    private static (NotificationService sut, FakeEmailSender mail) CreateSut()
    {
        var mail = new FakeEmailSender();
        return (new NotificationService(mail), mail);
    }

    private static Student SampleStudent() =>
        new() { Email = "anna@wgbs.ch", FirstName = "Anna", LastName = "Muster", PasswordHash = "x" };

    private static Presentation SamplePresentation() => new()
    {
        Topic = "Digitalisierung",
        StartsAt = new DateTime(2026, 9, 1, 9, 0, 0),
        Room = new Room { Name = "411" }
    };

    [Fact]
    public async Task SendCredentials_addresses_student_and_contains_password()
    {
        (NotificationService sut, FakeEmailSender mail) = CreateSut();

        await sut.SendCredentialsAsync(SampleStudent(), "Pa55word");

        FakeEmailSender.SentEmail msg = Assert.Single(mail.Sent);
        Assert.Equal("anna@wgbs.ch", msg.Recipient);
        Assert.Equal("Ihre Zugangsdaten", msg.Subject);
        Assert.Contains("Pa55word", msg.Body);
        Assert.Contains("Anna Muster", msg.Body);
    }

    [Fact]
    public async Task SendConfirmation_lists_every_selected_presentation()
    {
        (NotificationService sut, FakeEmailSender mail) = CreateSut();

        await sut.SendConfirmationAsync(SampleStudent(), new[] { SamplePresentation() });

        FakeEmailSender.SentEmail msg = Assert.Single(mail.Sent);
        Assert.Equal("Bestätigung Ihrer Präsentationsauswahl", msg.Subject);
        Assert.Contains("Digitalisierung", msg.Body);
        Assert.Contains("411", msg.Body);
    }

    [Fact]
    public async Task SendAdminNotification_includes_custom_message()
    {
        (NotificationService sut, FakeEmailSender mail) = CreateSut();
        var admin = new Admin { Email = "admin@wgbs.ch", FirstName = "Sys", LastName = "Admin" };

        await sut.SendAdminNotificationAsync(admin, "Bitte prüfen.");

        FakeEmailSender.SentEmail msg = Assert.Single(mail.Sent);
        Assert.Equal("admin@wgbs.ch", msg.Recipient);
        Assert.Contains("Bitte prüfen.", msg.Body);
    }

    [Fact]
    public async Task SendSelectionChangedByAdmin_handles_empty_selection()
    {
        (NotificationService sut, FakeEmailSender mail) = CreateSut();

        await sut.SendSelectionChangedByAdminAsync(SampleStudent(), Array.Empty<Presentation>());

        FakeEmailSender.SentEmail msg = Assert.Single(mail.Sent);
        Assert.Contains("keine Präsentation", msg.Body);
    }

    [Fact]
    public async Task SendSelectionChangedByAdmin_lists_current_selection()
    {
        (NotificationService sut, FakeEmailSender mail) = CreateSut();

        await sut.SendSelectionChangedByAdminAsync(SampleStudent(), new[] { SamplePresentation() });

        FakeEmailSender.SentEmail msg = Assert.Single(mail.Sent);
        Assert.Contains("aktuelle Auswahl", msg.Body);
        Assert.Contains("Digitalisierung", msg.Body);
    }

    [Fact]
    public async Task SendCredentials_validates_arguments()
    {
        (NotificationService sut, _) = CreateSut();

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.SendCredentialsAsync(null!, "pw"));
    }
}
