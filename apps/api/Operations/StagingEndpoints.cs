using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace GreenInk;

public static partial class StagingEndpoints
{
    public record AreaInput(
        Guid PlantId,
        string Code,
        string Name,
        Guid StagingBinId,
        Guid SupplyBinId,
        string Workflow
    );

    public record RuleInput(
        Guid PlantId,
        Guid MaterialId,
        decimal PackSize,
        bool PreweighRequired,
        decimal TolerancePercent,
        bool CorrectionRequired,
        string? SafetyNotes,
        string? HandlingNotes
    );

    public record FactorInput(Guid PlantId, Guid BatchId, decimal Factor, string Evidence);

    public record PlanInput(
        Guid PlantId,
        Guid ProductionOrderId,
        Guid StagingAreaId,
        DateTimeOffset ScheduledAt,
        bool Priority,
        bool ApprovalRequired,
        bool Approved,
        string? ApprovalNotes
    );

    public record PickInput(
        Guid PlantId,
        Guid PalletId,
        string SourceBin,
        decimal Quantity,
        decimal RequirementQuantity,
        decimal? Gross,
        decimal? Tare,
        string? ScaleReference,
        bool SafetyConfirmed,
        Guid CommandId,
        string? PalletCode = null,
        uint RuleVersion = 0,
        uint FactorVersion = 0
    );

    public record WeighInput(
        Guid PlantId,
        decimal Gross,
        decimal Tare,
        string? ScaleReference,
        Guid CommandId
    );

    public record KitMoveInput(Guid PlantId, string KitCode, string DestinationBin, Guid CommandId);

    public record ReturnInput(Guid PlantId, string DestinationBin, string Reason, Guid CommandId);

    public record TransferInput(
        Guid PlantId,
        Guid PalletId,
        string SourceBin,
        string DestinationBin,
        decimal Quantity,
        string Reason,
        Guid CommandId
    );

    public record RequestInput(
        Guid PlantId,
        Guid MaterialId,
        Guid DestinationBinId,
        Guid? StagingPlanId,
        decimal Quantity,
        string Kind,
        string Reason,
        Guid CommandId
    );

    public record FulfilInput(
        Guid PlantId,
        Guid PalletId,
        string SourceBin,
        decimal Quantity,
        Guid CommandId
    );

    public record CancelInput(Guid PlantId, string Reason, Guid CommandId);

    public record IssueInput(
        Guid PlantId,
        Guid PalletId,
        string Kind,
        string Notes,
        decimal? CountedQuantity,
        Guid CommandId
    );

    public record ResolveInput(Guid PlantId, bool ApplyCount, string Resolution, Guid CommandId);

    public record KanbanInput(
        Guid PlantId,
        Guid BinId,
        Guid MaterialId,
        string Mode,
        decimal LowStock,
        decimal ReorderStock,
        decimal MaxStock
    );

    public static void MapStaging(this WebApplication app)
    {
        var g = app.MapGroup("/api/operations/staging").RequireAuthorization();
        ReadList<StagingArea>(g, "areas");
        ReadList<StagingRule>(g, "rules");
        ReadList<BatchStagingFactor>(g, "factors");
        ReadList<ReplenishmentRequest>(g, "requests");
        ReadList<StagingStockIssue>(g, "stock-issues");
        g.MapPost(
            "/areas",
            async (AreaInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true, true);
                Ops.Check(
                    r.Workflow is "Separate" or "Together" or "PickOnly",
                    "Choose Separate, Together or PickOnly workflow."
                );
                await Get<WarehouseBin>(db, s, r.StagingBinId);
                await Get<WarehouseBin>(db, s, r.SupplyBinId);
                Ops.Check(
                    r.StagingBinId != r.SupplyBinId,
                    "Staging and supply bins must be different."
                );
                var code = Ops.Code(r.Code);
                var area = await Ops.Rows<StagingArea>(db, s)
                    .SingleOrDefaultAsync(x => x.Code == code);
                if (area is null)
                {
                    area = Ops.Stamp(new StagingArea { Code = code }, s);
                    db.Add(area);
                }
                else
                    Ops.Check(
                        !await Ops.Rows<StagingPlan>(db, s)
                            .AnyAsync(x => x.StagingAreaId == area.Id),
                        "This area is used by a plan. Create a new area to change its workflow or bins."
                    );
                area.Name = Ops.Name(r.Name);
                area.StagingBinId = r.StagingBinId;
                area.SupplyBinId = r.SupplyBinId;
                area.Workflow = r.Workflow;
                Ops.Audit(db, s, "StagingAreaSaved", area.Id);
                await db.SaveChangesAsync();
                return Results.Ok(area);
            }
        );
        g.MapPost(
            "/rules",
            async (RuleInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true, true);
                Ops.Check(
                    (await Get<Material>(db, s, r.MaterialId)).Kind == "Raw",
                    "Choose a raw material."
                );
                Ops.Check(
                    Nonnegative(r.PackSize)
                        && Nonnegative(r.TolerancePercent)
                        && r.TolerancePercent <= 100,
                    "Pack size must be nonnegative; tolerance must be from 0 to 100 percent, with at most six decimals."
                );
                if (r.CorrectionRequired)
                    Ops.Check(
                        !await Ops.Rows<KanbanPolicy>(db, s)
                            .AnyAsync(x => x.MaterialId == r.MaterialId && x.Mode == "Demand"),
                        "Use a static Kanban buffer for correction-factor materials."
                    );
                var row = await Ops.Rows<StagingRule>(db, s)
                    .SingleOrDefaultAsync(x => x.MaterialId == r.MaterialId);
                if (row is null)
                {
                    row = Ops.Stamp(new StagingRule { MaterialId = r.MaterialId }, s);
                    db.Add(row);
                }
                row.PackSize = r.PackSize;
                row.PreweighRequired = r.PreweighRequired;
                row.TolerancePercent = r.TolerancePercent;
                row.CorrectionRequired = r.CorrectionRequired;
                row.SafetyNotes = Notes(r.SafetyNotes);
                row.HandlingNotes = Notes(r.HandlingNotes);
                Ops.Audit(db, s, "StagingRuleSaved", row.Id);
                await db.SaveChangesAsync();
                return Results.Ok(row);
            }
        );
        g.MapPost(
            "/factors",
            async (FactorInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true, true);
                Ops.Check(
                    (await Get<Batch>(db, s, r.BatchId)).Kind == "Raw",
                    "Choose a raw batch."
                );
                Ops.Check(
                    Ops.Positive(r.Factor) && r.Factor <= 1000,
                    "Correction factor must be positive and at most 1000."
                );
                var row = await Ops.Rows<BatchStagingFactor>(db, s)
                    .SingleOrDefaultAsync(x => x.BatchId == r.BatchId);
                if (row is null)
                {
                    row = Ops.Stamp(new BatchStagingFactor { BatchId = r.BatchId }, s);
                    db.Add(row);
                }
                row.Factor = r.Factor;
                row.Evidence = Notes(r.Evidence, true);
                Ops.Audit(db, s, "BatchStagingFactorSaved", row.Id);
                await db.SaveChangesAsync();
                return Results.Ok(row);
            }
        );
        g.MapPost(
            "/plans",
            async (PlanInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true, true);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var order = await Get<ProductionOrder>(db, s, r.ProductionOrderId);
                Ops.Check(order.Status == "Released", "Schedule a released production order.");
                var area = await Get<StagingArea>(db, s, r.StagingAreaId);
                var row = await Ops.Rows<StagingPlan>(db, s)
                    .SingleOrDefaultAsync(x => x.ProductionOrderId == order.Id);
                Ops.Check(
                    r.ScheduledAt > DateTimeOffset.UtcNow.AddYears(-1)
                        && r.ScheduledAt < DateTimeOffset.UtcNow.AddYears(2),
                    "Scheduled time is outside the supported planning window."
                );
                if (row is null)
                {
                    Ops.Check(
                        !await Ops.Rows<Reservation>(db, s)
                            .AnyAsync(x =>
                                x.ProductionOrderId == order.Id && x.Status == "Reserved"
                            ),
                        "Cancel existing quick reservations before creating a guided staging plan."
                    );
                    row = Ops.Stamp(
                        new StagingPlan
                        {
                            ProductionOrderId = order.Id,
                            StagingAreaId = area.Id,
                            Workflow = area.Workflow,
                        },
                        s
                    );
                    db.Add(row);
                }
                else
                {
                    Ops.Check(
                        row.StagingAreaId == area.Id
                            || !await Ops.Rows<StagingPick>(db, s)
                                .AnyAsync(x => x.StagingPlanId == row.Id),
                        "Area cannot change after picking starts."
                    );
                    if (row.StagingAreaId != area.Id)
                    {
                        row.StagingAreaId = area.Id;
                        row.Workflow = area.Workflow;
                    }
                    Ops.Check(
                        !await Ops.Rows<StagingPick>(db, s)
                            .AnyAsync(x => x.StagingPlanId == row.Id && x.Status != "Returned")
                            || (
                                row.ApprovalRequired == r.ApprovalRequired
                                && row.Approved == r.Approved
                            ),
                        "Approval settings cannot change while materials are staged. Return them first."
                    );
                }
                row.ScheduledAt = r.ScheduledAt.ToUniversalTime();
                row.Priority = r.Priority;
                row.ApprovalRequired = r.ApprovalRequired;
                row.Approved = r.Approved;
                row.ApprovalNotes = Notes(r.ApprovalNotes, r.ApprovalRequired && r.Approved);
                Ops.Audit(db, s, "StagingPlanScheduled", row.Id);
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(row);
            }
        );
        g.MapGet(
            "/orders",
            async (Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                var rows = await (
                    from o in Ops.Rows<ProductionOrder>(db, s).AsNoTracking()
                    join m in Ops.Rows<Material>(db, s) on o.MaterialId equals m.Id
                    join plan in Ops.Rows<StagingPlan>(db, s)
                        on o.Id equals plan.ProductionOrderId
                        into plans
                    from plan in plans.DefaultIfEmpty()
                    orderby plan != null && plan.Priority descending, plan == null
                        ? DateTimeOffset.MaxValue
                        : plan.ScheduledAt, o.CreatedAt descending
                    select new
                    {
                        o.Id,
                        o.Number,
                        o.Status,
                        o.PlannedQuantity,
                        product = m.Name,
                        productCode = m.Code,
                        m.Uom,
                        planId = plan == null ? (Guid?)null : plan.Id,
                        scheduledAt = plan == null ? (DateTimeOffset?)null : plan.ScheduledAt,
                        priority = plan != null && plan.Priority,
                        approvalPending = plan != null && plan.ApprovalRequired && !plan.Approved,
                        workflow = plan == null ? null : plan.Workflow,
                        areaId = plan == null ? (Guid?)null : plan.StagingAreaId,
                        picked = Ops.Rows<StagingPick>(db, s)
                            .Count(x =>
                                plan != null && x.StagingPlanId == plan.Id && x.Status != "Returned"
                            ),
                        waiting = Ops.Rows<StagingPick>(db, s)
                            .Count(x =>
                                plan != null && x.StagingPlanId == plan.Id && x.Status == "Picked"
                            ),
                        supplied = Ops.Rows<StagingPick>(db, s)
                            .Count(x =>
                                plan != null && x.StagingPlanId == plan.Id && x.Status == "Supply"
                            ),
                        issued = Ops.Rows<StagingPick>(db, s)
                            .Count(x =>
                                plan != null && x.StagingPlanId == plan.Id && x.Status == "Issued"
                            ),
                    }
                )
                    .Take(201)
                    .ToListAsync();
                return Results.Ok(new { items = rows.Take(200), truncated = rows.Count > 200 });
            }
        );
        g.MapGet("/plans/{id:guid}", PlanDetail);
        g.MapGet(
            "/plans/{id:guid}/label.svg",
            async (Guid id, Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                var p = await Get<StagingPlan>(db, s, id);
                using var qr = QRCodeGenerator.GenerateQrCode(
                    p.KitCode,
                    QRCodeGenerator.ECCLevel.Q
                );
                using var renderer = new SvgQRCode(qr);
                return Results.Text(renderer.GetGraphic(5), "image/svg+xml");
            }
        );
        MapPicking(g);
        MapSupply(g);
    }

    static async Task<IResult> PlanDetail(Guid id, Guid plantId, FactoryDb db, HttpContext h)
    {
        var s = await Ops.Require(db, h.User, plantId);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        var plan = await Get<StagingPlan>(db, s, id);
        var area = await Get<StagingArea>(db, s, plan.StagingAreaId);
        var order = await Get<ProductionOrder>(db, s, plan.ProductionOrderId);
        var bins = await Ops.Rows<WarehouseBin>(db, s)
            .AsNoTracking()
            .Where(x => x.Id == area.StagingBinId || x.Id == area.SupplyBinId)
            .ToListAsync();
        var lines = await (
            from b in Ops.Rows<BomLine>(db, s)
            join m in Ops.Rows<Material>(db, s) on b.MaterialId equals m.Id
            join rule in Ops.Rows<StagingRule>(db, s) on m.Id equals rule.MaterialId into rules
            from rule in rules.DefaultIfEmpty()
            where b.ProductionOrderId == order.Id
            orderby m.Code
            select new
            {
                materialId = m.Id,
                m.Code,
                m.Name,
                m.Uom,
                required = b.Quantity,
                ruleVersion = rule == null ? (uint?)null : rule.Version,
                packSize = rule == null ? 0 : rule.PackSize,
                preweighRequired = rule != null && rule.PreweighRequired,
                correctionRequired = rule != null && rule.CorrectionRequired,
                tolerancePercent = rule == null ? 0 : rule.TolerancePercent,
                safetyNotes = rule == null ? "" : rule.SafetyNotes,
                handlingNotes = rule == null ? "" : rule.HandlingNotes,
            }
        ).ToListAsync();
        var picks = await Ops.Rows<StagingPick>(db, s)
            .AsNoTracking()
            .Where(x => x.StagingPlanId == id)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
        var reservations = await Ops.Rows<Reservation>(db, s)
            .AsNoTracking()
            .Where(x => x.ProductionOrderId == order.Id)
            .ToListAsync();
        var consumption = await (
            from c in Ops.Rows<Consumption>(db, s)
            join p in Ops.Rows<Pallet>(db, s) on c.PalletId equals p.Id
            join b in Ops.Rows<Batch>(db, s) on p.BatchId equals b.Id
            where c.ProductionOrderId == order.Id
            group c by b.MaterialId into grp
            select new
            {
                materialId = grp.Key,
                quantity = grp.Sum(x => x.RequirementQuantity ?? x.Quantity),
            }
        ).ToDictionaryAsync(x => x.materialId, x => x.quantity);
        var history = await Ops.Rows<StagingCommand>(db, s)
            .AsNoTracking()
            .Where(x => x.StagingPlanId == id)
            .OrderByDescending(x => x.CreatedAt)
            .Take(201)
            .ToListAsync();
        var measurements = await Ops.Rows<StagingMeasurement>(db, s)
            .AsNoTracking()
            .Where(x =>
                Ops.Rows<StagingPick>(db, s)
                    .Any(p => p.Id == x.StagingPickId && p.StagingPlanId == id)
            )
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
        var materialIds = lines.Select(x => x.materialId).ToArray();
        var now = DateTimeOffset.UtcNow;
        var sources = await (
            from p in Ops.Rows<Pallet>(db, s)
            join b in Ops.Rows<Batch>(db, s) on p.BatchId equals b.Id
            join factor in Ops.Rows<BatchStagingFactor>(db, s)
                on b.Id equals factor.BatchId
                into factors
            from factor in factors.DefaultIfEmpty()
            where
                materialIds.Contains(b.MaterialId)
                && b.Kind == "Raw"
                && b.QualityStatus == "Released"
                && (b.ExpiryDate == null || b.ExpiryDate > now)
                && p.Quantity > p.ReservedQuantity
                && !Ops.Rows<StagingStockIssue>(db, s)
                    .Any(i => i.PalletId == p.Id && i.Status == "Open")
            orderby b.ExpiryDate == null, b.ExpiryDate, p.CreatedAt
            select new
            {
                p.Id,
                p.Code,
                p.Location,
                p.BatchId,
                b.MaterialId,
                batchNumber = b.Number,
                b.ExpiryDate,
                available = p.Quantity - p.ReservedQuantity,
                factor = factor == null ? (decimal?)null : factor.Factor,
                factorVersion = factor == null ? (uint?)null : factor.Version,
            }
        )
            .Take(501)
            .ToListAsync();
        var pickedIds = picks
            .SelectMany(x => new[] { x.SourcePalletId, x.PickedPalletId, x.CurrentPalletId })
            .Distinct()
            .ToArray();
        var preparedPallets = await (
            from p in Ops.Rows<Pallet>(db, s)
            join b in Ops.Rows<Batch>(db, s) on p.BatchId equals b.Id
            where pickedIds.Contains(p.Id)
            select new
            {
                p.Id,
                p.Code,
                p.Location,
                p.Quantity,
                p.ReservedQuantity,
                batchNumber = b.Number,
                b.MaterialId,
            }
        ).ToListAsync();
        var progress = lines
            .Select(l =>
            {
                var live = picks
                    .Where(p =>
                        p.MaterialId == l.materialId
                        && p.Status != "Returned"
                        && p.Status != "Issued"
                    )
                    .ToArray();
                var credited = consumption.GetValueOrDefault(l.materialId);
                return new
                {
                    l.materialId,
                    issued = credited,
                    allocated = live.Sum(x => x.RequirementQuantity),
                    ready = live.Where(x => x.Status is "Kitted" or "Supply")
                        .Sum(x => x.RequirementQuantity),
                    remaining = Math.Max(
                        0,
                        l.required - credited - live.Sum(x => x.RequirementQuantity)
                    ),
                };
            })
            .ToArray();
        return Results.Ok(
            new
            {
                plan,
                area,
                order,
                bins,
                lines,
                progress,
                picks,
                reservations,
                measurements,
                preparedPallets,
                sources = sources.Take(500),
                sourcesTruncated = sources.Count > 500,
                history = history.Take(200),
                historyTruncated = history.Count > 200,
            }
        );
    }

    static void ReadList<T>(RouteGroupBuilder g, string path)
        where T : class, IPlantRow =>
        g.MapGet(
            "/" + path,
            async (Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                return Results.Ok(
                    await Ops.Rows<T>(db, s)
                        .AsNoTracking()
                        .OrderByDescending(x => x.CreatedAt)
                        .Take(200)
                        .ToListAsync()
                );
            }
        );

    static async Task<T> Get<T>(FactoryDb db, OpsScope s, Guid id)
        where T : class, IPlantRow =>
        await Ops.Rows<T>(db, s).SingleOrDefaultAsync(x => x.Id == id)
        ?? throw new OpsError(404, "Record not found in this plant.");

    static string Notes(string? text, bool required = false)
    {
        var value = (text ?? "").Trim();
        Ops.Check(
            value.Length <= 2000 && (!required || value.Length > 0),
            "Provide a note of at most 2000 characters."
        );
        return value;
    }

    static bool Nonnegative(decimal q) => q >= 0 && q <= 1000000000m && decimal.Round(q, 6) == q;

    static void Usable(Batch b)
    {
        Ops.Check(
            b.Kind == "Raw" && b.QualityStatus == "Released",
            "Only quality-released raw stock can be staged."
        );
        Ops.Check(
            b.ExpiryDate == null || b.ExpiryDate > DateTimeOffset.UtcNow,
            "This batch has expired."
        );
    }

    static string Signature<T>(string kind, Guid? target, T input) =>
        Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    JsonSerializer.Serialize(
                        new
                        {
                            kind,
                            target,
                            input,
                        }
                    )
                )
            )
        );

    static async Task<StagingCommand?> Replay(
        FactoryDb db,
        OpsScope s,
        Guid command,
        string signature
    )
    {
        Ops.Command(command);
        var old = await Ops.Rows<StagingCommand>(db, s)
            .SingleOrDefaultAsync(x => x.CommandId == command);
        if (old != null)
            Ops.Check(old.Signature == signature, "Command ID was used with different input.");
        return old;
    }

    static StagingCommand Record(
        FactoryDb db,
        OpsScope s,
        Guid command,
        string signature,
        string kind,
        Guid result,
        Guid? plan = null,
        string notes = ""
    )
    {
        var row = Ops.Stamp(
            new StagingCommand
            {
                CommandId = command,
                Signature = signature,
                Kind = kind,
                ResultId = result,
                StagingPlanId = plan,
                Notes = notes,
            },
            s
        );
        db.Add(row);
        Ops.Audit(db, s, "Staging" + kind, result);
        return row;
    }

    static StockMovement Move(
        OpsScope s,
        Pallet p,
        decimal q,
        string kind,
        string from,
        string to,
        Guid eventId
    ) =>
        Ops.Stamp(
            new StockMovement
            {
                PalletId = p.Id,
                Quantity = q,
                Kind = kind,
                FromLocation = from,
                ToLocation = to,
                Reference = eventId.ToString(),
                CommandId = Guid.NewGuid(),
            },
            s
        );

    static Pallet Split(
        FactoryDb db,
        OpsScope s,
        Pallet source,
        decimal q,
        string destination,
        Guid eventId,
        string kind
    )
    {
        Ops.Check(
            Ops.Positive(q) && source.Quantity - source.ReservedQuantity >= q,
            "Insufficient unreserved stock for this movement."
        );
        var target = Ops.Stamp(
            new Pallet
            {
                BatchId = source.BatchId,
                Code = "PREP-" + Guid.NewGuid().ToString("N").ToUpperInvariant(),
                Quantity = q,
                Location = destination,
            },
            s
        );
        source.Quantity -= q;
        db.Add(target);
        db.AddRange(
            Move(s, source, -q, kind + "Out", source.Location, destination, eventId),
            Move(s, target, q, kind + "In", source.Location, destination, eventId)
        );
        return target;
    }
}
