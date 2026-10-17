using Microsoft.EntityFrameworkCore;

namespace GreenInk;

public static partial class OperationsModel
{
    static void ConfigureStaging(ModelBuilder b)
    {
        Row<StagingArea>(b);
        Row<StagingRule>(b);
        Row<BatchStagingFactor>(b);
        Row<StagingPlan>(b);
        Row<StagingPick>(b);
        Row<StagingMeasurement>(b);
        Row<StagingCommand>(b);
        Row<ReplenishmentRequest>(b);
        Row<StagingStockIssue>(b);
        Row<KanbanPolicy>(b);
        Ref<StagingArea, WarehouseBin>(b, "StagingBinId");
        Ref<StagingArea, WarehouseBin>(b, "SupplyBinId");
        Ref<StagingRule, Material>(b, "MaterialId");
        Ref<BatchStagingFactor, Batch>(b, "BatchId");
        Ref<StagingPlan, ProductionOrder>(b, "ProductionOrderId");
        Ref<StagingPlan, StagingArea>(b, "StagingAreaId");
        Ref<StagingPick, StagingPlan>(b, "StagingPlanId");
        Ref<StagingPick, Material>(b, "MaterialId");
        Ref<StagingPick, Pallet>(b, "SourcePalletId");
        Ref<StagingPick, Pallet>(b, "PickedPalletId");
        Ref<StagingPick, Pallet>(b, "CurrentPalletId");
        Ref<Reservation, StagingPick>(b, "StagingPickId");
        Ref<StagingMeasurement, StagingPick>(b, "StagingPickId");
        Ref<StagingCommand, StagingPlan>(b, "StagingPlanId");
        Ref<ReplenishmentRequest, Material>(b, "MaterialId");
        Ref<ReplenishmentRequest, WarehouseBin>(b, "DestinationBinId");
        Ref<ReplenishmentRequest, StagingPlan>(b, "StagingPlanId");
        Ref<StagingStockIssue, Pallet>(b, "PalletId");
        Ref<KanbanPolicy, Material>(b, "MaterialId");
        Ref<KanbanPolicy, WarehouseBin>(b, "BinId");
        Unique<StagingArea>(b, "Code");
        Unique<StagingRule>(b, "MaterialId");
        Unique<BatchStagingFactor>(b, "BatchId");
        Unique<StagingPlan>(b, "ProductionOrderId");
        Unique<StagingPlan>(b, "KitCode");
        Unique<StagingCommand>(b, "CommandId");
        b.Entity<KanbanPolicy>()
            .HasIndex(x => new
            {
                x.TenantId,
                x.PlantId,
                x.BinId,
                x.MaterialId,
            })
            .IsUnique();
        b.Entity<Reservation>().Property(x => x.RequirementQuantity).HasPrecision(20, 6);
        b.Entity<Consumption>().Property(x => x.RequirementQuantity).HasPrecision(20, 6);
        b.Entity<StagingStockIssue>().Property(x => x.CountedQuantity).HasPrecision(20, 6);
        b.Entity<StagingRule>().Property(x => x.SafetyNotes).HasMaxLength(2000);
        b.Entity<StagingRule>().Property(x => x.HandlingNotes).HasMaxLength(2000);
        b.Entity<StagingPick>().Property(x => x.SafetyNotes).HasMaxLength(2000);
        b.Entity<StagingPick>().Property(x => x.HandlingNotes).HasMaxLength(2000);
        b.Entity<StagingStockIssue>().Property(x => x.Notes).HasMaxLength(2000);
        b.Entity<StagingStockIssue>().Property(x => x.Resolution).HasMaxLength(2000);
        b.Entity<StagingCommand>().Property(x => x.Notes).HasMaxLength(2000);
        b.Entity<StagingPlan>().Property(x => x.ApprovalNotes).HasMaxLength(2000);
        b.Entity<ReplenishmentRequest>().Property(x => x.Reason).HasMaxLength(2000);
        b.Entity<BatchStagingFactor>().Property(x => x.Evidence).HasMaxLength(2000);
        b.Entity<StagingPick>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_StagingPick_Staging",
                    "\"Quantity\" > 0 AND \"RequirementQuantity\" > 0 AND \"CorrectionFactor\" > 0"
                )
            );
        b.Entity<StagingMeasurement>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_StagingMeasurement_Staging",
                    "\"Gross\" > \"Tare\" AND \"Tare\" >= 0 AND \"Net\" = \"Gross\" - \"Tare\" AND \"ExpectedQuantity\" > 0"
                )
            );
        b.Entity<ReplenishmentRequest>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_ReplenishmentRequest_Staging",
                    "\"Quantity\" > 0 AND \"FulfilledQuantity\" >= 0 AND \"FulfilledQuantity\" <= \"Quantity\""
                )
            );
        b.Entity<KanbanPolicy>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_KanbanPolicy_Staging",
                    "\"LowStock\" >= 0 AND \"LowStock\" <= \"ReorderStock\" AND \"ReorderStock\" <= \"MaxStock\" AND \"MaxStock\" > 0"
                )
            );
        b.Entity<BatchStagingFactor>()
            .ToTable(t => t.HasCheckConstraint("CK_BatchStagingFactor_Staging", "\"Factor\" > 0"));
        b.Entity<StagingRule>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_StagingRule_Staging",
                    "\"PackSize\" >= 0 AND \"TolerancePercent\" >= 0 AND \"TolerancePercent\" <= 100"
                )
            );
        b.Entity<Reservation>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Reservation_Staging",
                    "\"RequirementQuantity\" IS NULL OR \"RequirementQuantity\" > 0"
                )
            );
        b.Entity<Consumption>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Consumption_Staging",
                    "\"RequirementQuantity\" IS NULL OR \"RequirementQuantity\" > 0"
                )
            );
    }
}
