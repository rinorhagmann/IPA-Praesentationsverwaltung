namespace IPA_Praesentationsverwaltung.Services.Infrastructure;

/// <summary>
/// Abstraction over the password hashing primitive so that services depend on
/// the contract rather than a concrete cryptographic implementation.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Creates a salted, iterated hash of the given plaintext password.</summary>
    string Hash(string password);

    /// <summary>
    /// Verifies a plaintext password against a previously created hash in a way
    /// that is resistant to timing attacks.
    /// </summary>
    bool Verify(string password, string hash);
}
