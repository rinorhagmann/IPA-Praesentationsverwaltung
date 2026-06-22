using IPA_Praesentationsverwaltung.Services.Infrastructure;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Services;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _sut = new();

    [Fact]
    public void Verify_returns_true_for_the_correct_password()
    {
        string hash = _sut.Hash("S3cret-Pass!");
        Assert.True(_sut.Verify("S3cret-Pass!", hash));
    }

    [Fact]
    public void Verify_returns_false_for_a_wrong_password()
    {
        string hash = _sut.Hash("S3cret-Pass!");
        Assert.False(_sut.Verify("wrong-password", hash));
    }

    [Fact]
    public void Hash_uses_a_random_salt_so_hashes_differ()
    {
        string first = _sut.Hash("samePassword");
        string second = _sut.Hash("samePassword");

        Assert.NotEqual(first, second);
        Assert.True(_sut.Verify("samePassword", first));
        Assert.True(_sut.Verify("samePassword", second));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-valid-hash")]
    [InlineData("1.2.3.4")]
    public void Verify_returns_false_for_malformed_hashes(string malformed)
    {
        Assert.False(_sut.Verify("anything", malformed));
    }
}
