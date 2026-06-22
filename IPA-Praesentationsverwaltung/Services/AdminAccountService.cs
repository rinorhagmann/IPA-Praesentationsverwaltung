using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using IPA_Praesentationsverwaltung.Services.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IPA_Praesentationsverwaltung.Services;

/// <summary>Entity Framework backed implementation of <see cref="IAdminAccountService"/>.</summary>
public sealed class AdminAccountService : IAdminAccountService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public AdminAccountService(ApplicationDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<Admin>> GetAllAdminsAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Admins
            .AsNoTracking()
            .OrderBy(a => a.LastName).ThenBy(a => a.FirstName)
            .ToListAsync(cancellationToken);

    public Task<Admin?> GetAdminByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.Admins.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task CreateAdminAsync(Admin admin, string plainPassword, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(admin);
        ArgumentException.ThrowIfNullOrWhiteSpace(plainPassword);

        admin.Email = admin.Email.Trim().ToLowerInvariant();
        admin.Role = UserRole.Admin;
        admin.PasswordHash = _passwordHasher.Hash(plainPassword);
        admin.CreatedAt = DateTime.UtcNow;

        _dbContext.Admins.Add(admin);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAdminAsync(Admin admin, string? newPlainPassword, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(admin);

        Admin existing = await _dbContext.Admins.FirstOrDefaultAsync(a => a.Id == admin.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Admin {admin.Id} was not found.");

        existing.FirstName = admin.FirstName;
        existing.LastName = admin.LastName;
        existing.Email = admin.Email.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(newPlainPassword))
        {
            existing.PasswordHash = _passwordHasher.Hash(newPlainPassword);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        // Refuse to delete the final administrator to keep the system manageable.
        if (await _dbContext.Admins.CountAsync(cancellationToken) <= 1)
        {
            return false;
        }

        Admin? admin = await _dbContext.Admins.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (admin is null)
        {
            return false;
        }

        _dbContext.Admins.Remove(admin);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
