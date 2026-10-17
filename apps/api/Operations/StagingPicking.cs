using System.Data;
using Microsoft.EntityFrameworkCore;

namespace GreenInk;

public static partial class StagingEndpoints
{
    static void MapPicking(RouteGroupBuilder g)
    {
        g.MapPost(
            "/plans/{id:guid}/pick",
            async (Guid id, PickInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true);
                var sig = Signature("Pick", id, r);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var prior = await Replay(db, s, r.CommandId, sig);
                if (prior != null)
                    return Results.Ok(prior);
                var plan = await Get<StagingPlan>(db, s, id);
                var order = await Get<ProductionOrder>(db, s, plan.ProductionOrderId);
                Ops.Check(order.Status == "Released", "Order is not released.");
                Ops.Check(
                    !plan.ApprovalRequired || plan.Approved,
                    "Engineering approval is pending. Ask a manager to approve the staging plan."
                );
                var area = await Get<StagingArea>(db, s, plan.StagingAreaId);
                var source = await Get<Pallet>(db, s, r.PalletId);
                var batch = await Get<Batch>(db, s, source.BatchId);
                Usable(batch);
                Ops.Check(
                    string.Equals(
                        source.Code,
                        r.PalletCode?.Trim(),
                        StringComparison.OrdinalIgnoreCase
                    ),
                    "Scanned material label does not match the selected source pallet."
                );
                Ops.Check(
                    source.Location == Ops.Code(r.SourceBin),
                    "Scanned source bin does not match this pallet's current location."
                );
                Ops.Check(
                    !await Ops.Rows<StagingStockIssue>(db, s)
                        .AnyAsync(x => x.PalletId == source.Id && x.Status == "Open"),
                    "This pallet has an open stock issue. Select another batch or resolve the issue."
                );
                var bom = await Ops.Rows<BomLine>(db, s)
                    .SingleOrDefaultAsync(x =>
                        x.ProductionOrderId == order.Id && x.MaterialId == batch.MaterialId
                    );
                Ops.Check(bom != null, "Scanned material is not in this order's BOM.");
                Ops.Check(
                    Ops.Positive(r.Quantity) && Ops.Positive(r.RequirementQuantity),
                    "Physical and BOM quantities must be positive with at most six decimals."
                );
                var reserved = await (
                    from res in Ops.Rows<Reservation>(db, s)
                    join p in Ops.Rows<Pallet>(db, s) on res.PalletId equals p.Id
                    join b in Ops.Rows<Batch>(db, s) on p.BatchId equals b.Id
                    where
                        res.ProductionOrderId == order.Id
                        && res.Status == "Reserved"
                        && b.MaterialId == batch.MaterialId
                    select res.RequirementQuantity ?? res.Quantity
                ).SumAsync();
                var used = await (
                    from c in Ops.Rows<Consumption>(db, s)
                    join p in Ops.Rows<Pallet>(db, s) on c.PalletId equals p.Id
                    join b in Ops.Rows<Batch>(db, s) on p.BatchId equals b.Id
                    where c.ProductionOrderId == order.Id && b.MaterialId == batch.MaterialId
                    select c.RequirementQuantity ?? c.Quantity
                ).SumAsync();
                Ops.Check(
                    reserved + used + r.RequirementQuantity <= bom!.Quantity,
                    $"Only {Math.Max(0, bom.Quantity - reserved - used):0.######} BOM units remain to allocate."
                );
                var rule = await Ops.Rows<StagingRule>(db, s)
                    .SingleOrDefaultAsync(x => x.MaterialId == batch.MaterialId);
                Ops.Check(
                    r.RuleVersion == (rule?.Version ?? 0),
                    "Preparation rules changed. Close this form, refresh the plan and review the safety/weight instructions again."
                );
                var factor = 1m;
                if (rule?.CorrectionRequired == true)
                {
                    var f = await Ops.Rows<BatchStagingFactor>(db, s)
                        .SingleOrDefaultAsync(x => x.BatchId == batch.Id);
                    Ops.Check(
                        f != null,
                        "This batch needs an approved correction factor before picking."
                    );
                    Ops.Check(
                        r.FactorVersion == f!.Version,
                        "Batch factor changed. Close this form, refresh the plan and review the corrected target again."
                    );
                    factor = f!.Factor;
                }
                var expected = decimal.Round(r.RequirementQuantity * factor, 6);
                Ops.Check(
                    Ops.Positive(expected),
                    "Corrected physical target is outside the supported range."
                );
                Ops.Check(
                    string.IsNullOrWhiteSpace(rule?.SafetyNotes) || r.SafetyConfirmed,
                    "Acknowledge the material's safety instructions before picking."
                );
                if (plan.Workflow == "Separate")
                {
                    Ops.Check(
                        r.Quantity >= expected,
                        "Separate picking must bring enough physical stock for the selected BOM quantity. Use a smaller BOM allocation for a partial batch."
                    );
                    Ops.Check(
                        r.Gross is null && r.Tare is null,
                        "Record weighing at the separate kitting step."
                    );
                }
                else
                {
                    if (rule?.PreweighRequired == true)
                    {
                        Ops.Check(
                            r.Gross.HasValue && r.Tare.HasValue,
                            "Enter gross and tare readings for this material."
                        );
                        Ops.Check(
                            r.Gross - r.Tare == r.Quantity,
                            "Physical quantity must equal gross minus tare."
                        );
                    }
                    else
                        Ops.Check(
                            (r.Gross is null && r.Tare is null)
                                || (
                                    r.Gross.HasValue
                                    && r.Tare.HasValue
                                    && r.Gross - r.Tare == r.Quantity
                                ),
                            "Gross and tare must be supplied together and match physical quantity."
                        );
                    CheckTolerance(r.Quantity, expected, rule?.TolerancePercent ?? 0);
                }
                var destination = await Get<WarehouseBin>(
                    db,
                    s,
                    plan.Workflow == "Separate" ? area.StagingBinId : area.SupplyBinId
                );
                await CheckCapacity(db, s, source, destination.Code, r.Quantity);
                var command = Record(db, s, r.CommandId, sig, "Pick", Guid.Empty, id);
                var target = Split(
                    db,
                    s,
                    source,
                    r.Quantity,
                    destination.Code,
                    command.Id,
                    "StagingPick"
                );
                target.ReservedQuantity = r.Quantity;
                var pick = Ops.Stamp(
                    new StagingPick
                    {
                        StagingPlanId = id,
                        MaterialId = batch.MaterialId,
                        SourcePalletId = source.Id,
                        PickedPalletId = target.Id,
                        CurrentPalletId = target.Id,
                        Quantity = r.Quantity,
                        RequirementQuantity = r.RequirementQuantity,
                        CorrectionFactor = factor,
                        TolerancePercent = rule?.TolerancePercent ?? 0,
                        PreweighRequired = rule?.PreweighRequired ?? false,
                        SafetyNotes = rule?.SafetyNotes ?? "",
                        HandlingNotes = rule?.HandlingNotes ?? "",
                        Status = plan.Workflow == "Separate" ? "Picked" : "Supply",
                    },
                    s
                );
                db.Add(pick);
                db.Add(
                    Ops.Stamp(
                        new Reservation
                        {
                            ProductionOrderId = order.Id,
                            PalletId = target.Id,
                            Quantity = r.Quantity,
                            RequirementQuantity = r.RequirementQuantity,
                            StagingPickId = pick.Id,
                            CommandId = r.CommandId,
                        },
                        s
                    )
                );
                if (r.Gross.HasValue)
                    AddMeasurement(db, s, pick, r.Gross.Value, r.Tare!.Value, r.ScaleReference);
                command.ResultId = pick.Id;
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(command);
            }
        );
        g.MapPost(
            "/picks/{id:guid}/weigh",
            async (Guid id, WeighInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true);
                var sig = Signature("Weigh", id, r);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var prior = await Replay(db, s, r.CommandId, sig);
                if (prior != null)
                    return Results.Ok(prior);
                var pick = await Get<StagingPick>(db, s, id);
                var plan = await Get<StagingPlan>(db, s, pick.StagingPlanId);
                Ops.Check(
                    (await Get<ProductionOrder>(db, s, plan.ProductionOrderId)).Status
                        == "Released",
                    "Order is not released."
                );
                Ops.Check(
                    pick.Status == "Picked" && plan.Workflow == "Separate",
                    "Only a separately picked material can be kitted."
                );
                var expected = decimal.Round(pick.RequirementQuantity * pick.CorrectionFactor, 6);
                var net = r.Gross - r.Tare;
                Ops.Check(
                    Ops.Positive(r.Gross) && Nonnegative(r.Tare) && Ops.Positive(net),
                    "Gross must exceed nonnegative tare; readings support six decimals."
                );
                CheckTolerance(net, expected, pick.TolerancePercent);
                var pallet = await Get<Pallet>(db, s, pick.CurrentPalletId);
                Usable(await Get<Batch>(db, s, pallet.BatchId));
                var res = await Ops.Rows<Reservation>(db, s)
                    .SingleAsync(x => x.StagingPickId == pick.Id);
                Ops.Check(
                    res.Status == "Reserved"
                        && pallet.ReservedQuantity >= res.Quantity
                        && pallet.Quantity >= net,
                    "Picked stock is unavailable or already issued."
                );
                var area = await Get<StagingArea>(db, s, plan.StagingAreaId);
                var staging = await Get<WarehouseBin>(db, s, area.StagingBinId);
                Ops.Check(
                    pallet.Location == staging.Code,
                    "Picked pallet is no longer in its staging bin."
                );
                var command = Record(db, s, r.CommandId, sig, "Weigh", id, plan.Id);
                pallet.ReservedQuantity -= res.Quantity;
                var target = Split(db, s, pallet, net, staging.Code, command.Id, "StagingKit");
                target.ReservedQuantity = net;
                res.PalletId = target.Id;
                res.Quantity = net;
                pick.CurrentPalletId = target.Id;
                pick.Quantity = net;
                pick.Status = "Kitted";
                AddMeasurement(db, s, pick, r.Gross, r.Tare, r.ScaleReference);
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(command);
            }
        );
        g.MapPost(
            "/plans/{id:guid}/putaway",
            async (Guid id, KitMoveInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true);
                var sig = Signature("KitDelivered", id, r);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var prior = await Replay(db, s, r.CommandId, sig);
                if (prior != null)
                    return Results.Ok(prior);
                var plan = await Get<StagingPlan>(db, s, id);
                var order = await Get<ProductionOrder>(db, s, plan.ProductionOrderId);
                Ops.Check(order.Status == "Released", "Order is not released.");
                Ops.Check(
                    Ops.Code(r.KitCode) == plan.KitCode,
                    "Scanned kit code does not match this order."
                );
                var area = await Get<StagingArea>(db, s, plan.StagingAreaId);
                var destination = await Get<WarehouseBin>(db, s, area.SupplyBinId);
                Ops.Check(
                    Ops.Code(r.DestinationBin) == destination.Code,
                    "Scan the configured production supply bin."
                );
                var picks = await Ops.Rows<StagingPick>(db, s)
                    .Where(x =>
                        x.StagingPlanId == id && x.Status != "Returned" && x.Status != "Issued"
                    )
                    .ToListAsync();
                Ops.Check(
                    picks.Count > 0 && picks.All(x => x.Status is "Kitted" or "Supply"),
                    "Weigh all picked materials before delivering the kit."
                );
                var bom = await Ops.Rows<BomLine>(db, s)
                    .Where(x => x.ProductionOrderId == order.Id)
                    .ToListAsync();
                foreach (var line in bom)
                {
                    var consumed = await (
                        from c in Ops.Rows<Consumption>(db, s)
                        join p in Ops.Rows<Pallet>(db, s) on c.PalletId equals p.Id
                        join b in Ops.Rows<Batch>(db, s) on p.BatchId equals b.Id
                        where c.ProductionOrderId == order.Id && b.MaterialId == line.MaterialId
                        select c.RequirementQuantity ?? c.Quantity
                    ).SumAsync();
                    Ops.Check(
                        consumed
                            + picks
                                .Where(x => x.MaterialId == line.MaterialId)
                                .Sum(x => x.RequirementQuantity)
                            >= line.Quantity,
                        "Every BOM material must be allocated and kitted before full-kit delivery."
                    );
                }
                Ops.Check(picks.Any(x => x.Status == "Kitted"), "This kit is already at supply.");
                foreach (
                    var group in picks.Where(x => x.Status == "Kitted").GroupBy(x => x.MaterialId)
                )
                {
                    var first = await Get<Pallet>(db, s, group.First().CurrentPalletId);
                    await CheckCapacity(db, s, first, destination.Code, group.Sum(x => x.Quantity));
                }
                var command = Record(db, s, r.CommandId, sig, "KitDelivered", id, id);
                foreach (var pick in picks.Where(x => x.Status == "Kitted"))
                {
                    var pallet = await Get<Pallet>(db, s, pick.CurrentPalletId);
                    Usable(await Get<Batch>(db, s, pallet.BatchId));
                    db.Add(
                        Move(
                            s,
                            pallet,
                            0,
                            "KitDelivery",
                            pallet.Location,
                            destination.Code,
                            command.Id
                        )
                    );
                    pallet.Location = destination.Code;
                    pick.Status = "Supply";
                }
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(command);
            }
        );
        g.MapPost(
            "/picks/{id:guid}/return",
            async (Guid id, ReturnInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true);
                var sig = Signature("Returned", id, r);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var prior = await Replay(db, s, r.CommandId, sig);
                if (prior != null)
                    return Results.Ok(prior);
                var pick = await Get<StagingPick>(db, s, id);
                var plan = await Get<StagingPlan>(db, s, pick.StagingPlanId);
                Ops.Check(
                    pick.Status is "Picked" or "Kitted" or "Supply",
                    "Issued or already returned material cannot be reverted."
                );
                var destinationCode = Ops.Code(r.DestinationBin);
                var destination = await Ops.Rows<WarehouseBin>(db, s)
                    .SingleOrDefaultAsync(x => x.Code == destinationCode);
                Ops.Check(destination != null, "Choose a configured return bin.");
                var area = await Get<StagingArea>(db, s, plan.StagingAreaId);
                Ops.Check(
                    destination!.Id != area.StagingBinId && destination.Id != area.SupplyBinId,
                    "Return to a warehouse bin outside this staging/supply area."
                );
                var pallet = await Get<Pallet>(db, s, pick.CurrentPalletId);
                var res = await Ops.Rows<Reservation>(db, s)
                    .SingleAsync(x => x.StagingPickId == id);
                Ops.Check(
                    res.Status == "Reserved" && pallet.ReservedQuantity >= res.Quantity,
                    "Reservation has already changed."
                );
                await CheckCapacity(db, s, pallet, destination.Code, pick.Quantity);
                var command = Record(
                    db,
                    s,
                    r.CommandId,
                    sig,
                    "Returned",
                    id,
                    plan.Id,
                    Notes(r.Reason, true)
                );
                pallet.ReservedQuantity -= res.Quantity;
                res.Status = "Cancelled";
                pick.Status = "Returned";
                // Return only the dedicated allocation; any loose stock remains visible and can be transferred separately.
                var target = Split(
                    db,
                    s,
                    pallet,
                    pick.Quantity,
                    destination.Code,
                    command.Id,
                    "StagingReturn"
                );
                command.ResultId = target.Id;
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(command);
            }
        );
    }

    static void CheckTolerance(decimal net, decimal expected, decimal percent)
    {
        var delta = expected * percent / 100m;
        Ops.Check(
            net >= expected - delta && net <= expected + delta,
            $"Net weight {net:0.######} is outside the allowed range {expected - delta:0.######}–{expected + delta:0.######}. Target: {expected:0.######}."
        );
    }

    static void AddMeasurement(
        FactoryDb db,
        OpsScope s,
        StagingPick pick,
        decimal gross,
        decimal tare,
        string? scale
    )
    {
        Ops.Check(
            Ops.Positive(gross) && Nonnegative(tare) && Ops.Positive(gross - tare),
            "Gross must exceed nonnegative tare."
        );
        db.Add(
            Ops.Stamp(
                new StagingMeasurement
                {
                    StagingPickId = pick.Id,
                    Gross = gross,
                    Tare = tare,
                    Net = gross - tare,
                    ExpectedQuantity = decimal.Round(
                        pick.RequirementQuantity * pick.CorrectionFactor,
                        6
                    ),
                    ScaleReference = string.IsNullOrWhiteSpace(scale)
                        ? "Manual reading"
                        : Ops.Name(scale),
                    Method = "Manual",
                },
                s
            )
        );
    }
}
