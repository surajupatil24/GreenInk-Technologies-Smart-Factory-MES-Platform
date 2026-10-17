using Microsoft.EntityFrameworkCore;

namespace GreenInk;

public static partial class OperationsModel
{
    static void ConfigureDispatch(ModelBuilder b)
    {
        foreach (
            var t in new[]
            {
                typeof(DispatchArea),
                typeof(DispatchInstruction),
                typeof(DispatchPackage),
                typeof(DispatchWarehouse),
                typeof(DispatchRun),
                typeof(DispatchRunLine),
                typeof(DispatchPick),
                typeof(DispatchHandlingUnit),
                typeof(DispatchIssueLine),
                typeof(DispatchReversalLine),
                typeof(DispatchEvent),
                typeof(DispatchCommand),
            }
        )
            b.Entity(t).HasBaseType((Type?)null);
        Row<DispatchArea>(b);
        Row<DispatchInstruction>(b);
        Row<DispatchPackage>(b);
        Row<DispatchWarehouse>(b);
        Row<DispatchRun>(b);
        Row<DispatchRunLine>(b);
        Row<DispatchPick>(b);
        Row<DispatchHandlingUnit>(b);
        Row<DispatchIssueLine>(b);
        Row<DispatchReversalLine>(b);
        Row<DispatchEvent>(b);
        Row<DispatchCommand>(b);
        Ref<DispatchArea, WarehouseBin>(b, "PackBinId");
        Ref<DispatchArea, WarehouseBin>(b, "LoadingBinId");
        Ref<DispatchInstruction, Partner>(b, "CustomerId");
        Ref<DispatchRun, DispatchArea>(b, "DispatchAreaId");
        Ref<DispatchRun, DispatchWarehouse>(b, "DispatchWarehouseId");
        b.Entity<DispatchRun>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.AssignedToId)
            .OnDelete(DeleteBehavior.Restrict);
        Ref<DispatchRunLine, DispatchRun>(b, "DispatchRunId");
        Ref<DispatchRunLine, DeliveryOrder>(b, "DeliveryOrderId");
        Ref<DispatchRunLine, Material>(b, "MaterialId");
        Ref<DispatchRunLine, Partner>(b, "CustomerId");
        Ref<DispatchPick, DispatchRun>(b, "DispatchRunId");
        Ref<DispatchPick, DispatchRunLine>(b, "DispatchRunLineId");
        Ref<DispatchPick, Pallet>(b, "SourcePalletId");
        Ref<DispatchPick, Pallet>(b, "PickedPalletId");
        Ref<DispatchPick, DispatchHandlingUnit>(b, "DispatchHandlingUnitId");
        Ref<DispatchHandlingUnit, DispatchRun>(b, "DispatchRunId");
        Ref<DispatchHandlingUnit, DispatchPackage>(b, "DispatchPackageId");
        Ref<DispatchIssueLine, DispatchRun>(b, "DispatchRunId");
        Ref<DispatchIssueLine, DispatchPick>(b, "DispatchPickId");
        Ref<DispatchIssueLine, Shipment>(b, "ShipmentId");
        Ref<DispatchReversalLine, DispatchIssueLine>(b, "DispatchIssueLineId");
        Ref<DispatchReversalLine, Shipment>(b, "ShipmentId");
        Ref<DispatchEvent, DispatchRun>(b, "DispatchRunId");
        Unique<DispatchArea>(b, "Code");
        Unique<DispatchInstruction>(b, "CustomerId");
        Unique<DispatchPackage>(b, "Code");
        Unique<DispatchWarehouse>(b, "Code");
        Unique<DispatchRun>(b, "Number");
        Unique<DispatchHandlingUnit>(b, "Code");
        Unique<DispatchCommand>(b, "CommandId");
        Unique<DispatchReversalLine>(b, "DispatchIssueLineId");
        b.Entity<DispatchRunLine>()
            .HasIndex(x => new
            {
                x.TenantId,
                x.PlantId,
                x.DeliveryOrderId,
            })
            .IsUnique()
            .HasFilter("\"Active\" AND \"DeliveryOrderId\" IS NOT NULL");
        foreach (
            var t in new[]
            {
                typeof(DispatchRun),
                typeof(DispatchRunLine),
                typeof(DispatchHandlingUnit),
            }
        )
        foreach (var p in t.GetProperties().Where(x => x.Name.EndsWith("Json")))
            b.Entity(t).Property(p.Name).HasColumnType("jsonb").HasMaxLength(16000);
        foreach (var p in new[] { "Picking", "Packing", "Shipping", "Safety" })
            b.Entity<DispatchInstruction>().Property(p).HasMaxLength(2000);
        b.Entity<DispatchEvent>().Property(x => x.Notes).HasMaxLength(16000);
        b.Entity<DispatchHandlingUnit>().Property(x => x.GrossKg).HasPrecision(20, 6);
        b.Entity<DispatchRun>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_DispatchRun_State",
                    "\"Status\" IN ('Planned','Picking','Picked','Packing','Packed','Loading','Loaded','Issued','Cancelled','Reversed') AND \"Kind\" IN ('Customer','Shuttle') AND ((\"Kind\"='Customer' AND \"DispatchWarehouseId\" IS NULL) OR (\"Kind\"='Shuttle' AND \"DispatchWarehouseId\" IS NOT NULL))"
                )
            );
        b.Entity<DispatchPick>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_DispatchPick_Quantity",
                    "\"PickedQuantity\" > 0 AND \"Quantity\" >= 0 AND \"ReturnedQuantity\" >= 0 AND \"Quantity\" + \"ReturnedQuantity\" = \"PickedQuantity\""
                )
            );
        b.Entity<DispatchPackage>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_DispatchPackage_Size",
                    "\"LengthCm\" > 0 AND \"WidthCm\" > 0 AND \"HeightCm\" > 0 AND \"TareKg\" >= 0 AND \"MaximumGrossKg\" > \"TareKg\""
                )
            );
        b.Entity<DispatchInstruction>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_DispatchInstruction_Shelf",
                    "\"MinimumShelfDays\" BETWEEN 0 AND 3650"
                )
            );
        foreach (
            var t in new[]
            {
                typeof(DispatchRunLine),
                typeof(DispatchIssueLine),
                typeof(DispatchReversalLine),
            }
        )
            b.Entity(t)
                .ToTable(x =>
                    x.HasCheckConstraint("CK_" + t.Name + "_Quantity", "\"Quantity\" > 0")
                );
    }
}
