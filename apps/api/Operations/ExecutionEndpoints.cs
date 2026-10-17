using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace GreenInk;

public static class ExecutionEndpoints
{
    public record Requirement(Guid MaterialId, decimal Quantity);

    public record OrderInput(
        Guid PlantId,
        string Number,
        Guid MaterialId,
        decimal PlannedQuantity,
        Requirement[] Requirements
    );

    public record CommandInput(Guid PlantId, Guid CommandId);

    public record ReserveInput(
        Guid PlantId,
        Guid ProductionOrderId,
        Guid PalletId,
        decimal Quantity,
        Guid CommandId
    );

    public record CompleteInput(
        Guid PlantId,
        decimal Quantity,
        decimal ScrapQuantity,
        string Location,
        Guid CommandId
    );

    public record DowntimeInput(Guid PlantId, Guid ProductionOrderId, int Minutes, string Reason);

    public record DeliveryInput(
        Guid PlantId,
        string Number,
        Guid CustomerId,
        Guid MaterialId,
        decimal Quantity
    );

    public record ShipInput(
        Guid PlantId,
        Guid DeliveryOrderId,
        Guid PalletId,
        decimal Quantity,
        string Truck,
        Guid CommandId
    );

    public static void MapExecution(this WebApplication app)
    {
        var g = app.MapGroup("/api/operations").RequireAuthorization();
        List<ProductionOrder>(g, "production-orders");
        List<Reservation>(g, "reservations");
        List<Consumption>(g, "consumptions");
        List<ProductionConfirmation>(g, "confirmations");
        List<DowntimeEvent>(g, "downtimes");
        List<DeliveryOrder>(g, "delivery-orders");
        List<Shipment>(g, "shipments");
        g.MapGet(
            "/bom",
            async (Guid plantId, Guid productionOrderId, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, plantId);
                Ops.Check(
                    await Ops.Rows<ProductionOrder>(db, s).AnyAsync(x => x.Id == productionOrderId),
                    "Order not found."
                );
                return Results.Ok(
                    await Ops.Rows<BomLine>(db, s)
                        .Where(x => x.ProductionOrderId == productionOrderId)
                        .Take(200)
                        .ToListAsync()
                );
            }
        );
        g.MapPost(
            "/production-orders",
            async (OrderInput r, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, r.PlantId, true);
                var number = Ops.Code(r.Number);
                Ops.Check(
                    Ops.Positive(r.PlannedQuantity),
                    "Planned quantity must be positive with at most six decimal places."
                );
                Ops.Check(
                    r.Requirements is { Length: > 0 and <= 200 },
                    "Supply 1–200 raw material requirements."
                );
                Ops.Check(
                    r.Requirements.All(x => x is not null),
                    "BOM requirements cannot contain null entries."
                );
                Ops.Check(
                    r.Requirements.Select(x => x.MaterialId).Distinct().Count()
                        == r.Requirements.Length,
                    "Duplicate BOM materials are not allowed."
                );
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                await MaterialKind(db, s, r.MaterialId, "Finished");
                Ops.Check(
                    !await Ops.Rows<ProductionOrder>(db, s).AnyAsync(x => x.Number == number),
                    "Order number already exists."
                );
                var o = Ops.Stamp(
                    new ProductionOrder
                    {
                        Number = number,
                        MaterialId = r.MaterialId,
                        PlannedQuantity = r.PlannedQuantity,
                    },
                    s
                );
                db.Add(o);
                foreach (var line in r.Requirements)
                {
                    Ops.Check(
                        Ops.Positive(line.Quantity),
                        "BOM quantities must be positive with at most six decimal places."
                    );
                    await MaterialKind(db, s, line.MaterialId, "Raw");
                    db.Add(
                        Ops.Stamp(
                            new BomLine
                            {
                                ProductionOrderId = o.Id,
                                MaterialId = line.MaterialId,
                                Quantity = line.Quantity,
                            },
                            s
                        )
                    );
                }
                Ops.Audit(db, s, "ProductionOrderCreated", o.Id);
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(o);
            }
        );
        g.MapPost(
            "/production-orders/{id:guid}/release",
            async (Guid id, CommandInput r, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, r.PlantId, true, true);
                Ops.Command(r.CommandId);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var o = await Order(db, s, id);
                if (o.Status == "Released")
                    return Results.Ok(o);
                Ops.Check(o.Status == "Draft", "Only draft orders can be released.");
                o.Status = "Released";
                Ops.Audit(db, s, "ProductionOrderReleased", id);
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(o);
            }
        );
        g.MapPost(
            "/reservations",
            async (ReserveInput r, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, r.PlantId, true);
                Ops.Command(r.CommandId);
                Ops.Check(
                    Ops.Positive(r.Quantity),
                    "Quantity must be positive with at most six decimal places."
                );
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var old = await Ops.Rows<Reservation>(db, s)
                    .SingleOrDefaultAsync(x => x.CommandId == r.CommandId);
                if (old != null)
                {
                    Ops.Check(
                        old.ProductionOrderId == r.ProductionOrderId
                            && old.PalletId == r.PalletId
                            && old.Quantity == r.Quantity,
                        "Command ID was used with different input."
                    );
                    return Results.Ok(old);
                }
                var o = await Order(db, s, r.ProductionOrderId);
                Ops.Check(o.Status == "Released", "Production order must be released.");
                Ops.Check(
                    !await Ops.Rows<StagingPlan>(db, s).AnyAsync(x => x.ProductionOrderId == o.Id),
                    "This order has a staging plan. Use the guided picking workspace."
                );
                var pallet = await Get<Pallet>(db, s, r.PalletId);
                var batch = await Get<Batch>(db, s, pallet.BatchId);
                Usable(batch, "Raw");
                Ops.Check(
                    pallet.Quantity - pallet.ReservedQuantity >= r.Quantity,
                    "Insufficient available stock."
                );
                var bom = await Ops.Rows<BomLine>(db, s)
                    .SingleOrDefaultAsync(x =>
                        x.ProductionOrderId == o.Id && x.MaterialId == batch.MaterialId
                    );
                Ops.Check(bom != null, "Material is not required by this order.");
                var open = await (
                    from a in Ops.Rows<Reservation>(db, s)
                    join b in Ops.Rows<Pallet>(db, s) on a.PalletId equals b.Id
                    join c in Ops.Rows<Batch>(db, s) on b.BatchId equals c.Id
                    where
                        a.ProductionOrderId == o.Id
                        && a.Status == "Reserved"
                        && c.MaterialId == batch.MaterialId
                    select a.RequirementQuantity ?? a.Quantity
                ).SumAsync();
                var used = await Used(db, s, o.Id, batch.MaterialId);
                Ops.Check(
                    open + used + r.Quantity <= bom!.Quantity,
                    $"Order {o.Number} requires {bom!.Quantity:0.######} of this material: {used:0.######} issued and {open:0.######} reserved. Reserve at most {Math.Max(0, bom.Quantity - used - open):0.######} more."
                );
                pallet.ReservedQuantity += r.Quantity;
                var row = Ops.Stamp(
                    new Reservation
                    {
                        ProductionOrderId = o.Id,
                        PalletId = pallet.Id,
                        Quantity = r.Quantity,
                        CommandId = r.CommandId,
                    },
                    s
                );
                db.Add(row);
                Ops.Audit(db, s, "StockReserved", row.Id);
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(row);
            }
        );
        g.MapPost(
            "/reservations/{id:guid}/issue",
            async (Guid id, CommandInput r, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, r.PlantId, true);
                Ops.Command(r.CommandId);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var row = await Get<Reservation>(db, s, id);
                var prior = await Ops.Rows<Consumption>(db, s)
                    .SingleOrDefaultAsync(x => x.CommandId == r.CommandId);
                if (prior != null)
                {
                    var priorMove = await Ops.Rows<StockMovement>(db, s)
                        .SingleOrDefaultAsync(x =>
                            x.CommandId == r.CommandId && x.Kind == "Consumption"
                        );
                    Ops.Check(
                        row.Status == "Issued"
                            && prior.ProductionOrderId == row.ProductionOrderId
                            && prior.PalletId == row.PalletId
                            && prior.Quantity == row.Quantity
                            && priorMove != null
                            && priorMove.Reference.EndsWith(":" + row.Id),
                        "Command ID was used with different input."
                    );
                    return Results.Ok(prior);
                }
                Ops.Check(row.Status == "Reserved", "Only reserved stock can be issued.");
                if (row.StagingPickId.HasValue)
                {
                    var staged = await Get<StagingPick>(db, s, row.StagingPickId.Value);
                    var plan = await Get<StagingPlan>(db, s, staged.StagingPlanId);
                    var area = await Get<StagingArea>(db, s, plan.StagingAreaId);
                    var bin = await Get<WarehouseBin>(db, s, area.SupplyBinId);
                    var prepared = await Get<Pallet>(db, s, row.PalletId);
                    Ops.Check(
                        staged.Status == "Supply" && prepared.Location == bin.Code,
                        "Deliver the prepared kit to its production supply bin before issuing."
                    );
                    Ops.Check(
                        !await Ops.Rows<StagingStockIssue>(db, s)
                            .AnyAsync(x => x.PalletId == row.PalletId && x.Status == "Open"),
                        "Resolve the prepared stock issue before issuing."
                    );
                    staged.Status = "Issued";
                }
                var o = await Order(db, s, row.ProductionOrderId);
                Ops.Check(o.Status == "Released", "Order must be released.");
                var pallet = await Get<Pallet>(db, s, row.PalletId);
                Usable(await Get<Batch>(db, s, pallet.BatchId), "Raw");
                Ops.Check(
                    pallet.Quantity >= row.Quantity && pallet.ReservedQuantity >= row.Quantity,
                    "Stock is no longer available."
                );
                pallet.Quantity -= row.Quantity;
                pallet.ReservedQuantity -= row.Quantity;
                row.Status = "Issued";
                var used = Ops.Stamp(
                    new Consumption
                    {
                        ProductionOrderId = o.Id,
                        PalletId = pallet.Id,
                        Quantity = row.Quantity,
                        RequirementQuantity = row.RequirementQuantity,
                        CommandId = r.CommandId,
                    },
                    s
                );
                db.Add(used);
                db.Add(
                    Move(
                        s,
                        pallet,
                        -row.Quantity,
                        "Consumption",
                        $"{o.Number}:{row.Id}",
                        r.CommandId
                    )
                );
                Ops.Audit(db, s, "ReservationIssued", row.Id);
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(used);
            }
        );
        g.MapPost(
            "/reservations/{id:guid}/cancel",
            async (Guid id, CommandInput r, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, r.PlantId, true);
                Ops.Command(r.CommandId);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var row = await Get<Reservation>(db, s, id);
                if (row.Status == "Cancelled")
                    return Results.Ok(row);
                Ops.Check(row.Status == "Reserved", "Only reserved stock can be cancelled.");
                Ops.Check(
                    row.StagingPickId is null,
                    "Use the guided return action to cancel a staged allocation and restore its stock."
                );
                var pallet = await Get<Pallet>(db, s, row.PalletId);
                Ops.Check(
                    pallet.ReservedQuantity >= row.Quantity,
                    "Reservation balance is inconsistent."
                );
                pallet.ReservedQuantity -= row.Quantity;
                row.Status = "Cancelled";
                Ops.Audit(db, s, "ReservationCancelled", id);
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(row);
            }
        );
        g.MapPost(
            "/production-orders/{id:guid}/complete",
            async (Guid id, CompleteInput r, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, r.PlantId, true);
                Ops.Command(r.CommandId);
                Ops.Check(
                    Ops.Positive(r.Quantity)
                        && r.ScrapQuantity >= 0
                        && r.ScrapQuantity <= 1000000000m
                        && decimal.Round(r.ScrapQuantity, 6) == r.ScrapQuantity,
                    "Output must be positive and scrap nonnegative, with at most six decimal places."
                );
                var location = Ops.Code(r.Location);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var prior = await Ops.Rows<ProductionConfirmation>(db, s)
                    .SingleOrDefaultAsync(x => x.CommandId == r.CommandId);
                if (prior != null)
                {
                    var movement = await Ops.Rows<StockMovement>(db, s)
                        .SingleAsync(x => x.CommandId == r.CommandId && x.Kind == "Production");
                    Ops.Check(
                        prior.ProductionOrderId == id
                            && prior.Quantity == r.Quantity
                            && prior.ScrapQuantity == r.ScrapQuantity
                            && movement.ToLocation == location,
                        "Command ID was used with different input."
                    );
                    return Results.Ok(prior);
                }
                var o = await Order(db, s, id);
                Ops.Check(
                    o.Status == "Released",
                    "Order must be released and not previously completed."
                );
                Ops.Check(
                    !await Ops.Rows<StagingPlan>(db, s)
                        .AnyAsync(x =>
                            x.ProductionOrderId == id && x.ApprovalRequired && !x.Approved
                        ),
                    "Engineering approval is pending for this order's staging plan."
                );
                Ops.Check(
                    r.Quantity + r.ScrapQuantity == o.PlannedQuantity,
                    $"Good output plus scrap must equal this order’s planned quantity of {o.PlannedQuantity:0.######}. Partial completion is not supported; enter actual quantities only."
                );
                Ops.Check(
                    await Ops.Rows<WarehouseBin>(db, s).AnyAsync(x => x.Code == location),
                    "Choose a configured destination bin."
                );
                var bom = await Ops.Rows<BomLine>(db, s)
                    .Where(x => x.ProductionOrderId == id)
                    .ToListAsync();
                Ops.Check(bom.Count > 0, "Order has no BOM requirements.");
                foreach (var line in bom)
                {
                    var issued = await Used(db, s, id, line.MaterialId);
                    if (issued < line.Quantity)
                    {
                        var material = await Get<Material>(db, s, line.MaterialId);
                        throw new OpsError(
                            400,
                            $"Issue {line.Quantity - issued:0.######} {material.Uom} more of {material.Code} — {material.Name} in Material Preparation before completing order {o.Number}. Required: {line.Quantity:0.######}; already issued: {issued:0.######}."
                        );
                    }
                }
                var batch = Ops.Stamp(
                    new Batch
                    {
                        MaterialId = o.MaterialId,
                        Number = $"FG-{Guid.NewGuid():N}",
                        Kind = "Finished",
                        ProductionOrderId = id,
                        QualityStatus = "Hold",
                    },
                    s
                );
                var pallet = Ops.Stamp(
                    new Pallet
                    {
                        BatchId = batch.Id,
                        Code = $"PAL-{Guid.NewGuid():N}",
                        Location = location,
                        Quantity = r.Quantity,
                    },
                    s
                );
                var row = Ops.Stamp(
                    new ProductionConfirmation
                    {
                        ProductionOrderId = id,
                        BatchId = batch.Id,
                        PalletId = pallet.Id,
                        Quantity = r.Quantity,
                        ScrapQuantity = r.ScrapQuantity,
                        CommandId = r.CommandId,
                    },
                    s
                );
                db.AddRange(batch, pallet, row);
                db.Add(Move(s, pallet, r.Quantity, "Production", o.Number, r.CommandId));
                o.CompletedQuantity = r.Quantity;
                o.Status = "Completed";
                Ops.Audit(db, s, "ProductionCompleted", id);
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(row);
            }
        );
        g.MapPost(
            "/downtime",
            async (DowntimeInput r, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, r.PlantId, true);
                await Order(db, s, r.ProductionOrderId);
                Ops.Check(
                    r.Minutes > 0 && r.Minutes <= 525600,
                    "Minutes must be between 1 and 525600."
                );
                var row = Ops.Stamp(
                    new DowntimeEvent
                    {
                        ProductionOrderId = r.ProductionOrderId,
                        Minutes = r.Minutes,
                        Reason = Ops.Name(r.Reason),
                    },
                    s
                );
                db.Add(row);
                Ops.Audit(db, s, "DowntimeRecorded", row.Id);
                await db.SaveChangesAsync();
                return Results.Ok(row);
            }
        );
        g.MapGet(
            "/finished-goods",
            async (Guid plantId, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, plantId);
                return Results.Ok(
                    await (
                        from a in Ops.Rows<Pallet>(db, s)
                        join b in Ops.Rows<Batch>(db, s) on a.BatchId equals b.Id
                        where b.Kind == "Finished"
                        orderby a.CreatedAt descending
                        select new
                        {
                            a.Id,
                            a.PlantId,
                            a.Code,
                            a.BatchId,
                            a.Location,
                            a.Quantity,
                            a.ReservedQuantity,
                            b.MaterialId,
                            b.QualityStatus,
                            b.ExpiryDate,
                            b.ProductionOrderId,
                            BatchNumber = b.Number,
                        }
                    )
                        .Take(200)
                        .ToListAsync()
                );
            }
        );
        g.MapPost(
            "/delivery-orders",
            async (DeliveryInput r, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, r.PlantId, true);
                var number = Ops.Code(r.Number);
                Ops.Check(
                    Ops.Positive(r.Quantity),
                    "Quantity must be positive with at most six decimal places."
                );
                await MaterialKind(db, s, r.MaterialId, "Finished");
                Ops.Check(
                    (await Get<Partner>(db, s, r.CustomerId)).Kind == "Customer",
                    "Choose a customer."
                );
                Ops.Check(
                    !await Ops.Rows<DeliveryOrder>(db, s).AnyAsync(x => x.Number == number),
                    "Delivery order number already exists."
                );
                var row = Ops.Stamp(
                    new DeliveryOrder
                    {
                        Number = number,
                        CustomerId = r.CustomerId,
                        MaterialId = r.MaterialId,
                        Quantity = r.Quantity,
                    },
                    s
                );
                db.Add(row);
                Ops.Audit(db, s, "DeliveryOrderCreated", row.Id);
                await db.SaveChangesAsync();
                return Results.Ok(row);
            }
        );
        g.MapPost(
            "/shipments",
            async (ShipInput r, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, r.PlantId, true);
                Ops.Command(r.CommandId);
                Ops.Check(
                    Ops.Positive(r.Quantity),
                    "Quantity must be positive with at most six decimal places."
                );
                var truck = Ops.Name(r.Truck);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var prior = await Ops.Rows<Shipment>(db, s)
                    .SingleOrDefaultAsync(x => x.CommandId == r.CommandId);
                if (prior != null)
                {
                    Ops.Check(
                        prior.DeliveryOrderId == r.DeliveryOrderId
                            && prior.PalletId == r.PalletId
                            && prior.Quantity == r.Quantity
                            && prior.Truck == truck,
                        "Command ID was used with different input."
                    );
                    return Results.Ok(prior);
                }
                var o = await Get<DeliveryOrder>(db, s, r.DeliveryOrderId);
                Ops.Check(o.Status == "Open", "Delivery order is not open.");
                Ops.Check(
                    r.Quantity == o.Quantity,
                    "Initial workflow requires shipping the complete delivery quantity."
                );
                var pallet = await Get<Pallet>(db, s, r.PalletId);
                var batch = await Get<Batch>(db, s, pallet.BatchId);
                Usable(batch, "Finished");
                Ops.Check(
                    batch.MaterialId == o.MaterialId,
                    "Pallet material does not match delivery order."
                );
                Ops.Check(
                    pallet.Quantity - pallet.ReservedQuantity >= r.Quantity,
                    "Insufficient available stock."
                );
                pallet.Quantity -= r.Quantity;
                o.Status = "Shipped";
                var row = Ops.Stamp(
                    new Shipment
                    {
                        DeliveryOrderId = o.Id,
                        PalletId = pallet.Id,
                        Quantity = r.Quantity,
                        Truck = truck,
                        CommandId = r.CommandId,
                    },
                    s
                );
                db.Add(row);
                db.Add(Move(s, pallet, -r.Quantity, "Dispatch", o.Number, r.CommandId));
                Ops.Audit(db, s, "ShipmentDispatched", row.Id);
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(row);
            }
        );
    }

    static void List<T>(RouteGroupBuilder g, string path)
        where T : class, IPlantRow
    {
        g.MapGet(
            "/" + path,
            async (Guid plantId, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, plantId);
                return Results.Ok(
                    await Ops.Rows<T>(db, s)
                        .AsNoTracking()
                        .OrderByDescending(x => x.CreatedAt)
                        .Take(200)
                        .ToListAsync()
                );
            }
        );
    }

    static async Task<T> Get<T>(FactoryDb db, OpsScope s, Guid id)
        where T : class, IPlantRow =>
        await Ops.Rows<T>(db, s).SingleOrDefaultAsync(x => x.Id == id)
        ?? throw new OpsError(404, "Record not found in this plant.");

    static Task<ProductionOrder> Order(FactoryDb db, OpsScope s, Guid id) =>
        Get<ProductionOrder>(db, s, id);

    static async Task MaterialKind(FactoryDb db, OpsScope s, Guid id, string kind) =>
        Ops.Check((await Get<Material>(db, s, id)).Kind == kind, $"Choose a {kind} material.");

    static void Usable(Batch batch, string kind)
    {
        Ops.Check(batch.Kind == kind, "Incorrect batch kind.");
        Ops.Check(batch.QualityStatus == "Released", "Batch must be quality released.");
        Ops.Check(
            batch.ExpiryDate == null || batch.ExpiryDate > DateTimeOffset.UtcNow,
            "Batch has expired."
        );
    }

    static Task<decimal> Used(FactoryDb db, OpsScope s, Guid order, Guid material) =>
        (
            from a in Ops.Rows<Consumption>(db, s)
            join b in Ops.Rows<Pallet>(db, s) on a.PalletId equals b.Id
            join c in Ops.Rows<Batch>(db, s) on b.BatchId equals c.Id
            where a.ProductionOrderId == order && c.MaterialId == material
            select a.RequirementQuantity ?? a.Quantity
        ).SumAsync();

    static StockMovement Move(
        OpsScope s,
        Pallet pallet,
        decimal quantity,
        string kind,
        string reference,
        Guid command
    ) =>
        Ops.Stamp(
            new StockMovement
            {
                PalletId = pallet.Id,
                Quantity = quantity,
                Kind = kind,
                FromLocation = quantity < 0 ? pallet.Location : "",
                ToLocation = quantity > 0 ? pallet.Location : "",
                Reference = reference,
                CommandId = command,
            },
            s
        );
}
