using System.Data;
using Microsoft.EntityFrameworkCore;

namespace GreenInk;

public static partial class StagingEndpoints
{
    static void MapSupply(RouteGroupBuilder g)
    {
        g.MapPost(
            "/transfers",
            async (TransferInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true);
                var sig = Signature("Transfer", null, r);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var prior = await Replay(db, s, r.CommandId, sig);
                if (prior != null)
                    return Results.Ok(prior);
                var source = await Get<Pallet>(db, s, r.PalletId);
                Usable(await Get<Batch>(db, s, source.BatchId));
                Ops.Check(
                    source.Location == Ops.Code(r.SourceBin),
                    "Source bin scan does not match this pallet."
                );
                var code = Ops.Code(r.DestinationBin);
                Ops.Check(code != source.Location, "Choose a different target bin.");
                Ops.Check(
                    await Ops.Rows<WarehouseBin>(db, s).AnyAsync(x => x.Code == code),
                    "Choose a configured target bin."
                );
                Ops.Check(
                    !await Ops.Rows<StagingStockIssue>(db, s)
                        .AnyAsync(x => x.PalletId == source.Id && x.Status == "Open"),
                    "Resolve this pallet's stock issue before transferring."
                );
                await CheckCapacity(db, s, source, code, r.Quantity);
                var command = Record(
                    db,
                    s,
                    r.CommandId,
                    sig,
                    "Transfer",
                    Guid.Empty,
                    null,
                    Notes(r.Reason, true)
                );
                command.ResultId = Split(
                    db,
                    s,
                    source,
                    r.Quantity,
                    code,
                    command.Id,
                    "PreparationTransfer"
                ).Id;
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(command);
            }
        );
        g.MapPost(
            "/requests",
            async (RequestInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true);
                var sig = Signature("ReplenishmentRequested", null, r);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var prior = await Replay(db, s, r.CommandId, sig);
                if (prior != null)
                    return Results.Ok(prior);
                Ops.Check(
                    (await Get<Material>(db, s, r.MaterialId)).Kind == "Raw",
                    "Choose a raw material."
                );
                await Get<WarehouseBin>(db, s, r.DestinationBinId);
                Ops.Check(
                    Ops.Positive(r.Quantity),
                    "Request quantity must be positive with at most six decimals."
                );
                Ops.Check(
                    r.Kind is "RMShortage" or "Immediate" or "Kanban",
                    "Choose RMShortage, Immediate or Kanban request."
                );
                if (r.StagingPlanId.HasValue)
                {
                    var plan = await Get<StagingPlan>(db, s, r.StagingPlanId.Value);
                    Ops.Check(
                        (await Get<ProductionOrder>(db, s, plan.ProductionOrderId)).Status
                            == "Released",
                        "Order is no longer released."
                    );
                    Ops.Check(
                        await Ops.Rows<BomLine>(db, s)
                            .AnyAsync(x =>
                                x.ProductionOrderId == plan.ProductionOrderId
                                && x.MaterialId == r.MaterialId
                            ),
                        "Requested material is not in this order's BOM."
                    );
                }
                var row = Ops.Stamp(
                    new ReplenishmentRequest
                    {
                        MaterialId = r.MaterialId,
                        DestinationBinId = r.DestinationBinId,
                        StagingPlanId = r.StagingPlanId,
                        Quantity = r.Quantity,
                        Kind = r.Kind,
                        Reason = Notes(r.Reason, true),
                    },
                    s
                );
                db.Add(row);
                var command = Record(
                    db,
                    s,
                    r.CommandId,
                    sig,
                    "ReplenishmentRequested",
                    row.Id,
                    r.StagingPlanId,
                    row.Reason
                );
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(command);
            }
        );
        g.MapPost(
            "/requests/{id:guid}/fulfil",
            async (Guid id, FulfilInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true);
                var sig = Signature("ReplenishmentDelivered", id, r);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var prior = await Replay(db, s, r.CommandId, sig);
                if (prior != null)
                    return Results.Ok(prior);
                var row = await Get<ReplenishmentRequest>(db, s, id);
                Ops.Check(
                    row.Status is "Requested" or "Started",
                    "This request is completed or cancelled."
                );
                Ops.Check(
                    Ops.Positive(r.Quantity) && row.FulfilledQuantity + r.Quantity <= row.Quantity,
                    "Delivery exceeds the outstanding request quantity."
                );
                var pallet = await Get<Pallet>(db, s, r.PalletId);
                var batch = await Get<Batch>(db, s, pallet.BatchId);
                Usable(batch);
                Ops.Check(
                    batch.MaterialId == row.MaterialId,
                    "Scanned stock does not match the requested material."
                );
                Ops.Check(
                    pallet.Location == Ops.Code(r.SourceBin),
                    "Scanned source bin does not match this pallet."
                );
                Ops.Check(
                    !await Ops.Rows<StagingStockIssue>(db, s)
                        .AnyAsync(x => x.PalletId == pallet.Id && x.Status == "Open"),
                    "Resolve the source stock issue first."
                );
                var destination = await Get<WarehouseBin>(db, s, row.DestinationBinId);
                Ops.Check(
                    pallet.Location != destination.Code,
                    "Replenishment must come from another bin."
                );
                await CheckCapacity(db, s, pallet, destination.Code, r.Quantity);
                var command = Record(
                    db,
                    s,
                    r.CommandId,
                    sig,
                    "ReplenishmentDelivered",
                    id,
                    row.StagingPlanId
                );
                Split(db, s, pallet, r.Quantity, destination.Code, command.Id, "Replenishment");
                row.FulfilledQuantity += r.Quantity;
                row.Status = row.FulfilledQuantity == row.Quantity ? "Completed" : "Started";
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(command);
            }
        );
        g.MapPost(
            "/requests/{id:guid}/cancel",
            async (Guid id, CancelInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true);
                var sig = Signature("ReplenishmentCancelled", id, r);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var prior = await Replay(db, s, r.CommandId, sig);
                if (prior != null)
                    return Results.Ok(prior);
                var row = await Get<ReplenishmentRequest>(db, s, id);
                Ops.Check(
                    row.Status == "Requested" && row.FulfilledQuantity == 0,
                    "Only an unstarted replenishment can be cancelled."
                );
                row.Status = "Cancelled";
                var command = Record(
                    db,
                    s,
                    r.CommandId,
                    sig,
                    "ReplenishmentCancelled",
                    id,
                    row.StagingPlanId,
                    Notes(r.Reason, true)
                );
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(command);
            }
        );
        g.MapPost(
            "/stock-issues",
            async (IssueInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true);
                var sig = Signature("StockIssueReported", null, r);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var prior = await Replay(db, s, r.CommandId, sig);
                if (prior != null)
                    return Results.Ok(prior);
                var pallet = await Get<Pallet>(db, s, r.PalletId);
                Ops.Check(
                    r.Kind
                        is "StockNotFound"
                            or "BatchMismatch"
                            or "BinMismatch"
                            or "Loss"
                            or "Gain",
                    "Choose a valid stock issue type."
                );
                Ops.Check(
                    r.CountedQuantity is null || Nonnegative(r.CountedQuantity.Value),
                    "Counted quantity must be nonnegative with at most six decimals."
                );
                Ops.Check(
                    !await Ops.Rows<StagingStockIssue>(db, s)
                        .AnyAsync(x => x.PalletId == pallet.Id && x.Status == "Open"),
                    "An open stock issue already exists for this pallet."
                );
                if (r.Kind is "Loss" or "Gain")
                    Ops.Check(
                        r.CountedQuantity.HasValue
                            && (
                                r.Kind == "Loss"
                                    ? r.CountedQuantity < pallet.Quantity
                                    : r.CountedQuantity > pallet.Quantity
                            ),
                        "Enter a counted quantity consistent with the loss or gain."
                    );
                var issue = Ops.Stamp(
                    new StagingStockIssue
                    {
                        PalletId = pallet.Id,
                        Kind = r.Kind,
                        Notes = Notes(r.Notes, true),
                        SystemQuantity = pallet.Quantity,
                        CountedQuantity = r.CountedQuantity,
                    },
                    s
                );
                db.Add(issue);
                var command = Record(
                    db,
                    s,
                    r.CommandId,
                    sig,
                    "StockIssueReported",
                    issue.Id,
                    null,
                    issue.Notes
                );
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(command);
            }
        );
        g.MapPost(
            "/stock-issues/{id:guid}/resolve",
            async (Guid id, ResolveInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true, true);
                var sig = Signature("StockIssueResolved", id, r);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var prior = await Replay(db, s, r.CommandId, sig);
                if (prior != null)
                    return Results.Ok(prior);
                var issue = await Get<StagingStockIssue>(db, s, id);
                Ops.Check(issue.Status == "Open", "This issue is already resolved.");
                var pallet = await Get<Pallet>(db, s, issue.PalletId);
                var command = Record(
                    db,
                    s,
                    r.CommandId,
                    sig,
                    "StockIssueResolved",
                    id,
                    null,
                    Notes(r.Resolution, true)
                );
                if (r.ApplyCount)
                {
                    Ops.Check(
                        issue.Kind is "Loss" or "Gain" && issue.CountedQuantity.HasValue,
                        "Only a loss/gain issue with a physical count can adjust stock."
                    );
                    Ops.Check(
                        pallet.Quantity == issue.SystemQuantity,
                        "Stock changed after this count. Resolve without adjustment and report a fresh count."
                    );
                    Ops.Check(
                        issue.CountedQuantity >= pallet.ReservedQuantity,
                        "Physical count is below reserved stock. Return or resolve allocations before adjusting."
                    );
                    var delta = issue.CountedQuantity!.Value - pallet.Quantity;
                    Ops.Check(delta != 0, "No stock adjustment is needed.");
                    db.Add(
                        Move(
                            s,
                            pallet,
                            delta,
                            "Staging" + issue.Kind,
                            pallet.Location,
                            pallet.Location,
                            command.Id
                        )
                    );
                    pallet.Quantity = issue.CountedQuantity.Value;
                }
                issue.Status = "Resolved";
                issue.Resolution = command.Notes;
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(command);
            }
        );
        g.MapPost(
            "/kanban",
            async (KanbanInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true, true);
                await Get<WarehouseBin>(db, s, r.BinId);
                Ops.Check(
                    (await Get<Material>(db, s, r.MaterialId)).Kind == "Raw",
                    "Kanban requires a raw material."
                );
                Ops.Check(r.Mode is "Static" or "Demand", "Choose Static or Demand mode.");
                Ops.Check(
                    Nonnegative(r.LowStock)
                        && Nonnegative(r.ReorderStock)
                        && Ops.Positive(r.MaxStock)
                        && r.LowStock <= r.ReorderStock
                        && r.ReorderStock <= r.MaxStock,
                    "Set 0 ≤ low stock ≤ reorder stock ≤ maximum stock."
                );
                if (r.Mode == "Demand")
                    Ops.Check(
                        !await Ops.Rows<StagingRule>(db, s)
                            .AnyAsync(x => x.MaterialId == r.MaterialId && x.CorrectionRequired),
                        "Use static Kanban for correction-factor materials; demand mode uses uncorrected BOM units."
                    );
                var row = await Ops.Rows<KanbanPolicy>(db, s)
                    .SingleOrDefaultAsync(x => x.BinId == r.BinId && x.MaterialId == r.MaterialId);
                if (row is null)
                {
                    row = Ops.Stamp(
                        new KanbanPolicy { BinId = r.BinId, MaterialId = r.MaterialId },
                        s
                    );
                    db.Add(row);
                }
                row.Mode = r.Mode;
                row.LowStock = r.LowStock;
                row.ReorderStock = r.ReorderStock;
                row.MaxStock = r.MaxStock;
                Ops.Audit(db, s, "KanbanPolicySaved", row.Id);
                await db.SaveChangesAsync();
                return Results.Ok(row);
            }
        );
        g.MapGet("/kanban", KanbanDashboard);
    }

    internal static async Task CheckCapacity(
        FactoryDb db,
        OpsScope s,
        Pallet source,
        string destination,
        decimal incoming
    )
    {
        if (source.Location == destination)
            return;
        var batch = await Get<Batch>(db, s, source.BatchId);
        var policy = await (
            from p in Ops.Rows<KanbanPolicy>(db, s)
            join b in Ops.Rows<WarehouseBin>(db, s) on p.BinId equals b.Id
            where b.Code == destination && p.MaterialId == batch.MaterialId
            select p
        ).SingleOrDefaultAsync();
        if (policy is null)
            return;
        var current = await (
            from p in Ops.Rows<Pallet>(db, s)
            join b in Ops.Rows<Batch>(db, s) on p.BatchId equals b.Id
            where p.Location == destination && b.MaterialId == batch.MaterialId
            select p.Quantity
        ).SumAsync();
        Ops.Check(
            current + incoming <= policy.MaxStock,
            "Movement exceeds the configured Kanban maximum. Review the buffer policy before moving excess stock."
        );
    }

    static async Task<IResult> KanbanDashboard(Guid plantId, FactoryDb db, HttpContext h)
    {
        var s = await Ops.Require(db, h.User, plantId);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        var policies = await (
            from p in Ops.Rows<KanbanPolicy>(db, s).AsNoTracking()
            join b in Ops.Rows<WarehouseBin>(db, s) on p.BinId equals b.Id
            join m in Ops.Rows<Material>(db, s) on p.MaterialId equals m.Id
            orderby p.Mode descending, m.Code
            select new
            {
                policy = p,
                binCode = b.Code,
                materialCode = m.Code,
                materialName = m.Name,
                m.Uom,
            }
        )
            .Take(200)
            .ToListAsync();
        var result = new List<object>();
        var now = DateTimeOffset.UtcNow;
        foreach (var p in policies)
        {
            var qty = await (
                from a in Ops.Rows<Pallet>(db, s)
                join b in Ops.Rows<Batch>(db, s) on a.BatchId equals b.Id
                where
                    a.Location == p.binCode
                    && b.MaterialId == p.policy.MaterialId
                    && b.QualityStatus == "Released"
                    && (b.ExpiryDate == null || b.ExpiryDate > now)
                    && !Ops.Rows<StagingStockIssue>(db, s)
                        .Any(i => i.PalletId == a.Id && i.Status == "Open")
                select a.Quantity - a.ReservedQuantity
            ).SumAsync();
            var demand = 0m;
            if (p.policy.Mode == "Demand")
            {
                var planOrders = await (
                    from plan in Ops.Rows<StagingPlan>(db, s)
                    join area in Ops.Rows<StagingArea>(db, s) on plan.StagingAreaId equals area.Id
                    join o in Ops.Rows<ProductionOrder>(db, s) on plan.ProductionOrderId equals o.Id
                    where
                        o.Status == "Released"
                        && (
                            area.StagingBinId == p.policy.BinId
                            || area.SupplyBinId == p.policy.BinId
                        )
                    select o.Id
                ).ToArrayAsync();
                var req = await Ops.Rows<BomLine>(db, s)
                    .Where(x =>
                        planOrders.Contains(x.ProductionOrderId)
                        && x.MaterialId == p.policy.MaterialId
                    )
                    .SumAsync(x => x.Quantity);
                var issued = await (
                    from c in Ops.Rows<Consumption>(db, s)
                    join a in Ops.Rows<Pallet>(db, s) on c.PalletId equals a.Id
                    join b in Ops.Rows<Batch>(db, s) on a.BatchId equals b.Id
                    where
                        planOrders.Contains(c.ProductionOrderId)
                        && b.MaterialId == p.policy.MaterialId
                    select c.RequirementQuantity ?? c.Quantity
                ).SumAsync();
                var reserved = await (
                    from c in Ops.Rows<Reservation>(db, s)
                    join a in Ops.Rows<Pallet>(db, s) on c.PalletId equals a.Id
                    join b in Ops.Rows<Batch>(db, s) on a.BatchId equals b.Id
                    where
                        c.Status == "Reserved"
                        && planOrders.Contains(c.ProductionOrderId)
                        && b.MaterialId == p.policy.MaterialId
                    select c.RequirementQuantity ?? c.Quantity
                ).SumAsync();
                demand = Math.Max(0, req - issued - reserved);
            }
            var open = await Ops.Rows<ReplenishmentRequest>(db, s)
                .Where(x =>
                    x.MaterialId == p.policy.MaterialId
                    && x.DestinationBinId == p.policy.BinId
                    && (x.Status == "Requested" || x.Status == "Started")
                )
                .SumAsync(x => x.Quantity - x.FulfilledQuantity);
            var need =
                p.policy.Mode == "Demand" ? Math.Max(0, demand - qty)
                : qty < p.policy.ReorderStock ? Math.Max(0, p.policy.MaxStock - qty)
                : 0;
            var target =
                p.policy.Mode == "Demand"
                    ? Math.Min(p.policy.MaxStock, Math.Max(qty, demand))
                    : p.policy.MaxStock;
            var request = Math.Max(0, Math.Min(need, Math.Max(0, target - qty)) - open);
            var status =
                p.policy.Mode == "Demand"
                    ? (
                        qty >= demand ? "Healthy"
                        : qty >= p.policy.MaxStock ? "Capacity"
                        : "Urgent"
                    )
                    : (
                        qty > p.policy.MaxStock ? "Capacity"
                        : qty < p.policy.LowStock ? "Urgent"
                        : qty < p.policy.ReorderStock ? "Replenish"
                        : "Healthy"
                    );
            result.Add(
                new
                {
                    p.policy,
                    p.binCode,
                    p.materialCode,
                    p.materialName,
                    p.Uom,
                    available = qty,
                    demand,
                    openQuantity = open,
                    requestQuantity = request,
                    status,
                }
            );
        }
        return Results.Ok(result);
    }
}
