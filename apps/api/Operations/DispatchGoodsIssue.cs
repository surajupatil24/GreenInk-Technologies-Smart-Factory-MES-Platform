using Microsoft.EntityFrameworkCore;

namespace GreenInk;

public static partial class DispatchEndpoints
{
    static void MapGoodsIssue(RouteGroupBuilder g)
    {
        g.MapPost(
            "/runs/{id:guid}/goods-issue",
            (Guid id, StateInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "GoodsIssue",
                    id,
                    r,
                    true,
                    async s =>
                    {
                        var run = await Run(db, s, id);
                        Ops.Check(
                            run.Status == "Loaded" && r.Confirmed,
                            "Confirm local goods issue only after loading completion."
                        );
                        await Balanced(db, s, run);
                        var area = Parse<DispatchArea>(run.SettingsJson);
                        var loading = await Get<WarehouseBin>(db, s, area.LoadingBinId);
                        var picks = await Ops.Rows<DispatchPick>(db, s)
                            .Where(x => x.DispatchRunId == id && x.Quantity > 0)
                            .ToListAsync();
                        Ops.Check(picks.Count > 0, "No picked stock to issue.");
                        foreach (var pick in picks)
                        {
                            Ops.Check(
                                pick.Status == "Loaded" && pick.DispatchHandlingUnitId.HasValue,
                                "Every picked allocation must be loaded."
                            );
                            var unit = await Get<DispatchHandlingUnit>(
                                db,
                                s,
                                pick.DispatchHandlingUnitId!.Value
                            );
                            Ops.Check(unit.Status == "Loaded", "Handling unit is not loaded.");
                            var line = await Get<DispatchRunLine>(db, s, pick.DispatchRunLineId);
                            var pallet = await Get<Pallet>(db, s, pick.PickedPalletId);
                            await Usable(db, s, line, await Get<Batch>(db, s, pallet.BatchId));
                            Ops.Check(
                                pallet.Location == loading.Code
                                    && pallet.Quantity >= pick.Quantity
                                    && pallet.ReservedQuantity >= pick.Quantity,
                                "Picked stock balance or loading location changed."
                            );
                            pallet.Quantity -= pick.Quantity;
                            pallet.ReservedQuantity -= pick.Quantity;
                            pick.Status = "Issued";
                            Shipment? shipment = null;
                            if (line.DeliveryOrderId.HasValue)
                            {
                                var order = await Get<DeliveryOrder>(
                                    db,
                                    s,
                                    line.DeliveryOrderId.Value
                                );
                                Ops.Check(order.Status == "Open", "Delivery order is not open.");
                                shipment = Ops.Stamp(
                                    new Shipment
                                    {
                                        DeliveryOrderId = order.Id,
                                        PalletId = pallet.Id,
                                        Quantity = pick.Quantity,
                                        Truck = run.Truck,
                                        CommandId = Guid.NewGuid(),
                                    },
                                    s
                                );
                                db.Add(shipment);
                            }
                            db.Add(
                                Ops.Stamp(
                                    new DispatchIssueLine
                                    {
                                        DispatchRunId = id,
                                        DispatchPickId = pick.Id,
                                        ShipmentId = shipment?.Id,
                                        Quantity = pick.Quantity,
                                    },
                                    s
                                )
                            );
                            db.Add(
                                Move(
                                    s,
                                    pallet,
                                    -pick.Quantity,
                                    run.Kind == "Shuttle" ? "WarehouseDispatch" : "Dispatch",
                                    id,
                                    loading.Code,
                                    ""
                                )
                            );
                        }
                        foreach (
                            var line in await Ops.Rows<DispatchRunLine>(db, s)
                                .Where(x => x.DispatchRunId == id && x.DeliveryOrderId != null)
                                .ToListAsync()
                        )
                            (await Get<DeliveryOrder>(db, s, line.DeliveryOrderId!.Value)).Status =
                                "Shipped";
                        foreach (
                            var unit in await Ops.Rows<DispatchHandlingUnit>(db, s)
                                .Where(x => x.DispatchRunId == id && x.Status == "Loaded")
                                .ToListAsync()
                        )
                            unit.Status = "Issued";
                        run.Status = "Issued";
                        run.IssuedAt = DateTimeOffset.UtcNow;
                        run.GoodsIssueReference =
                            "LOCAL-PGI-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
                        Event(
                            db,
                            s,
                            id,
                            "LocalGoodsIssue",
                            $"{run.GoodsIssueReference}. {Text(r.Notes)} Local stock posting; no SAP or destination receipt confirmation."
                        );
                        return id;
                    }
                )
        );
        g.MapPost(
            "/runs/{id:guid}/reverse",
            (Guid id, StateInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "ReverseGoodsIssue",
                    id,
                    r,
                    true,
                    async s =>
                    {
                        var run = await Run(db, s, id);
                        Ops.Check(
                            run.Status == "Issued" && r.Confirmed,
                            "Only issued dispatch can be reversed with returned-stock confirmation."
                        );
                        var reason = Text(r.Notes, 2000, true);
                        var area = Parse<DispatchArea>(run.SettingsJson);
                        var returnBin = await Get<WarehouseBin>(db, s, area.PackBinId);
                        var issues = await Ops.Rows<DispatchIssueLine>(db, s)
                            .Where(x => x.DispatchRunId == id)
                            .ToListAsync();
                        Ops.Check(issues.Count > 0, "No goods issue exists for this dispatch.");
                        foreach (var issue in issues)
                        {
                            var pick = await Get<DispatchPick>(db, s, issue.DispatchPickId);
                            var pallet = await Get<Pallet>(db, s, pick.PickedPalletId);
                            Ops.Check(
                                pallet.ReservedQuantity == 0
                                    && pallet.Quantity == 0
                                    && pallet.Quantity + issue.Quantity <= 1000000000m,
                                "Pallet balance cannot support reversal."
                            );
                            var batch = await Get<Batch>(db, s, pallet.BatchId);
                            await StagingEndpoints.CheckCapacity(
                                db,
                                s,
                                new Pallet { BatchId = batch.Id, Location = "" },
                                returnBin.Code,
                                issue.Quantity
                            );
                            var from = pallet.Location;
                            pallet.Quantity += issue.Quantity;
                            pallet.Location = returnBin.Code;
                            batch.QualityStatus = "Hold";
                            pick.Status = "Reversed";
                            db.Add(
                                Ops.Stamp(
                                    new DispatchReversalLine
                                    {
                                        DispatchIssueLineId = issue.Id,
                                        ShipmentId = issue.ShipmentId,
                                        Quantity = issue.Quantity,
                                    },
                                    s
                                )
                            );
                            db.Add(
                                Move(
                                    s,
                                    pallet,
                                    issue.Quantity,
                                    "DispatchReversal",
                                    id,
                                    from,
                                    returnBin.Code
                                )
                            );
                            await db.SaveChangesAsync();
                        }
                        foreach (
                            var line in await Ops.Rows<DispatchRunLine>(db, s)
                                .Where(x => x.DispatchRunId == id)
                                .ToListAsync()
                        )
                        {
                            line.Active = false;
                            if (line.DeliveryOrderId.HasValue)
                                (
                                    await Get<DeliveryOrder>(db, s, line.DeliveryOrderId.Value)
                                ).Status = "Open";
                        }
                        foreach (
                            var unit in await Ops.Rows<DispatchHandlingUnit>(db, s)
                                .Where(x => x.DispatchRunId == id && x.Status == "Issued")
                                .ToListAsync()
                        )
                            unit.Status = "Reversed";
                        run.Status = "Reversed";
                        run.ReversedAt = DateTimeOffset.UtcNow;
                        Event(
                            db,
                            s,
                            id,
                            "GoodsIssueReversed",
                            $"{reason}. Physical return confirmed by manager; restored stock is on quality hold. No external ERP reversal was transmitted."
                        );
                        return id;
                    }
                )
        );
    }
}
