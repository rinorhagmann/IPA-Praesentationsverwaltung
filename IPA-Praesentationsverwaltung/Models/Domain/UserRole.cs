namespace IPA_Praesentationsverwaltung.Models.Domain;

/// <summary>
/// Distinguishes the two account types handled by the application.
/// Used as the Entity Framework discriminator for the table-per-hierarchy
/// mapping of <see cref="User"/> and its derived types.
/// </summary>
public enum UserRole
{
    /// <summary>A G3 student who registers as an observer for presentations.</summary>
    Student = 0,

    /// <summary>An administrator who manages the system.</summary>
    Admin = 1
}
