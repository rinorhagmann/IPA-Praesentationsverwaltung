namespace IPA_Praesentationsverwaltung.Models.Domain;

/// <summary>
/// An administrator account. Administrators import data, manage the domain
/// entities, trigger notifications and reset the system at the start of a year.
/// </summary>
public class Admin : User
{
    /// <summary>
    /// Whether this administrator may perform privileged system operations
    /// such as resetting the school year. Reserved for future fine-grained rights.
    /// </summary>
    public bool CanManageSystem { get; set; } = true;
}
