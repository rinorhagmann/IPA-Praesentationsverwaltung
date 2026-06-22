using System.Security.Cryptography;
using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using IPA_Praesentationsverwaltung.Services.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IPA_Praesentationsverwaltung.Services;

/// <summary>Entity Framework backed implementation of <see cref="IStudentService"/>.</summary>
public sealed class StudentService : IStudentService
{
    // Characters that avoid visual ambiguity (no O/0, I/l/1) for printed credentials.
    private const string PasswordAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
    private const int GeneratedPasswordLength = 10;

    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public StudentService(ApplicationDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<Student>> GetAllStudentsAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Students
            .AsNoTracking()
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync(cancellationToken);

    public Task<Student?> GetStudentByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.Students.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<string> CreateStudentAsync(
        Student student, string? plainPassword = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(student);

        string password = plainPassword ?? GenerateInitialPassword();
        student.Email = student.Email.Trim().ToLowerInvariant();
        student.Role = UserRole.Student;
        student.PasswordHash = _passwordHasher.Hash(password);
        student.CreatedAt = DateTime.UtcNow;

        _dbContext.Students.Add(student);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return password;
    }

    public async Task UpdateStudentAsync(
        Student student, string? newPlainPassword = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(student);

        Student existing = await _dbContext.Students.FirstOrDefaultAsync(s => s.Id == student.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Student {student.Id} was not found.");

        existing.FirstName = student.FirstName;
        existing.LastName = student.LastName;
        existing.Email = student.Email.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(newPlainPassword))
        {
            existing.PasswordHash = _passwordHasher.Hash(newPlainPassword);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteStudentAsync(int id, CancellationToken cancellationToken = default)
    {
        Student? student = await _dbContext.Students.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (student is null)
        {
            return;
        }

        _dbContext.Students.Remove(student);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public string GenerateInitialPassword()
    {
        // Uses a cryptographically secure RNG with rejection of biased values.
        char[] buffer = new char[GeneratedPasswordLength];
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = PasswordAlphabet[RandomNumberGenerator.GetInt32(PasswordAlphabet.Length)];
        }

        return new string(buffer);
    }

    public async Task<IReadOnlyList<Student>> GetStudentsWithoutSentCredentialsAsync(
        CancellationToken cancellationToken = default) =>
        await _dbContext.Students
            .Where(s => !s.InitialPasswordSent)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync(cancellationToken);

    public async Task<string> ResetPasswordAsync(int studentId, CancellationToken cancellationToken = default)
    {
        Student student = await _dbContext.Students.FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken)
            ?? throw new InvalidOperationException($"Student {studentId} was not found.");

        string password = GenerateInitialPassword();
        student.PasswordHash = _passwordHasher.Hash(password);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return password;
    }

    public async Task MarkCredentialsSentAsync(int studentId, CancellationToken cancellationToken = default)
    {
        Student student = await _dbContext.Students.FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken)
            ?? throw new InvalidOperationException($"Student {studentId} was not found.");

        student.InitialPasswordSent = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
