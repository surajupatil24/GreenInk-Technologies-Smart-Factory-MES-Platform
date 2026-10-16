using System.Data;
using Microsoft.EntityFrameworkCore;

namespace GreenInk;

// Gate coordination is deliberately separate from inventory posting; only a receipt moves stock.
public static class ReceivingEndpoints
{
    public record PolicyInput(Guid PlantId, bool RequireTruck, bool RequireDeliveryNote);

    public record DockInput(Guid PlantId, string Code, string Name, bool Maintenance);

    public record CheckInput(
        Guid PlantId,
        string Prompt,
        string Stage,
        bool Required,
        bool RequirePass,
        bool Active
    );

    public record VisitInput(
        Guid PlantId,
        string TruckNumber,
        string DriverName,
        DateTimeOffset ScheduledAt,
        Guid[] PurchaseOrderIds,
        Guid CommandId
    );

    public record TransitionInput(Guid PlantId, string Status, Guid? DockStationId);

    public record AnswerInput(Guid PlantId, bool Passed, string? Notes);

    public static void MapReceiving(this WebApplication app)
    {
        var g = app.MapGroup("/api/operations/receiving").RequireAuthorization();
        g.MapGet(
            "/policy",
            async (Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                return Results.Ok(
                    await Ops.Rows<ReceivingPolicy>(db, s).AsNoTracking().SingleOrDefaultAsync()
                        ?? new ReceivingPolicy { PlantId = plantId, TenantId = s.TenantId }
                );
            }
        );
        g.MapPost(
            "/policy",
            async (PolicyInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true, true);
                var row = await Ops.Rows<ReceivingPolicy>(db, s).SingleOrDefaultAsync();
                if (row is null)
                {
                    row = Ops.Stamp(new ReceivingPolicy(), s);
                    db.Add(row);
                }
                row.RequireTruck = r.RequireTruck;
                row.RequireDeliveryNote = r.RequireDeliveryNote;
                Ops.Audit(db, s, "ReceivingPolicyChanged", row.Id);
                await db.SaveChangesAsync();
                return Results.Ok(row);
            }
        );
        g.MapGet(
            "/docks",
            async (Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                return Results.Ok(
                    await Ops.Rows<DockStation>(db, s)
                        .AsNoTracking()
                        .OrderBy(x => x.Code)
                        .Take(200)
                        .ToListAsync()
                );
            }
        );
        g.MapPost(
            "/docks",
            async (DockInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true, true);
                var code = Ops.Code(r.Code);
                var row = await Ops.Rows<DockStation>(db, s)
                    .SingleOrDefaultAsync(x => x.Code == code);
                if (row is null)
                {
                    row = Ops.Stamp(new DockStation { Code = code }, s);
                    db.Add(row);
                }
                row.Name = Ops.Name(r.Name);
                row.Maintenance = r.Maintenance;
                Ops.Audit(db, s, "ReceivingDockSaved", row.Id);
                await db.SaveChangesAsync();
                return Results.Ok(row);
            }
        );
        g.MapGet(
            "/checks",
            async (Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                return Results.Ok(
                    await Ops.Rows<ReceivingCheck>(db, s)
                        .AsNoTracking()
                        .OrderBy(x => x.CreatedAt)
                        .Take(200)
                        .ToListAsync()
                );
            }
        );
        g.MapPost(
            "/checks",
            async (CheckInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true, true);
                Ops.Check(
                    r.Stage is "Arrival" or "Unloading",
                    "Checklist stage must be Arrival or Unloading."
                );
                var prompt = Ops.Name(r.Prompt);
                var row = await Ops.Rows<ReceivingCheck>(db, s)
                    .SingleOrDefaultAsync(x => x.Prompt == prompt && x.Stage == r.Stage);
                if (row is null)
                {
                    row = Ops.Stamp(new ReceivingCheck { Prompt = prompt, Stage = r.Stage }, s);
                    db.Add(row);
                }
                row.Required = r.Required;
                row.RequirePass = r.RequirePass;
                row.Active = r.Active;
                Ops.Audit(db, s, "ReceivingCheckSaved", row.Id);
                await db.SaveChangesAsync();
                return Results.Ok(row);
            }
        );
        g.MapGet(
            "/visits",
            async (Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                return Results.Ok(
                    await Ops.Rows<TruckVisit>(db, s)
                        .AsNoTracking()
                        .OrderByDescending(x => x.ScheduledAt)
                        .Take(200)
                        .ToListAsync()
                );
            }
        );
        g.MapPost(
            "/visits",
            async (VisitInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true);
                Ops.Command(r.CommandId);
                var truck = Ops.Code(r.TruckNumber);
                var driver = Ops.Name(r.DriverName);
                // PostgreSQL timestamps have microsecond precision. Normalize before hashing/replay comparison.
                var scheduled = new DateTimeOffset(
                    r.ScheduledAt.UtcTicks - r.ScheduledAt.UtcTicks % 10,
                    TimeSpan.Zero
                );
                Ops.Check(
                    r.PurchaseOrderIds is { Length: > 0 and <= 50 },
                    "Select between one and fifty purchase orders."
                );
                var ids = r.PurchaseOrderIds.Distinct().Order().ToArray();
                Ops.Check(
                    ids.Length == r.PurchaseOrderIds.Length,
                    "Duplicate purchase orders are not allowed."
                );
                Ops.Check(
                    scheduled > DateTimeOffset.UtcNow.AddYears(-1)
                        && scheduled < DateTimeOffset.UtcNow.AddYears(2),
                    "Arrival date must be within the scheduling window."
                );
                var signature = string.Join(",", ids);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var existing = await Ops.Rows<TruckVisit>(db, s)
                    .SingleOrDefaultAsync(x => x.CommandId == r.CommandId);
                if (existing is not null)
                {
                    Ops.Check(
                        existing.TruckNumber == truck
                            && existing.DriverName == driver
                            && existing.ScheduledAt == scheduled
                            && existing.OrderSignature == signature,
                        "commandId was already used for a different visit."
                    );
                    return Results.Ok(existing);
                }
                Ops.Check(
                    await Ops.Rows<PurchaseOrder>(db, s)
                        .CountAsync(x =>
                            ids.Contains(x.Id) && (x.Status == "Open" || x.Status == "Partial")
                        ) == ids.Length,
                    "All orders must be open and belong to this plant."
                );
                var visit = Ops.Stamp(
                    new TruckVisit
                    {
                        TruckNumber = truck,
                        DriverName = driver,
                        ScheduledAt = scheduled,
                        CommandId = r.CommandId,
                        OrderSignature = signature,
                    },
                    s
                );
                db.Add(visit);
                foreach (var id in ids)
                    db.Add(
                        Ops.Stamp(
                            new TruckOrder { TruckVisitId = visit.Id, PurchaseOrderId = id },
                            s
                        )
                    );
                // Snapshot the applicable checklist. Later configuration edits cannot erase an active visit's obligations.
                foreach (
                    var check in await Ops.Rows<ReceivingCheck>(db, s)
                        .Where(x => x.Active)
                        .ToListAsync()
                )
                    db.Add(
                        Ops.Stamp(
                            new TruckCheckAnswer
                            {
                                TruckVisitId = visit.Id,
                                Prompt = check.Prompt,
                                Stage = check.Stage,
                                Required = check.Required,
                                RequirePass = check.RequirePass,
                            },
                            s
                        )
                    );
                Ops.Audit(db, s, "TruckRegistered", visit.Id);
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(visit);
            }
        );
        g.MapGet(
            "/visits/{id:guid}",
            async (Guid id, Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                var visit = await Ops.Rows<TruckVisit>(db, s)
                    .AsNoTracking()
                    .SingleOrDefaultAsync(x => x.Id == id);
                if (visit is null)
                    return Results.NotFound();
                return Results.Ok(
                    new
                    {
                        visit,
                        orders = await Ops.Rows<TruckOrder>(db, s)
                            .AsNoTracking()
                            .Where(x => x.TruckVisitId == id)
                            .ToListAsync(),
                        checks = await Ops.Rows<TruckCheckAnswer>(db, s)
                            .AsNoTracking()
                            .Where(x => x.TruckVisitId == id)
                            .OrderBy(x => x.CreatedAt)
                            .ThenBy(x => x.Id)
                            .ToListAsync(),
                    }
                );
            }
        );
        g.MapPost(
            "/visits/{id:guid}/status",
            async (Guid id, TransitionInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var visit = await Ops.Rows<TruckVisit>(db, s).SingleOrDefaultAsync(x => x.Id == id);
                if (visit is null)
                    return Results.NotFound();
                if (visit.Status == r.Status)
                {
                    Ops.Check(
                        r.Status != "Docked" || visit.DockStationId == r.DockStationId,
                        "Visit is already assigned to another dock."
                    );
                    return Results.Ok(visit);
                }
                var allowed =
                    (visit.Status, r.Status)
                    is
                        ("Expected", "Arrived")
                        or
                        ("Arrived", "Docked")
                        or
                        ("Docked", "Unloading")
                        or
                        ("Unloading", "Unloaded")
                        or
                        ("Unloaded", "Out");
                Ops.Check(allowed, $"Cannot change visit from {visit.Status} to {r.Status}.");
                if (r.Status == "Docked")
                {
                    await CheckGate(db, s, id, "Arrival");
                    var dock = await Ops.Rows<DockStation>(db, s)
                        .SingleOrDefaultAsync(x => x.Id == r.DockStationId);
                    Ops.Check(
                        dock is not null && !dock.Maintenance,
                        "Select a dock that is not under maintenance."
                    );
                    visit.DockStationId = dock!.Id; // A dock may have a queue; exclusive occupancy is checked when unloading starts.
                }
                if (r.Status == "Unloading")
                {
                    var dock = await Ops.Rows<DockStation>(db, s)
                        .SingleAsync(x => x.Id == visit.DockStationId);
                    Ops.Check(!dock.Maintenance, "Dock is under maintenance.");
                    Ops.Check(
                        !await Ops.Rows<TruckVisit>(db, s)
                            .AnyAsync(x =>
                                x.Id != id && x.DockStationId == dock.Id && x.Status == "Unloading"
                            ),
                        "Dock is unloading another truck. Wait for it to finish."
                    );
                    await CheckGate(db, s, id, "Unloading");
                }
                if (r.Status == "Unloaded")
                    await CheckGate(db, s, id, "Unloading");
                visit.Status = r.Status;
                var now = DateTimeOffset.UtcNow;
                if (r.Status == "Arrived")
                    visit.ArrivedAt = now;
                if (r.Status == "Unloading")
                    visit.UnloadingStartedAt = now;
                if (r.Status == "Unloaded")
                    visit.UnloadingCompletedAt = now;
                if (r.Status == "Out")
                    visit.ExitedAt = now;
                Ops.Audit(db, s, "Truck" + r.Status, id);
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(visit);
            }
        );
        g.MapPost(
            "/checks/{id:guid}/answer",
            async (Guid id, AnswerInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var answer = await Ops.Rows<TruckCheckAnswer>(db, s)
                    .SingleOrDefaultAsync(x => x.Id == id);
                if (answer is null)
                    return Results.NotFound();
                var visit = await Ops.Rows<TruckVisit>(db, s)
                    .SingleAsync(x => x.Id == answer.TruckVisitId);
                Ops.Check(
                    answer.Stage == "Arrival"
                        ? visit.Status is "Expected" or "Arrived"
                        : visit.Status is "Expected" or "Arrived" or "Docked",
                    "This checklist is locked after its gate has passed."
                );
                var notes = (r.Notes ?? "").Trim();
                Ops.Check(notes.Length <= 2000, "Notes must be at most 2000 characters.");
                answer.Passed = r.Passed;
                answer.Notes = notes;
                answer.AnsweredAt = DateTimeOffset.UtcNow;
                Ops.Audit(db, s, "TruckChecklistAnswered", answer.Id);
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(answer);
            }
        );
    }

    public static async Task CheckGate(FactoryDb db, OpsScope s, Guid visitId, string stage)
    {
        var blocked = await Ops.Rows<TruckCheckAnswer>(db, s)
            .Where(x =>
                x.TruckVisitId == visitId
                && x.Stage == stage
                && ((x.Required && x.Passed == null) || (x.RequirePass && x.Passed != true))
            )
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync();
        Ops.Check(
            blocked is null,
            $"Complete and pass the {stage.ToLowerInvariant()} checklist before continuing: {blocked?.Prompt}"
        );
    }
}
