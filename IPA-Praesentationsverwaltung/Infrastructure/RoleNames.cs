namespace IPA_Praesentationsverwaltung.Infrastructure;

/// <summary>
/// Central definition of the authorization role names so controllers and the
/// authentication setup never rely on magic strings.
/// </summary>
public static class RoleNames
{
    public const string Admin = nameof(Models.Domain.UserRole.Admin);
    public const string Student = nameof(Models.Domain.UserRole.Student);
}
