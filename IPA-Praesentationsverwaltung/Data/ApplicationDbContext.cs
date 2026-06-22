using IPA_Praesentationsverwaltung.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace IPA_Praesentationsverwaltung.Data;

/// <summary>
/// Entity Framework Core database context. Maps the domain model to the MSSQL
/// schema using a table-per-hierarchy strategy for the <see cref="User"/>
/// inheritance hierarchy and enforces the relational constraints required for a
/// third-normal-form schema.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Presentation> Presentations => Set<Presentation>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Registration> Registrations => Set<Registration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUsers(modelBuilder);
        ConfigureRooms(modelBuilder);
        ConfigurePresentations(modelBuilder);
        ConfigureRegistrations(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            // Table-per-hierarchy: a single Users table discriminated by Role.
            entity.HasDiscriminator(u => u.Role)
                  .HasValue<Student>(UserRole.Student)
                  .HasValue<Admin>(UserRole.Admin);

            // E-mail addresses act as the natural login identifier and must be unique.
            entity.HasIndex(u => u.Email).IsUnique();
        });
    }

    private static void ConfigureRooms(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>(entity =>
        {
            // A room is recorded only once; its name is unique to avoid duplicates.
            entity.HasIndex(r => r.Name).IsUnique();
        });
    }

    private static void ConfigurePresentations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Presentation>(entity =>
        {
            entity.HasOne(p => p.Room)
                  .WithMany(r => r.Presentations)
                  .HasForeignKey(p => p.RoomId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureRegistrations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Registration>(entity =>
        {
            entity.HasOne(reg => reg.Student)
                  .WithMany(s => s.Registrations)
                  .HasForeignKey(reg => reg.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(reg => reg.Presentation)
                  .WithMany(p => p.Registrations)
                  .HasForeignKey(reg => reg.PresentationId)
                  .OnDelete(DeleteBehavior.Cascade);

            // A student may register for a given presentation only once.
            entity.HasIndex(reg => new { reg.StudentId, reg.PresentationId }).IsUnique();
        });
    }
}
