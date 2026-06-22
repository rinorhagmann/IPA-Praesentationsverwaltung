using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace IPA_Praesentationsverwaltung.Services.Infrastructure;

/// <summary>
/// Password hasher based on PBKDF2 (RFC 2898) using HMAC-SHA256, a per-password
/// random salt and a high iteration count. The format stored in the database is
/// <c>{iterations}.{base64Salt}.{base64Subkey}</c>, which keeps the parameters
/// alongside the hash so they can evolve over time.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSizeInBytes = 128 / 8;     // 16 bytes
    private const int SubkeySizeInBytes = 256 / 8;    // 32 bytes
    private const int DefaultIterations = 100_000;
    private const char SegmentSeparator = '.';

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeInBytes);
        byte[] subkey = DeriveKey(password, salt, DefaultIterations);

        return string.Join(SegmentSeparator,
            DefaultIterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(subkey));
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        string[] segments = hash.Split(SegmentSeparator);
        if (segments.Length != 3 ||
            !int.TryParse(segments[0], out int iterations))
        {
            return false;
        }

        try
        {
            byte[] salt = Convert.FromBase64String(segments[1]);
            byte[] expectedSubkey = Convert.FromBase64String(segments[2]);
            byte[] actualSubkey = DeriveKey(password, salt, iterations);

            // Fixed-time comparison to avoid leaking information through timing.
            return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
        }
        catch (FormatException)
        {
            // A malformed stored hash can never match a password.
            return false;
        }
    }

    private static byte[] DeriveKey(string password, byte[] salt, int iterations) =>
        KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: iterations,
            numBytesRequested: SubkeySizeInBytes);
}
