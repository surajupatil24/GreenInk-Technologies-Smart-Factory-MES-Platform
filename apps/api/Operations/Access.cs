using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace GreenInk;

public record OpsScope(Guid TenantId, Guid PlantId, Guid ActorId, string Role);

public sealed class OpsError(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

public static class Ops
{
    public static async Task<OpsScope> Require(
        FactoryDb db,
        ClaimsPrincipal p,
        Guid plantId,
        bool write = false,
        bool manager = false
    )
    {
        if (!Guid.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            throw new OpsError(401, "Sign in first.");
        var user = await db.Users.Include(x => x.Plants).SingleAsync(x => x.Id == id);
        var plant = await db.Plants.AsNoTracking().SingleOrDefaultAsync(x => x.Id == plantId);
        if (
            plant is null
            || !(
                user.Role == "PlatformAdmin"
                || user.TenantId == plant.TenantId
                    && (user.Role == "TenantAdmin" || user.Plants.Any(x => x.PlantId == plantId))
            )
        )
            throw new OpsError(403, "Plant access denied.");
        if (
            write && user.Role == "Viewer"
            || manager && user.Role is not ("PlatformAdmin" or "TenantAdmin" or "PlantManager")
        )
            throw new OpsError(403, "Your role cannot perform this operation.");
        return new(plant.TenantId, plant.Id, id, user.Role);
    }

    public static IQueryable<T> Rows<T>(FactoryDb db, OpsScope s)
        where T : class, IPlantRow =>
        db.Set<T>().Where(x => x.TenantId == s.TenantId && x.PlantId == s.PlantId);

    public static T Stamp<T>(T row, OpsScope s)
        where T : IPlantRow
    {
        row.TenantId = s.TenantId;
        row.PlantId = s.PlantId;
        return row;
    }

    public static void Audit(FactoryDb db, OpsScope s, string action, Guid id) =>
        db.AuditEvents.Add(
            new AuditEvent
            {
                ActorId = s.ActorId,
                TenantId = s.TenantId,
                Action = action,
                EntityId = id,
            }
        );

    public static void Check(bool condition, string message)
    {
        if (!condition)
            throw new OpsError(400, message);
    }

    public static bool Positive(decimal q) => q > 0 && q <= 1000000000m && decimal.Round(q, 6) == q;

    public static string Code(string? value)
    {
        Check(
            Rules.ValidCode(value),
            "Code must contain 1–32 letters, digits, hyphens or underscores."
        );
        return value!.ToUpperInvariant();
    }

    public static string Name(string? value)
    {
        Check(Rules.ValidName(value), "A name of 1–160 characters is required.");
        return value!.Trim();
    }

    public static void Command(Guid id) =>
        Check(id != Guid.Empty, "A unique commandId is required for retry-safe posting.");
}
