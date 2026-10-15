using System.Data;
using Microsoft.EntityFrameworkCore;

namespace GreenInk;

public static class InboundInventoryEndpoints
{
    public record MaterialInput(Guid PlantId, string Code, string Name, string Uom, string Kind);

    public record PartnerInput(Guid PlantId, string Code, string Name, string Kind);

    public record BinInput(Guid PlantId, string Code, string Name, string Warehouse);

    public record PurchaseInput(
        Guid PlantId,
        string Number,
        Guid SupplierId,
        Guid MaterialId,
        decimal Quantity
    );

    public record ReceiptInput(
        Guid PlantId,
        Guid PurchaseOrderId,
        decimal Quantity,
        string SupplierBatch,
        string BatchNumber,
        DateTimeOffset? ExpiryDate,
        Guid CommandId
    );

    public record TransferInput(Guid PlantId, Guid PalletId, string Location, Guid CommandId);

    public record AdjustmentInput(
        Guid PlantId,
        Guid PalletId,
        decimal Quantity,
        string Reason,
        Guid CommandId
    );

    public static void MapInboundInventory(this WebApplication app)
    {
        var g = app.MapGroup("/api/operations").RequireAuthorization();
        g.MapGet(
            "/materials",
            async (Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                return Results.Ok(
                    await Ops.Rows<Material>(db, s)
                        .AsNoTracking()
                        .OrderBy(x => x.Code)
                        .Take(200)
                        .ToListAsync()
                );
            }
        );
        g.MapPost(
            "/materials",
            async (MaterialInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true);
                var code = Ops.Code(r.Code);
                var name = Ops.Name(r.Name);
                var uom = Ops.Name(r.Uom);
                Ops.Check(uom.Length <= 16, "UOM must be at most 16 characters.");
                Ops.Check(r.Kind is "Raw" or "Finished", "Kind must be Raw or Finished.");
                Ops.Check(
                    !await Ops.Rows<Material>(db, s).AnyAsync(x => x.Code == code),
                    "Material code already exists."
                );
                var row = Ops.Stamp(
                    new Material
                    {
                        Code = code,
                        Name = name,
                        Uom = uom,
                        Kind = r.Kind,
                    },
                    s
                );
                db.Add(row);
                Ops.Audit(db, s, "MaterialCreated", row.Id);
                await db.SaveChangesAsync();
                return Results.Ok(row);
            }
        );
        g.MapGet(
            "/partners",
            async (Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                return Results.Ok(
                    await Ops.Rows<Partner>(db, s)
                        .AsNoTracking()
                        .OrderBy(x => x.Code)
                        .Take(200)
                        .ToListAsync()
                );
            }
        );
        g.MapPost(
            "/partners",
            async (PartnerInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true);
                var code = Ops.Code(r.Code);
                Ops.Check(r.Kind is "Supplier" or "Customer", "Kind must be Supplier or Customer.");
                Ops.Check(
                    !await Ops.Rows<Partner>(db, s).AnyAsync(x => x.Code == code),
                    "Partner code already exists."
                );
                var row = Ops.Stamp(
                    new Partner
                    {
                        Code = code,
                        Name = Ops.Name(r.Name),
                        Kind = r.Kind,
                    },
                    s
                );
                db.Add(row);
                Ops.Audit(db, s, "PartnerCreated", row.Id);
                await db.SaveChangesAsync();
                return Results.Ok(row);
            }
        );
        g.MapGet(
            "/bins",
            async (Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                return Results.Ok(
                    await Ops.Rows<WarehouseBin>(db, s)
                        .AsNoTracking()
                        .OrderBy(x => x.Code)
                        .Take(200)
                        .ToListAsync()
                );
            }
        );
        g.MapPost(
            "/bins",
            async (BinInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true);
                var code = Ops.Code(r.Code);
                Ops.Check(
                    !await Ops.Rows<WarehouseBin>(db, s).AnyAsync(x => x.Code == code),
                    "Bin code already exists."
                );
                var row = Ops.Stamp(
                    new WarehouseBin
                    {
                        Code = code,
                        Name = Ops.Name(r.Name),
                        Warehouse = Ops.Name(r.Warehouse),
                    },
                    s
                );
                db.Add(row);
                Ops.Audit(db, s, "BinCreated", row.Id);
                await db.SaveChangesAsync();
                return Results.Ok(row);
            }
        );
        g.MapGet(
            "/purchase-orders",
            async (Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                return Results.Ok(
                    await Ops.Rows<PurchaseOrder>(db, s)
                        .AsNoTracking()
                        .OrderByDescending(x => x.CreatedAt)
                        .Take(200)
                        .ToListAsync()
                );
            }
        );
        g.MapPost(
            "/purchase-orders",
            async (PurchaseInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true);
                var number = Ops.Code(r.Number);
                Ops.Check(
                    Ops.Positive(r.Quantity),
                    "Quantity must be positive with at most six decimals."
                );
                Ops.Check(
                    await Ops.Rows<Partner>(db, s)
                        .AnyAsync(x => x.Id == r.SupplierId && x.Kind == "Supplier"),
                    "Select a supplier in this plant."
                );
                Ops.Check(
                    await Ops.Rows<Material>(db, s)
                        .AnyAsync(x => x.Id == r.MaterialId && x.Kind == "Raw"),
                    "Select a raw material in this plant."
                );
                Ops.Check(
                    !await Ops.Rows<PurchaseOrder>(db, s).AnyAsync(x => x.Number == number),
                    "Purchase order number already exists."
                );
                var row = Ops.Stamp(
                    new PurchaseOrder
                    {
                        Number = number,
                        SupplierId = r.SupplierId,
                        MaterialId = r.MaterialId,
                        Quantity = r.Quantity,
                    },
                    s
                );
                db.Add(row);
                Ops.Audit(db, s, "PurchaseOrderCreated", row.Id);
                await db.SaveChangesAsync();
                return Results.Ok(row);
            }
        );
        g.MapGet(
            "/receipts",
            async (Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                return Results.Ok(
                    await Ops.Rows<GoodsReceipt>(db, s)
                        .AsNoTracking()
                        .OrderByDescending(x => x.CreatedAt)
                        .Take(200)
                        .ToListAsync()
                );
            }
        );
        g.MapGet(
            "/batches",
            async (Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                return Results.Ok(
                    await Ops.Rows<Batch>(db, s)
                        .AsNoTracking()
                        .OrderByDescending(x => x.CreatedAt)
                        .Take(200)
                        .ToListAsync()
                );
            }
        );
        g.MapGet(
            "/pallets",
            async (Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                return Results.Ok(
                    await Ops.Rows<Pallet>(db, s)
                        .AsNoTracking()
                        .OrderByDescending(x => x.CreatedAt)
                        .Take(200)
                        .ToListAsync()
                );
            }
        );
        g.MapGet(
            "/movements",
            async (Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                return Results.Ok(
                    await Ops.Rows<StockMovement>(db, s)
                        .AsNoTracking()
                        .OrderByDescending(x => x.CreatedAt)
                        .Take(200)
                        .ToListAsync()
                );
            }
        );
        g.MapPost(
            "/receipts",
            async (ReceiptInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true);
                Ops.Command(r.CommandId);
                var supplierBatch = Ops.Name(r.SupplierBatch);
                var batchNumber = Ops.Code(r.BatchNumber);
                Ops.Check(
                    Ops.Positive(r.Quantity),
                    "Quantity must be positive with at most six decimals."
                );
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var existing = await Ops.Rows<GoodsReceipt>(db, s)
                    .SingleOrDefaultAsync(x => x.CommandId == r.CommandId);
                if (existing is not null)
                {
                    var b = await Ops.Rows<Batch>(db, s).SingleAsync(x => x.Id == existing.BatchId);
                    Ops.Check(
                        existing.PurchaseOrderId == r.PurchaseOrderId
                            && existing.Quantity == r.Quantity
                            && existing.SupplierBatch == supplierBatch
                            && b.Number == batchNumber
                            && b.ExpiryDate == r.ExpiryDate,
                        "commandId was already used for a different receipt."
                    );
                    return Results.Ok(existing);
                }
                Ops.Check(
                    r.ExpiryDate is null || r.ExpiryDate > DateTimeOffset.UtcNow,
                    "Expiry date must be in the future."
                );
                var po = await Ops.Rows<PurchaseOrder>(db, s)
                    .SingleOrDefaultAsync(x => x.Id == r.PurchaseOrderId);
                Ops.Check(po is not null, "Purchase order not found in this plant.");
                Ops.Check(po!.Status is "Open" or "Partial", "Purchase order is not open.");
                Ops.Check(
                    po.ReceivedQuantity + r.Quantity <= po.Quantity,
                    "Receipt exceeds outstanding purchase order quantity."
                );
                Ops.Check(
                    !await Ops.Rows<Batch>(db, s).AnyAsync(x => x.Number == batchNumber),
                    "Batch number already exists."
                );
                var batch = Ops.Stamp(
                    new Batch
                    {
                        MaterialId = po.MaterialId,
                        Number = batchNumber,
                        Kind = "Raw",
                        QualityStatus = "Hold",
                        ExpiryDate = r.ExpiryDate,
                    },
                    s
                );
                var pallet = Ops.Stamp(
                    new Pallet
                    {
                        BatchId = batch.Id,
                        Code = Guid.NewGuid().ToString("N"),
                        Quantity = r.Quantity,
                        Location = "RECEIVING",
                    },
                    s
                );
                var receipt = Ops.Stamp(
                    new GoodsReceipt
                    {
                        PurchaseOrderId = po.Id,
                        BatchId = batch.Id,
                        PalletId = pallet.Id,
                        Quantity = r.Quantity,
                        SupplierBatch = supplierBatch,
                        CommandId = r.CommandId,
                    },
                    s
                );
                po.ReceivedQuantity += r.Quantity;
                po.Status = po.ReceivedQuantity == po.Quantity ? "Received" : "Partial";
                db.AddRange(
                    batch,
                    pallet,
                    receipt,
                    Ops.Stamp(
                        new StockMovement
                        {
                            PalletId = pallet.Id,
                            Quantity = r.Quantity,
                            Kind = "Receipt",
                            ToLocation = pallet.Location,
                            Reference = po.Number,
                            CommandId = r.CommandId,
                        },
                        s
                    )
                );
                Ops.Audit(db, s, "GoodsReceived", receipt.Id);
                Ops.Audit(db, s, "BatchCreated", batch.Id);
                Ops.Audit(db, s, "PalletCreated", pallet.Id);
                Ops.Audit(db, s, "PurchaseOrderReceived", po.Id);
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(receipt);
            }
        );
        g.MapPost(
            "/transfers",
            async (TransferInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true);
                Ops.Command(r.CommandId);
                var location = Ops.Code(r.Location);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var existing = await Ops.Rows<StockMovement>(db, s)
                    .SingleOrDefaultAsync(x => x.CommandId == r.CommandId);
                if (existing is not null)
                {
                    Ops.Check(
                        existing.Kind == "Transfer"
                            && existing.PalletId == r.PalletId
                            && existing.ToLocation == location,
                        "commandId was already used for a different movement."
                    );
                    return Results.Ok(existing);
                }
                Ops.Check(
                    await Ops.Rows<WarehouseBin>(db, s).AnyAsync(x => x.Code == location),
                    "Destination must be a configured bin in this plant."
                );
                var pallet = await Ops.Rows<Pallet>(db, s)
                    .SingleOrDefaultAsync(x => x.Id == r.PalletId);
                Ops.Check(pallet is not null, "Pallet not found in this plant.");
                Ops.Check(pallet!.Quantity > 0, "Cannot transfer an empty pallet.");
                Ops.Check(pallet.Location != location, "Pallet is already at that location.");
                var movement = Ops.Stamp(
                    new StockMovement
                    {
                        PalletId = pallet.Id,
                        Kind = "Transfer",
                        Quantity = 0,
                        FromLocation = pallet.Location,
                        ToLocation = location,
                        CommandId = r.CommandId,
                    },
                    s
                );
                pallet.Location = location;
                db.Add(movement);
                Ops.Audit(db, s, "PalletTransferred", pallet.Id);
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(movement);
            }
        );
        g.MapPost(
            "/adjustments",
            async (AdjustmentInput r, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, r.PlantId, true, true);
                Ops.Command(r.CommandId);
                Ops.Check(
                    r.Quantity != 0
                        && r.Quantity >= -1000000000m
                        && r.Quantity <= 1000000000m
                        && decimal.Round(r.Quantity, 6) == r.Quantity,
                    "Adjustment must be nonzero with at most six decimals."
                );
                var reason = Ops.Name(r.Reason);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var existing = await Ops.Rows<StockMovement>(db, s)
                    .SingleOrDefaultAsync(x => x.CommandId == r.CommandId);
                if (existing is not null)
                {
                    Ops.Check(
                        existing.Kind == "Adjustment"
                            && existing.PalletId == r.PalletId
                            && existing.Quantity == r.Quantity
                            && existing.Reference == reason,
                        "commandId was already used for a different movement."
                    );
                    return Results.Ok(existing);
                }
                var pallet = await Ops.Rows<Pallet>(db, s)
                    .SingleOrDefaultAsync(x => x.Id == r.PalletId);
                Ops.Check(pallet is not null, "Pallet not found in this plant.");
                Ops.Check(
                    pallet!.Quantity + r.Quantity >= pallet.ReservedQuantity
                        && pallet.Quantity + r.Quantity >= 0,
                    "Adjustment would reduce stock below zero or reserved quantity."
                );
                Ops.Check(
                    pallet.Quantity + r.Quantity <= 1000000000m,
                    "Stock quantity limit exceeded."
                );
                pallet.Quantity += r.Quantity;
                var movement = Ops.Stamp(
                    new StockMovement
                    {
                        PalletId = pallet.Id,
                        Kind = "Adjustment",
                        Quantity = r.Quantity,
                        FromLocation = pallet.Location,
                        ToLocation = pallet.Location,
                        Reference = reason,
                        CommandId = r.CommandId,
                    },
                    s
                );
                db.Add(movement);
                Ops.Audit(db, s, "StockAdjusted", pallet.Id);
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(movement);
            }
        );
        g.MapGet(
            "/inventory",
            async (Guid plantId, string? barcode, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                var rows =
                    from p in Ops.Rows<Pallet>(db, s).AsNoTracking()
                    join b in Ops.Rows<Batch>(db, s).AsNoTracking() on p.BatchId equals b.Id
                    join m in Ops.Rows<Material>(db, s).AsNoTracking() on b.MaterialId equals m.Id
                    where barcode == null || p.Code == barcode
                    select new
                    {
                        pallet = p,
                        batch = b,
                        material = m,
                    };
                return Results.Ok(
                    await rows.OrderBy(x => x.pallet.CreatedAt).Take(200).ToListAsync()
                );
            }
        );
        g.MapGet(
            "/allocation",
            async (Guid plantId, Guid materialId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                var now = DateTimeOffset.UtcNow;
                var rows =
                    from p in Ops.Rows<Pallet>(db, s).AsNoTracking()
                    join b in Ops.Rows<Batch>(db, s).AsNoTracking() on p.BatchId equals b.Id
                    join m in Ops.Rows<Material>(db, s).AsNoTracking() on b.MaterialId equals m.Id
                    where
                        b.MaterialId == materialId
                        && b.QualityStatus == "Released"
                        && (b.ExpiryDate == null || b.ExpiryDate > now)
                        && p.Quantity > p.ReservedQuantity
                    select new
                    {
                        pallet = p,
                        batch = b,
                        material = m,
                    };
                return Results.Ok(
                    await rows.OrderBy(x => x.batch.ExpiryDate == null)
                        .ThenBy(x => x.batch.ExpiryDate)
                        .ThenBy(x => x.pallet.CreatedAt)
                        .Take(200)
                        .ToListAsync()
                );
            }
        );
    }
}
