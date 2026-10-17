using Microsoft.EntityFrameworkCore;

namespace GreenInk;

public static partial class OperationsModel
{
    public static void Configure(ModelBuilder b)
    {
        b.Entity<Plant>().HasAlternateKey(x => new { x.TenantId, x.Id });
        ConfigureStaging(b);
        ConfigureManufacturing(b);
        ConfigureDispatch(b);
        Row<ReceivingPolicy>(b);
        Row<DockStation>(b);
        Row<ReceivingCheck>(b);
        Row<TruckVisit>(b);
        Row<TruckOrder>(b);
        Row<TruckCheckAnswer>(b);
        b.Entity<ReceivingPolicy>().HasIndex(x => new { x.TenantId, x.PlantId }).IsUnique();
        Unique<DockStation>(b, "Code");
        b.Entity<ReceivingCheck>()
            .HasIndex(x => new
            {
                x.TenantId,
                x.PlantId,
                x.Prompt,
                x.Stage,
            })
            .IsUnique();
        b.Entity<GoodsReceipt>().Property(x => x.PackSize).HasPrecision(20, 6);
        b.Entity<GoodsReceipt>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Receipt_Packs",
                    "(\"PackSize\" IS NULL AND \"PackCount\" IS NULL AND \"LooseQuantity\" = 0) OR (\"PackSize\" IS NOT NULL AND \"PackCount\" IS NOT NULL AND \"PackSize\" > 0 AND \"PackCount\" > 0 AND \"LooseQuantity\" >= 0 AND \"Quantity\" = \"PackSize\" * \"PackCount\" + \"LooseQuantity\")"
                )
            );
        b.Entity<TruckVisit>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Visit_Status",
                    "\"Status\" IN ('Expected','Arrived','Docked','Unloading','Unloaded','Out')"
                )
            );
        Unique<TruckVisit>(b, "Token");
        Unique<TruckVisit>(b, "CommandId");
        b.Entity<TruckVisit>().Property(x => x.OrderSignature).HasMaxLength(4000);
        b.Entity<TruckCheckAnswer>().Property(x => x.Notes).HasMaxLength(2000);
        b.Entity<TruckOrder>()
            .HasIndex(x => new
            {
                x.TenantId,
                x.PlantId,
                x.TruckVisitId,
                x.PurchaseOrderId,
            })
            .IsUnique();
        Ref<TruckVisit, DockStation>(b, "DockStationId");
        Ref<TruckOrder, TruckVisit>(b, "TruckVisitId");
        Ref<TruckOrder, PurchaseOrder>(b, "PurchaseOrderId");
        Ref<TruckCheckAnswer, TruckVisit>(b, "TruckVisitId");
        Ref<GoodsReceipt, TruckVisit>(b, "TruckVisitId");
        Row<Material>(b);
        Row<Partner>(b);
        Row<WarehouseBin>(b);
        Row<PurchaseOrder>(b);
        Row<GoodsReceipt>(b);
        Row<Batch>(b);
        Row<Pallet>(b);
        Row<StockMovement>(b);
        Row<ProductionOrder>(b);
        Row<BomLine>(b);
        Row<Reservation>(b);
        Row<Consumption>(b);
        Row<ProductionConfirmation>(b);
        Row<DowntimeEvent>(b);
        Row<QualityInspection>(b);
        Row<DeliveryOrder>(b);
        Row<Shipment>(b);
        Row<LabelTemplate>(b);
        Row<PrintJob>(b);
        Row<IntegrationMessage>(b);
        Unique<Material>(b, "Code");
        Unique<Partner>(b, "Code");
        Unique<WarehouseBin>(b, "Code");
        Unique<PurchaseOrder>(b, "Number");
        Unique<Batch>(b, "Number");
        Unique<Pallet>(b, "Code");
        Unique<ProductionOrder>(b, "Number");
        Unique<DeliveryOrder>(b, "Number");
        Unique<LabelTemplate>(b, "Name");
        Unique<IntegrationMessage>(b, "ExternalId");
        Unique<GoodsReceipt>(b, "CommandId");
        Unique<StockMovement>(b, "CommandId");
        Unique<Reservation>(b, "CommandId");
        Unique<Consumption>(b, "CommandId");
        Unique<ProductionConfirmation>(b, "CommandId");
        Unique<QualityInspection>(b, "CommandId");
        Unique<Shipment>(b, "CommandId");
        Unique<PrintJob>(b, "CommandId");
        b.Entity<BomLine>()
            .HasIndex(x => new
            {
                x.TenantId,
                x.PlantId,
                x.ProductionOrderId,
                x.MaterialId,
            })
            .IsUnique();
        Ref<PurchaseOrder, Partner>(b, "SupplierId");
        Ref<PurchaseOrder, Material>(b, "MaterialId");
        Ref<GoodsReceipt, PurchaseOrder>(b, "PurchaseOrderId");
        Ref<GoodsReceipt, Batch>(b, "BatchId");
        Ref<GoodsReceipt, Pallet>(b, "PalletId");
        Ref<Batch, Material>(b, "MaterialId");
        Ref<Batch, ProductionOrder>(b, "ProductionOrderId");
        Ref<Pallet, Batch>(b, "BatchId");
        Ref<StockMovement, Pallet>(b, "PalletId");
        Ref<ProductionOrder, Material>(b, "MaterialId");
        Ref<BomLine, ProductionOrder>(b, "ProductionOrderId");
        Ref<BomLine, Material>(b, "MaterialId");
        Ref<Reservation, ProductionOrder>(b, "ProductionOrderId");
        Ref<Reservation, Pallet>(b, "PalletId");
        Ref<Consumption, ProductionOrder>(b, "ProductionOrderId");
        Ref<Consumption, Pallet>(b, "PalletId");
        Ref<ProductionConfirmation, ProductionOrder>(b, "ProductionOrderId");
        Ref<ProductionConfirmation, Batch>(b, "BatchId");
        Ref<ProductionConfirmation, Pallet>(b, "PalletId");
        Ref<DowntimeEvent, ProductionOrder>(b, "ProductionOrderId");
        Ref<QualityInspection, Batch>(b, "BatchId");
        Ref<DeliveryOrder, Partner>(b, "CustomerId");
        Ref<DeliveryOrder, Material>(b, "MaterialId");
        Ref<Shipment, DeliveryOrder>(b, "DeliveryOrderId");
        Ref<Shipment, Pallet>(b, "PalletId");
        Ref<PrintJob, Pallet>(b, "PalletId");
        Ref<PrintJob, LabelTemplate>(b, "LabelTemplateId");
        Ref<PrintJob, PrintJob>(b, "ReprintOfId");
        b.Entity<IntegrationMessage>().Property(x => x.Payload).HasMaxLength(32768);
        b.Entity<PrintJob>().Property(x => x.RenderedContent).HasMaxLength(32768);
        b.Entity<LabelTemplate>().Property(x => x.Content).HasMaxLength(4000);
        b.Entity<QualityInspection>().Property(x => x.Notes).HasMaxLength(2000);
        b.Entity<IntegrationMessage>().Property(x => x.Error).HasMaxLength(2000);
        b.Entity<Pallet>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Pallet_Stock",
                    "\"Quantity\" >= 0 AND \"ReservedQuantity\" >= 0 AND \"ReservedQuantity\" <= \"Quantity\""
                )
            );
        b.Entity<PurchaseOrder>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_PO_Quantities",
                    "\"Quantity\" > 0 AND \"ReceivedQuantity\" >= 0 AND \"ReceivedQuantity\" <= \"Quantity\""
                )
            );
        foreach (
            var type in new[]
            {
                typeof(GoodsReceipt),
                typeof(BomLine),
                typeof(Reservation),
                typeof(Consumption),
                typeof(ProductionConfirmation),
                typeof(DeliveryOrder),
                typeof(Shipment),
            }
        )
            b.Entity(type)
                .ToTable(t =>
                    t.HasCheckConstraint("CK_" + type.Name + "_Positive", "\"Quantity\" > 0")
                );
        b.Entity<ProductionOrder>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Production_Quantity",
                    "\"PlannedQuantity\" > 0 AND \"CompletedQuantity\" >= 0 AND \"CompletedQuantity\" <= \"PlannedQuantity\""
                )
            );
    }

    static void Row<T>(ModelBuilder b)
        where T : class, IPlantRow
    {
        var e = b.Entity<T>();
        e.HasKey(x => x.Id);
        e.HasAlternateKey(x => new
        {
            x.TenantId,
            x.PlantId,
            x.Id,
        });
        e.HasOne<Plant>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.PlantId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        e.HasIndex(x => new
        {
            x.TenantId,
            x.PlantId,
            x.CreatedAt,
        });
        e.Property(x => x.Version).IsRowVersion();
        foreach (var p in typeof(T).GetProperties())
        {
            if (p.PropertyType == typeof(string))
                e.Property(p.Name).HasMaxLength(160);
            if (p.PropertyType == typeof(decimal))
                e.Property(p.Name).HasPrecision(20, 6);
        }
    }

    static void Ref<T, R>(ModelBuilder b, string field)
        where T : class, IPlantRow
        where R : class, IPlantRow =>
        b.Entity<T>()
            .HasOne<R>()
            .WithMany()
            .HasForeignKey("TenantId", "PlantId", field)
            .HasPrincipalKey("TenantId", "PlantId", "Id")
            .OnDelete(DeleteBehavior.Restrict);

    static void Unique<T>(ModelBuilder b, string field)
        where T : class, IPlantRow =>
        b.Entity<T>().HasIndex("TenantId", "PlantId", field).IsUnique();
}
