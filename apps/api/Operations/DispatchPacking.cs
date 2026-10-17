using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace GreenInk;

public static partial class DispatchEndpoints
{
    static void MapPacking(RouteGroupBuilder g)
    {
        g.MapPost(
            "/runs/{id:guid}/units",
            (Guid id, UnitInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "OpenHandlingUnit",
                    id,
                    r,
                    false,
                    async s =>
                    {
                        var run = await Run(db, s, id);
                        Ops.Check(
                            run.Status == "Packing",
                            "Start packing before opening handling units."
                        );
                        Ops.Check(
                            await Ops.Rows<DispatchHandlingUnit>(db, s)
                                .CountAsync(x => x.DispatchRunId == id) < 200,
                            "This dispatch reached its handling-unit limit."
                        );
                        var package = await Get<DispatchPackage>(db, s, r.DispatchPackageId);
                        var device = Text(
                            r.TemperatureDevice,
                            160,
                            package.TemperatureDeviceRequired
                        );
                        Ops.Check(
                            device == ""
                                || !await Ops.Rows<DispatchHandlingUnit>(db, s)
                                    .AnyAsync(x =>
                                        x.TemperatureDevice == device
                                        && x.Status != "Cancelled"
                                        && x.Status != "Issued"
                                        && x.Status != "Reversed"
                                    ),
                            "Temperature device is already assigned to an active handling unit."
                        );
                        var unit = Ops.Stamp(
                            new DispatchHandlingUnit
                            {
                                DispatchRunId = id,
                                DispatchPackageId = package.Id,
                                PackageJson = Serialize(package),
                                TemperatureDevice = device,
                            },
                            s
                        );
                        db.Add(unit);
                        Event(db, s, id, "HandlingUnitOpened", unit.Code);
                        return unit.Id;
                    }
                )
        );
        g.MapPost(
            "/units/{id:guid}/pack",
            (Guid id, PackInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "Pack",
                    id,
                    r,
                    false,
                    async s =>
                    {
                        var unit = await Get<DispatchHandlingUnit>(db, s, id);
                        var run = await Run(db, s, unit.DispatchRunId);
                        Ops.Check(
                            run.Status == "Packing" && unit.Status == "Open",
                            "Choose an open handling unit during packing."
                        );
                        Ops.Check(
                            r.InstructionsConfirmed,
                            "Confirm the customer's packing instructions."
                        );
                        Ops.Check(
                            r.PickIds is { Length: > 0 and <= 50 }
                                && r.PalletCodes != null
                                && r.PalletCodes.Length == r.PickIds.Length
                                && r.PickIds.Distinct().Count() == r.PickIds.Length,
                            "Supply distinct allocations and their matching scanned labels."
                        );
                        var policy = Parse<DispatchArea>(run.SettingsJson);
                        var productMode = policy.PackMode == "Product";
                        for (var i = 0; i < r.PickIds!.Length; i++)
                        {
                            var pick = await Get<DispatchPick>(db, s, r.PickIds[i]);
                            Ops.Check(
                                pick.DispatchRunId == run.Id
                                    && pick.Status == "Picked"
                                    && pick.Quantity > 0
                                    && pick.DispatchHandlingUnitId == null,
                                "Allocation is not available for this handling unit."
                            );
                            var line = await Get<DispatchRunLine>(db, s, pick.DispatchRunLineId);
                            if (Parse<DispatchInstruction>(line.InstructionsJson).ProductPacking)
                                productMode = true;
                            var pallet = await Get<Pallet>(db, s, pick.PickedPalletId);
                            Ops.Check(
                                pallet.Code == r.PalletCodes![i],
                                "Scan each picked allocation label in the selected order."
                            );
                            await Usable(db, s, line, await Get<Batch>(db, s, pallet.BatchId));
                            pick.DispatchHandlingUnitId = id;
                            pick.Status = "Packed";
                        }
                        Ops.Check(
                            !productMode || r.PickIds.Length == 1,
                            "Product packing validates one picked allocation at a time."
                        );
                        Event(
                            db,
                            s,
                            run.Id,
                            "Packed",
                            $"{r.PickIds.Length} allocation(s) checked into {unit.Code}."
                        );
                        return id;
                    }
                )
        );
        g.MapPost(
            "/units/{id:guid}/state",
            (Guid id, UnitStateInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "HandlingUnitState",
                    id,
                    r,
                    false,
                    async s =>
                    {
                        var unit = await Get<DispatchHandlingUnit>(db, s, id);
                        var run = await Run(db, s, unit.DispatchRunId);
                        Ops.Check(!Terminal(run), "This dispatch is already completed.");
                        Ops.Check(unit.Code == r.UnitCode, "Scan the matching handling-unit code.");
                        var picks = await Ops.Rows<DispatchPick>(db, s)
                            .Where(x => x.DispatchHandlingUnitId == id && x.Quantity > 0)
                            .ToListAsync();
                        var policy = Parse<DispatchArea>(run.SettingsJson);
                        var packBin = await Get<WarehouseBin>(db, s, policy.PackBinId);
                        var loadingBin = await Get<WarehouseBin>(db, s, policy.LoadingBinId);
                        if (r.Action == "Close")
                        {
                            Ops.Check(
                                run.Status == "Packing" && unit.Status == "Open" && picks.Count > 0,
                                "Only a nonempty open handling unit can close during packing."
                            );
                            var package = Parse<DispatchPackage>(unit.PackageJson);
                            Ops.Check(
                                r.GrossKg.HasValue
                                    && Ops.Positive(r.GrossKg.Value)
                                    && r.GrossKg > package.TareKg
                                    && r.GrossKg <= package.MaximumGrossKg,
                                "Gross weight must exceed tare and fit the package maximum."
                            );
                            decimal knownNet = 0;
                            foreach (var pick in picks)
                            {
                                var line = await Get<DispatchRunLine>(
                                    db,
                                    s,
                                    pick.DispatchRunLineId
                                );
                                var material = await Get<Material>(db, s, line.MaterialId);
                                if (
                                    string.Equals(
                                        material.Uom,
                                        "kg",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                )
                                    knownNet += pick.Quantity;
                            }
                            Ops.Check(
                                r.GrossKg >= knownNet + package.TareKg,
                                "Gross weight cannot be below the known packed net kilograms plus tare."
                            );
                            unit.GrossKg = r.GrossKg;
                            unit.Status = "Closed";
                        }
                        else if (r.Action == "Unpack")
                        {
                            Ops.Check(Manager(s), "Only a manager can unpack a handling unit.");
                            Ops.Check(
                                unit.Status is "Open" or "Closed",
                                "Unload the handling unit before unpacking."
                            );
                            Text(r.Notes, 2000, true);
                            foreach (var pick in picks)
                            {
                                pick.DispatchHandlingUnitId = null;
                                pick.Status = "Picked";
                            }
                            unit.Status = "Cancelled";
                            run.Status = "Packing";
                        }
                        else if (r.Action == "Load")
                        {
                            Ops.Check(
                                run.Status == "Loading"
                                    && unit.Status == "Closed"
                                    && picks.Count > 0,
                                "Start truck loading and select a closed handling unit."
                            );
                            foreach (var pick in picks)
                            {
                                var line = await Get<DispatchRunLine>(
                                    db,
                                    s,
                                    pick.DispatchRunLineId
                                );
                                var pallet = await Get<Pallet>(db, s, pick.PickedPalletId);
                                Ops.Check(
                                    pallet.Location == packBin.Code,
                                    "Picked pallet is no longer in the packing bin."
                                );
                                await Usable(db, s, line, await Get<Batch>(db, s, pallet.BatchId));
                                await StagingEndpoints.CheckCapacity(
                                    db,
                                    s,
                                    pallet,
                                    loadingBin.Code,
                                    pallet.Quantity
                                );
                                db.Add(
                                    Move(
                                        s,
                                        pallet,
                                        0,
                                        "DispatchLoad",
                                        run.Id,
                                        packBin.Code,
                                        loadingBin.Code
                                    )
                                );
                                pallet.Location = loadingBin.Code;
                                pick.Status = "Loaded";
                                await db.SaveChangesAsync();
                            }
                            unit.Status = "Loaded";
                        }
                        else if (r.Action == "Unload")
                        {
                            Ops.Check(Manager(s), "Only a manager can unload a handling unit.");
                            Ops.Check(
                                unit.Status == "Loaded",
                                "Only a loaded handling unit can unload."
                            );
                            Text(r.Notes, 2000, true);
                            foreach (var pick in picks)
                            {
                                var pallet = await Get<Pallet>(db, s, pick.PickedPalletId);
                                Ops.Check(
                                    pallet.Location == loadingBin.Code,
                                    "Pallet is not in the loading bin."
                                );
                                await StagingEndpoints.CheckCapacity(
                                    db,
                                    s,
                                    pallet,
                                    packBin.Code,
                                    pallet.Quantity
                                );
                                db.Add(
                                    Move(
                                        s,
                                        pallet,
                                        0,
                                        "DispatchUnload",
                                        run.Id,
                                        loadingBin.Code,
                                        packBin.Code
                                    )
                                );
                                pallet.Location = packBin.Code;
                                pick.Status = "Packed";
                                await db.SaveChangesAsync();
                            }
                            unit.Status = "Closed";
                            run.Status = "Loading";
                        }
                        else
                            throw new OpsError(400, "Choose Close, Unpack, Load or Unload.");
                        Event(db, s, run.Id, "Unit" + r.Action, $"{unit.Code}. {Text(r.Notes)}");
                        return id;
                    }
                )
        );
        g.MapPost(
            "/runs/{id:guid}/truck",
            (Guid id, TruckInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "TruckLoading",
                    id,
                    r,
                    false,
                    async s =>
                    {
                        var run = await Run(db, s, id);
                        Ops.Check(
                            run.Status == "Packed",
                            "Complete packing before starting truck loading."
                        );
                        Ops.Check(
                            r.InstructionsConfirmed,
                            "Confirm the shipping instructions before loading."
                        );
                        run.Truck = Ops.Name(r.Truck);
                        run.Carrier = Ops.Name(r.Carrier);
                        run.Status = "Loading";
                        Event(db, s, id, "LoadingStarted", $"{run.Truck} · {run.Carrier}");
                        return id;
                    }
                )
        );
        g.MapPost(
            "/runs/{id:guid}/loading-complete",
            (Guid id, CommandInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "LoadingComplete",
                    id,
                    r,
                    false,
                    async s =>
                    {
                        var run = await Run(db, s, id);
                        Ops.Check(run.Status == "Loading", "Start loading first.");
                        var units = await Ops.Rows<DispatchHandlingUnit>(db, s)
                            .Where(x => x.DispatchRunId == id && x.Status != "Cancelled")
                            .ToListAsync();
                        Ops.Check(
                            units.Count > 0 && units.All(x => x.Status == "Loaded"),
                            "Load every active handling unit before completion."
                        );
                        await Balanced(db, s, run);
                        run.Status = "Loaded";
                        Event(db, s, id, "LoadingCompleted");
                        return id;
                    }
                )
        );
        g.MapGet(
            "/units/{id:guid}/label",
            async (Guid id, Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                var unit = await Get<DispatchHandlingUnit>(db, s, id);
                var run = await Run(db, s, unit.DispatchRunId);
                using var generator = new QRCodeGenerator();
                using var data = generator.CreateQrCode(unit.Code, QRCodeGenerator.ECCLevel.Q);
                using var svg = new SvgQRCode(data);
                return Results.Ok(
                    new
                    {
                        unit,
                        runNumber = run.Number,
                        truck = run.Truck,
                        qrSvg = svg.GetGraphic(5),
                        labelType = "Local handling-unit identifier, not a GS1 SSCC",
                        previewOnly = true,
                        erpStatus = "NotConnected",
                    }
                );
            }
        );
    }
}
