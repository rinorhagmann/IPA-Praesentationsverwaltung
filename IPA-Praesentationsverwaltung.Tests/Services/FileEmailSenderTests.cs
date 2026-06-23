using IPA_Praesentationsverwaltung.Services.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Services;

public class FileEmailSenderTests
{
    [Fact]
    public async Task SendAsync_writes_an_eml_file_to_the_outbox()
    {
        string outbox = Path.Combine(Path.GetTempPath(), "wgbs-mail-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var options = Options.Create(new EmailOptions { OutboxPath = outbox });
            var sut = new FileEmailSender(options, NullLogger<FileEmailSender>.Instance);

            await sut.SendAsync("anna@wgbs.ch", "Betreff", "Hallo Welt");

            string file = Assert.Single(Directory.GetFiles(outbox, "*.eml"));
            string content = await File.ReadAllTextAsync(file);
            Assert.Contains("To: anna@wgbs.ch", content);
            Assert.Contains("Subject: Betreff", content);
            Assert.Contains("Hallo Welt", content);
        }
        finally
        {
            if (Directory.Exists(outbox))
            {
                Directory.Delete(outbox, recursive: true);
            }
        }
    }

    [Fact]
    public void EmailOptions_reports_smtp_configuration_state()
    {
        Assert.False(new EmailOptions().IsSmtpConfigured);
        Assert.True(new EmailOptions { Smtp = new SmtpOptions { Host = "smtp.example.com" } }.IsSmtpConfigured);
    }
}
