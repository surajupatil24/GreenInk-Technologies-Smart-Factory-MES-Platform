using Microsoft.EntityFrameworkCore;

namespace GreenInk;

public static partial class OperationsModel
{
    static void ConfigureManufacturing(ModelBuilder b)
    {
        // Concrete tables avoid polymorphic scope/foreign-key ambiguities.
        foreach (
            var type in new[]
            {
                typeof(ProductionResource),
                typeof(ProductionRecipe),
                typeof(ProductionRun),
                typeof(ProductionPhase),
                typeof(ProductionEvent),
                typeof(QualitySpecification),
                typeof(QualitySample),
                typeof(QualityTestReading),
                typeof(QualityCase),
                typeof(FinishedPack),
                typeof(ManufacturingCommand),
            }
        )
            b.Entity(type).HasBaseType((Type?)null);
        Row<ProductionResource>(b);
        Row<ProductionRecipe>(b);
        Row<ProductionRun>(b);
        Row<ProductionPhase>(b);
        Row<ProductionEvent>(b);
        Row<QualitySpecification>(b);
        Row<QualitySample>(b);
        Row<QualityTestReading>(b);
        Row<QualityCase>(b);
        Row<FinishedPack>(b);
        Row<ManufacturingCommand>(b);
        Ref<ProductionRecipe, Material>(b, "MaterialId");
        Ref<ProductionRun, ProductionOrder>(b, "ProductionOrderId");
        Ref<ProductionRun, ProductionRecipe>(b, "ProductionRecipeId");
        Ref<ProductionRun, ProductionResource>(b, "ProductionResourceId");
        Ref<ProductionRun, QualitySpecification>(b, "QualitySpecificationId");
        Ref<ProductionPhase, ProductionRun>(b, "ProductionRunId");
        Ref<ProductionEvent, ProductionRun>(b, "ProductionRunId");
        Ref<QualitySpecification, Material>(b, "MaterialId");
        Ref<QualitySample, QualitySpecification>(b, "QualitySpecificationId");
        Ref<QualitySample, Material>(b, "MaterialId");
        Ref<QualitySample, Batch>(b, "BatchId");
        Ref<QualitySample, ProductionPhase>(b, "ProductionPhaseId");
        Ref<QualitySample, QualitySample>(b, "RetestOfId");
        Ref<QualityTestReading, QualitySample>(b, "QualitySampleId");
        Ref<QualityCase, Batch>(b, "BatchId");
        Ref<QualityCase, ProductionRun>(b, "ProductionRunId");
        Ref<QualityCase, QualitySample>(b, "QualitySampleId");
        Ref<FinishedPack, Pallet>(b, "SourcePalletId");
        Ref<FinishedPack, Pallet>(b, "OutputPalletId");
        Unique<ProductionResource>(b, "Code");
        Unique<ProductionRun>(b, "ProductionOrderId");
        Unique<QualitySample>(b, "Number");
        Unique<ManufacturingCommand>(b, "CommandId");
        Unique<FinishedPack>(b, "OutputPalletId");
        b.Entity<ProductionRecipe>()
            .HasIndex(x => new
            {
                x.TenantId,
                x.PlantId,
                x.Code,
                x.Revision,
            })
            .IsUnique();
        b.Entity<QualitySpecification>()
            .HasIndex(x => new
            {
                x.TenantId,
                x.PlantId,
                x.MaterialId,
                x.Revision,
            })
            .IsUnique();
        b.Entity<ProductionPhase>()
            .HasIndex(x => new
            {
                x.TenantId,
                x.PlantId,
                x.ProductionRunId,
                x.Sequence,
            })
            .IsUnique();
        foreach (
            var type in new[]
            {
                typeof(ProductionRecipe),
                typeof(ProductionPhase),
                typeof(QualitySpecification),
                typeof(QualitySample),
            }
        )
        foreach (var property in type.GetProperties().Where(x => x.Name.EndsWith("Json")))
            b.Entity(type).Property(property.Name).HasColumnType("jsonb").HasMaxLength(65536);
        foreach (
            var type in new[]
            {
                typeof(ProductionRecipe),
                typeof(ProductionPhase),
                typeof(ProductionEvent),
                typeof(QualitySample),
                typeof(QualityTestReading),
                typeof(QualityCase),
            }
        )
        foreach (
            var name in new[]
            {
                "Instructions",
                "Precautions",
                "Notes",
                "ReviewNotes",
                "Reason",
                "RootCause",
                "CorrectiveAction",
                "Resolution",
            }
        )
            if (type.GetProperty(name) != null)
                b.Entity(type).Property(name).HasMaxLength(2000);
        b.Entity<ProductionEvent>().Property(x => x.Notes).HasMaxLength(16000);
        b.Entity<QualityTestReading>().Property(x => x.NumericValue).HasPrecision(20, 6);
        b.Entity<ProductionRun>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Run_State",
                    "\"Status\" IN ('Scheduled','Running','Paused','Completed') AND \"PausedMinutes\" >= 0"
                )
            );
        b.Entity<ProductionPhase>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Phase_State",
                    "\"Sequence\" > 0 AND \"Status\" IN ('Pending','Running','Completed')"
                )
            );
        b.Entity<QualitySample>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Sample_Target",
                    "(\"BatchId\" IS NOT NULL) <> (\"ProductionPhaseId\" IS NOT NULL)"
                )
            );
        b.Entity<QualityTestReading>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Reading_Value",
                    "(\"NumericValue\" IS NOT NULL) <> (\"PassedValue\" IS NOT NULL)"
                )
            );
        b.Entity<QualityCase>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Case_Target",
                    "\"BatchId\" IS NOT NULL OR \"ProductionRunId\" IS NOT NULL"
                )
            );
        b.Entity<FinishedPack>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_FinishedPack_Quantity",
                    "\"PackSize\" > 0 AND \"PackCount\" >= 0 AND \"LooseQuantity\" >= 0 AND \"LooseQuantity\" < \"PackSize\" AND \"Quantity\" > 0 AND \"Quantity\" = \"PackSize\" * \"PackCount\" + \"LooseQuantity\""
                )
            );
    }
}
