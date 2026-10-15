using Microsoft.EntityFrameworkCore;

namespace GreenInk;

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
}

public class Plant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
}

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Viewer";
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public List<UserPlant> Plants { get; set; } = [];
}

public class UserPlant
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid PlantId { get; set; }
    public Plant Plant { get; set; } = null!;
}

public class UserSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTimeOffset ExpiresAt { get; set; }
}

public class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid ActorId { get; set; }
    public Guid? TenantId { get; set; }
    public string Action { get; set; } = "";
    public Guid EntityId { get; set; }
}

public class FactoryDb(DbContextOptions<FactoryDb> options) : DbContext(options)
{
    public DbSet<UserSession> Sessions => Set<UserSession>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Plant> Plants => Set<Plant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserPlant> UserPlants => Set<UserPlant>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<UserSession>().HasIndex(x => x.ExpiresAt);
        b.Entity<Tenant>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Plant>().HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Tenant>().Property(x => x.Name).HasMaxLength(160);
        b.Entity<Tenant>().Property(x => x.Code).HasMaxLength(32);
        b.Entity<Plant>().Property(x => x.Name).HasMaxLength(160);
        b.Entity<Plant>().Property(x => x.Code).HasMaxLength(32);
        b.Entity<UserPlant>().HasKey(x => new { x.UserId, x.PlantId });
        b.Entity<UserPlant>()
            .HasOne(x => x.User)
            .WithMany(x => x.Plants)
            .HasForeignKey(x => x.UserId);
        b.Entity<UserPlant>()
            .HasOne(x => x.Plant)
            .WithMany()
            .HasForeignKey(x => x.PlantId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<User>()
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<Plant>()
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<AuditEvent>().HasIndex(x => new { x.TenantId, x.CreatedAt });
    }
}

public record LoginRequest(string Email, string Password);

public record TenantRequest(string Name, string Code);

public record PlantRequest(string Name, string Code, Guid TenantId);

public record UserRequest(
    string Email,
    string DisplayName,
    string Password,
    string Role,
    Guid? TenantId,
    Guid[] PlantIds
);

public static class Rules
{
    public static readonly string[] Roles =
    [
        "PlatformAdmin",
        "TenantAdmin",
        "PlantManager",
        "Operator",
        "Viewer",
    ];

    public static bool ValidName(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 160;

    public static bool ValidCode(string? value) =>
        value is not null
        && System.Text.RegularExpressions.Regex.IsMatch(value, "^[A-Za-z0-9_-]{1,32}$");

    public static bool ValidPassword(string? value) =>
        value is not null
        && value.Length >= 14
        && value.Length <= 128
        && value.Any(char.IsUpper)
        && value.Any(char.IsLower)
        && value.Any(char.IsDigit);
}

public record AccessRequest(string Role, Guid[] PlantIds);

public record PasswordRequest(string CurrentPassword, string NewPassword);
