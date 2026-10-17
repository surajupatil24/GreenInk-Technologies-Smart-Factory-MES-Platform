namespace GreenInk;

public abstract class ManufacturingRow : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
}

public class ProductionResource : ManufacturingRow
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Maintenance { get; set; }
}

public class ProductionRecipe : ManufacturingRow
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public Guid MaterialId { get; set; }
    public int Revision { get; set; }
    public string Status { get; set; } = "Draft";
    public string Precautions { get; set; } = "";
    public string PhasesJson { get; set; } = "[]";
    public DateTimeOffset? PublishedAt { get; set; }
}

public class ProductionRun : ManufacturingRow
{
    public Guid ProductionOrderId { get; set; }
    public Guid ProductionRecipeId { get; set; }
    public Guid ProductionResourceId { get; set; }
    public Guid? QualitySpecificationId { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public bool Priority { get; set; }
    public string Status { get; set; } = "Scheduled";
    public bool SafetyConfirmed { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? PausedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public decimal PausedMinutes { get; set; }
}

public class ProductionPhase : ManufacturingRow
{
    public Guid ProductionRunId { get; set; }
    public int Sequence { get; set; }
    public string Name { get; set; } = "";
    public string Instructions { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public bool RequiresSample { get; set; }
    public string ChecksJson { get; set; } = "[]";
    public string ParametersJson { get; set; } = "[]";
    public string EvidenceJson { get; set; } = "{}";
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class ProductionEvent : ManufacturingRow
{
    public Guid ProductionRunId { get; set; }
    public Guid ActorId { get; set; }
    public string Kind { get; set; } = "";
    public string Notes { get; set; } = "";
}

public class QualitySpecification : ManufacturingRow
{
    public Guid MaterialId { get; set; }
    public string Name { get; set; } = "";
    public int Revision { get; set; }
    public string Status { get; set; } = "Draft";
    public string TestsJson { get; set; } = "[]";
}

public class QualitySample : ManufacturingRow
{
    public string Number { get; set; } =
        "QS-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
    public Guid QualitySpecificationId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid? BatchId { get; set; }
    public Guid? ProductionPhaseId { get; set; }
    public Guid? RetestOfId { get; set; }
    public string Status { get; set; } = "Collected";
    public string TestsJson { get; set; } = "[]";
    public string Notes { get; set; } = "";
    public string ReviewNotes { get; set; } = "";
    public Guid? ReviewedBy { get; set; }
    public DateTimeOffset? HandedOverAt { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }
    public DateTimeOffset? TestingAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
}

public class QualityTestReading : ManufacturingRow
{
    public Guid QualitySampleId { get; set; }
    public string TestCode { get; set; } = "";
    public decimal? NumericValue { get; set; }
    public bool? PassedValue { get; set; }
    public bool InSpec { get; set; }
    public string Notes { get; set; } = "";
    public Guid ActorId { get; set; }
}

public class QualityCase : ManufacturingRow
{
    public Guid? BatchId { get; set; }
    public Guid? ProductionRunId { get; set; }
    public Guid? QualitySampleId { get; set; }
    public string Severity { get; set; } = "Major";
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "Open";
    public string RootCause { get; set; } = "";
    public string CorrectiveAction { get; set; } = "";
    public string Resolution { get; set; } = "";
    public DateTimeOffset? ClosedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
}

public class FinishedPack : ManufacturingRow
{
    public Guid SourcePalletId { get; set; }
    public Guid OutputPalletId { get; set; }
    public decimal PackSize { get; set; }
    public int PackCount { get; set; }
    public decimal LooseQuantity { get; set; }
    public decimal Quantity { get; set; }
    public string PackagingReference { get; set; } = "";
}

public class ManufacturingCommand : ManufacturingRow
{
    public Guid CommandId { get; set; }
    public string Signature { get; set; } = "";
    public string Kind { get; set; } = "";
    public Guid ResultId { get; set; }
}
