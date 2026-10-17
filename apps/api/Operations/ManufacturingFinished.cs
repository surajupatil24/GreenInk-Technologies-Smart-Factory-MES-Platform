using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace GreenInk;

public static partial class ManufacturingEndpoints
{
    static void MapFinished(RouteGroupBuilder g)
    {
        g.MapPost(
            "/packs",
            (PackInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "PackFinished",
                    null,
                    r,
                    false,
                    async s =>
                    {
                        var source = await Get<Pallet>(db, s, r.SourcePalletId);
                        var batch = await Get<Batch>(db, s, source.BatchId);
                        Ops.Check(
                            r.PalletCode == source.Code,
                            "Scan the selected source pallet label."
                        );
                        UsableFinished(batch);
                        await ReleaseGuard(db, s, batch.Id);
                        Ops.Check(
                            Ops.Positive(r.PackSize)
                                && r.PackCount >= 0
                                && r.PackCount <= 100000
                                && r.LooseQuantity >= 0
                                && r.LooseQuantity < r.PackSize
                                && Number(r.LooseQuantity),
                            "Supply a positive pack size, valid count and loose quantity smaller than one pack."
                        );
                        var quantity = r.PackSize * r.PackCount + r.LooseQuantity;
                        Ops.Check(
                            Ops.Positive(quantity)
                                && source.Quantity - source.ReservedQuantity >= quantity,
                            "Packing exceeds free source stock or quantity limits."
                        );
                        Ops.Check(
                            !await Ops.Rows<StagingStockIssue>(db, s)
                                .AnyAsync(x => x.PalletId == source.Id && x.Status == "Open"),
                            "Resolve the stock issue before packing."
                        );
                        var reference = Text(r.PackagingReference, 160, true);
                        source.Quantity -= quantity;
                        var target = Ops.Stamp(
                            new Pallet
                            {
                                BatchId = batch.Id,
                                Code =
                                    "FGP-" + Guid.NewGuid().ToString("N")[..16].ToUpperInvariant(),
                                Quantity = quantity,
                                Location = source.Location,
                            },
                            s
                        );
                        var row = Ops.Stamp(
                            new FinishedPack
                            {
                                SourcePalletId = source.Id,
                                OutputPalletId = target.Id,
                                PackSize = r.PackSize,
                                PackCount = r.PackCount,
                                LooseQuantity = r.LooseQuantity,
                                Quantity = quantity,
                                PackagingReference = reference,
                            },
                            s
                        );
                        db.AddRange(target, row);
                        db.Add(PackMove(s, source, -quantity, "PackOut", row.Id, Guid.NewGuid()));
                        db.Add(PackMove(s, target, quantity, "PackIn", row.Id, Guid.NewGuid()));
                        return row.Id;
                    }
                )
        );
        g.MapPost(
            "/putaway",
            (PutawayInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "FinishedPutaway",
                    null,
                    r,
                    false,
                    async s =>
                    {
                        var pallet = await Get<Pallet>(db, s, r.PalletId);
                        var batch = await Get<Batch>(db, s, pallet.BatchId);
                        UsableFinished(batch);
                        await ReleaseGuard(db, s, batch.Id);
                        Ops.Check(
                            pallet.Code == r.PalletCode,
                            "Scan the selected finished pallet label."
                        );
                        var destination = Ops.Code(r.DestinationBin);
                        Ops.Check(
                            await Ops.Rows<WarehouseBin>(db, s)
                                .AnyAsync(x => x.Code == destination),
                            "Scan a configured destination bin."
                        );
                        Ops.Check(
                            pallet.Quantity > 0 && pallet.ReservedQuantity == 0,
                            "Putaway requires nonempty, unreserved finished stock."
                        );
                        Ops.Check(
                            pallet.Location != destination,
                            "Pallet is already at that destination."
                        );
                        Ops.Check(
                            !await Ops.Rows<StagingStockIssue>(db, s)
                                .AnyAsync(x => x.PalletId == pallet.Id && x.Status == "Open"),
                            "Resolve the stock issue before putaway."
                        );
                        await StagingEndpoints.CheckCapacity(
                            db,
                            s,
                            pallet,
                            destination,
                            pallet.Quantity
                        );
                        var movement = PackMove(
                            s,
                            pallet,
                            0,
                            "FinishedPutaway",
                            pallet.Id,
                            Guid.NewGuid()
                        );
                        movement.ToLocation = destination;
                        db.Add(movement);
                        pallet.Location = destination;
                        return pallet.Id;
                    }
                )
        );
        g.MapGet(
            "/packs/{id:guid}/label",
            async (Guid id, Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                var pack = await Get<FinishedPack>(db, s, id);
                var pallet = await Get<Pallet>(db, s, pack.OutputPalletId);
                var batch = await Get<Batch>(db, s, pallet.BatchId);
                var material = await Get<Material>(db, s, batch.MaterialId);
                using var generator = new QRCodeGenerator();
                using var data = generator.CreateQrCode(pallet.Code, QRCodeGenerator.ECCLevel.Q);
                using var svg = new SvgQRCode(data);
                return Results.Ok(
                    new
                    {
                        pack,
                        palletCode = pallet.Code,
                        batchNumber = batch.Number,
                        material = material.Code,
                        materialName = material.Name,
                        uom = material.Uom,
                        batch.QualityStatus,
                        batch.ExpiryDate,
                        qrSvg = svg.GetGraphic(5),
                        previewOnly = true,
                    }
                );
            }
        );
    }

    static void UsableFinished(Batch batch)
    {
        Ops.Check(
            batch.Kind == "Finished" && batch.QualityStatus == "Released",
            "Packing and finished putaway require a released finished batch."
        );
        Ops.Check(
            batch.ExpiryDate == null || batch.ExpiryDate > DateTimeOffset.UtcNow,
            "Finished stock has expired."
        );
    }

    static StockMovement PackMove(
        OpsScope s,
        Pallet pallet,
        decimal quantity,
        string kind,
        Guid reference,
        Guid command
    ) =>
        Ops.Stamp(
            new StockMovement
            {
                PalletId = pallet.Id,
                Quantity = quantity,
                Kind = kind,
                FromLocation = pallet.Location,
                ToLocation = pallet.Location,
                Reference = reference.ToString(),
                CommandId = command,
            },
            s
        );
}
