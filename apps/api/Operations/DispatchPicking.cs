using Microsoft.EntityFrameworkCore;

namespace GreenInk;

public static partial class DispatchEndpoints
{
    static void MapPicking(RouteGroupBuilder g)
    {
        g.MapPost(
            "/runs/{id:guid}/picks",
            (Guid id, PickInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "Pick",
                    id,
                    r,
                    false,
                    async s =>
                    {
                        var run = await Run(db, s, id);
                        Ops.Check(
                            run.Status == "Picking",
                            "Start picking before confirming stock."
                        );
                        Ops.Check(
                            r.InstructionsConfirmed && r.SafetyConfirmed,
                            "Confirm picking instructions and PPE/safety requirements."
                        );
                        var area = Parse<DispatchArea>(run.SettingsJson);
                        Ops.Check(
                            r.Lines is { Length: > 0 and <= 50 }
                                && r.Lines.All(x => x != null && Ops.Positive(x.Quantity)),
                            "Supply 1–50 positive pick lines."
                        );
                        Ops.Check(
                            area.PickMode == "Group" || r.Lines!.Length == 1,
                            "This area picks one delivery line at a time."
                        );
                        Ops.Check(
                            await Ops.Rows<DispatchPick>(db, s)
                                .CountAsync(x => x.DispatchRunId == id) + r.Lines!.Length
                                <= 500,
                            "Dispatch pick-history limit reached. Return stock and plan a new dispatch if necessary."
                        );
                        foreach (var group in r.Lines.GroupBy(x => x.DispatchRunLineId))
                        {
                            var line = await Get<DispatchRunLine>(db, s, group.Key);
                            Ops.Check(
                                line.DispatchRunId == id && line.Active,
                                "Pick line does not belong to this active dispatch."
                            );
                            var picked = await Ops.Rows<DispatchPick>(db, s)
                                .Where(x => x.DispatchRunLineId == line.Id)
                                .SumAsync(x => x.Quantity);
                            Ops.Check(
                                area.AllowOverpick
                                    || picked + group.Sum(x => x.Quantity) <= line.Quantity,
                                "Picking exceeds the delivery requirement."
                            );
                        }
                        var packBin = await Get<WarehouseBin>(db, s, area.PackBinId);
                        Guid result = id;
                        foreach (var input in r.Lines)
                        {
                            var line = await Get<DispatchRunLine>(db, s, input.DispatchRunLineId);
                            var source = await Get<Pallet>(db, s, input.PalletId);
                            Ops.Check(
                                source.Code == input.PalletCode
                                    && source.Location == input.SourceBin,
                                "Scan the matching source bin and pallet label."
                            );
                            var batch = await Get<Batch>(db, s, source.BatchId);
                            await Usable(db, s, line, batch);
                            Ops.Check(
                                source.Quantity - source.ReservedQuantity >= input.Quantity,
                                "Insufficient free source stock."
                            );
                            Ops.Check(
                                !await Ops.Rows<StagingStockIssue>(db, s)
                                    .AnyAsync(x => x.PalletId == source.Id && x.Status == "Open"),
                                "Resolve the stock issue before picking."
                            );
                            await StagingEndpoints.CheckCapacity(
                                db,
                                s,
                                source,
                                packBin.Code,
                                input.Quantity
                            );
                            var target = Ops.Stamp(
                                new Pallet
                                {
                                    BatchId = batch.Id,
                                    Code =
                                        "DP-"
                                        + Guid.NewGuid().ToString("N")[..16].ToUpperInvariant(),
                                    Location = packBin.Code,
                                    Quantity = input.Quantity,
                                    ReservedQuantity = input.Quantity,
                                },
                                s
                            );
                            source.Quantity -= input.Quantity;
                            var pick = Ops.Stamp(
                                new DispatchPick
                                {
                                    DispatchRunId = id,
                                    DispatchRunLineId = line.Id,
                                    SourcePalletId = source.Id,
                                    PickedPalletId = target.Id,
                                    PickedQuantity = input.Quantity,
                                    Quantity = input.Quantity,
                                },
                                s
                            );
                            db.AddRange(target, pick);
                            db.Add(
                                Move(
                                    s,
                                    source,
                                    -input.Quantity,
                                    "DispatchPickOut",
                                    id,
                                    source.Location,
                                    packBin.Code
                                )
                            );
                            db.Add(
                                Move(
                                    s,
                                    target,
                                    input.Quantity,
                                    "DispatchPickIn",
                                    id,
                                    source.Location,
                                    packBin.Code
                                )
                            );
                            Event(
                                db,
                                s,
                                id,
                                "Picked",
                                $"{input.Quantity:0.######} from {source.Code} / {source.Location} to {target.Code} / {packBin.Code}."
                            );
                            await db.SaveChangesAsync();
                            result = pick.Id;
                        }
                        return result;
                    }
                )
        );
        g.MapPost(
            "/picks/{id:guid}/return",
            (Guid id, ReturnInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "ReturnPick",
                    id,
                    r,
                    true,
                    async s =>
                    {
                        var pick = await Get<DispatchPick>(db, s, id);
                        var run = await Run(db, s, pick.DispatchRunId);
                        Ops.Check(!Terminal(run), "Issued stock requires a goods-issue reversal.");
                        Ops.Check(
                            pick.DispatchHandlingUnitId == null && pick.Status == "Picked",
                            "Unpack and unload the allocation before returning it."
                        );
                        Ops.Check(
                            Ops.Positive(r.Quantity) && r.Quantity <= pick.Quantity,
                            "Return quantity must fit the picked allocation."
                        );
                        var target = await Get<Pallet>(db, s, pick.PickedPalletId);
                        var source = await Get<Pallet>(db, s, pick.SourcePalletId);
                        Ops.Check(
                            target.Code == r.PalletCode && source.Location == r.DestinationBin,
                            "Scan the picked pallet and its current original-source bin."
                        );
                        Ops.Check(
                            target.Quantity >= r.Quantity
                                && target.ReservedQuantity >= r.Quantity
                                && source.Quantity + r.Quantity <= 1000000000m,
                            "Stock balance cannot support this return."
                        );
                        var reason = Text(r.Reason, 2000, true);
                        await StagingEndpoints.CheckCapacity(
                            db,
                            s,
                            target,
                            source.Location,
                            r.Quantity
                        );
                        target.Quantity -= r.Quantity;
                        target.ReservedQuantity -= r.Quantity;
                        source.Quantity += r.Quantity;
                        pick.Quantity -= r.Quantity;
                        pick.ReturnedQuantity += r.Quantity;
                        if (pick.Quantity == 0)
                            pick.Status = "Returned";
                        run.Status = "Picking";
                        db.Add(
                            Move(
                                s,
                                target,
                                -r.Quantity,
                                "DispatchReturnOut",
                                run.Id,
                                target.Location,
                                source.Location
                            )
                        );
                        db.Add(
                            Move(
                                s,
                                source,
                                r.Quantity,
                                "DispatchReturnIn",
                                run.Id,
                                target.Location,
                                source.Location
                            )
                        );
                        Event(
                            db,
                            s,
                            run.Id,
                            "PickReturned",
                            $"{r.Quantity:0.######} returned to {source.Code} / {source.Location}. {reason}"
                        );
                        return id;
                    }
                )
        );
    }

    public static async Task CheckFreeMovement(FactoryDb db, OpsScope s, Guid palletId) =>
        Ops.Check(
            !await Ops.Rows<DispatchPick>(db, s)
                .AnyAsync(x =>
                    x.PickedPalletId == palletId
                    && x.Quantity > 0
                    && x.Status != "Issued"
                    && x.Status != "Reversed"
                ),
            "This pallet is allocated to dispatch. Use its packing, loading or return workflow."
        );
}
