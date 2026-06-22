using IPA_Praesentationsverwaltung.Models.Domain;

namespace IPA_Praesentationsverwaltung.Services.Abstractions;

/// <summary>CRUD operations for administrator accounts.</summary>
public interface IAdminAccountService
{
    Task<IReadOnlyList<Admin>> GetAllAdminsAsync(CancellationToken cancellationToken = default);

    Task<Admin?> GetAdminByIdAsync(int id, CancellationToken cancellationToken = default);

    Task CreateAdminAsync(Admin admin, string plainPassword, CancellationToken cancellationToken = default);

    Task UpdateAdminAsync(Admin admin, string? newPlainPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an administrator. Returns false (and keeps the account) when it
    /// would remove the last administrator, so the system never locks itself out.
    /// </summary>
    Task<bool> DeleteAdminAsync(int id, CancellationToken cancellationToken = default);
}
