namespace GreenInk;

public class StagingArea : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public Guid StagingBinId { get; set; }
    public Guid SupplyBinId { get; set; }
    public string Workflow { get; set; } = "Separate";
}

public class StagingRule : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid MaterialId { get; set; }
    public decimal PackSize { get; set; }
    public bool PreweighRequired { get; set; }
    public decimal TolerancePercent { get; set; }
    public bool CorrectionRequired { get; set; }
    public string SafetyNotes { get; set; } = "";
    public string HandlingNotes { get; set; } = "";
}

public class BatchStagingFactor : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid BatchId { get; set; }
    public decimal Factor { get; set; } = 1;
    public string Evidence { get; set; } = "";
}

public class StagingPlan : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid ProductionOrderId { get; set; }
    public Guid StagingAreaId { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public bool Priority { get; set; }
    public bool ApprovalRequired { get; set; }
    public bool Approved { get; set; }
    public string ApprovalNotes { get; set; } = "";
    public string Workflow { get; set; } = "Separate";
    public string KitCode { get; set; } =
        "KIT-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
}

public class StagingPick : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid StagingPlanId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid SourcePalletId { get; set; }
    public Guid PickedPalletId { get; set; }
    public Guid CurrentPalletId { get; set; }
    public decimal Quantity { get; set; }
    public decimal RequirementQuantity { get; set; }
    public decimal CorrectionFactor { get; set; } = 1;
    public decimal TolerancePercent { get; set; }
    public bool PreweighRequired { get; set; }
    public string Status { get; set; } = "Picked";
    public string SafetyNotes { get; set; } = "";
    public string HandlingNotes { get; set; } = "";
}

public class StagingMeasurement : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid StagingPickId { get; set; }
    public decimal Gross { get; set; }
    public decimal Tare { get; set; }
    public decimal Net { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public string ScaleReference { get; set; } = "Manual reading";
    public string Method { get; set; } = "Manual";
}

public class StagingCommand : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid CommandId { get; set; }
    public string Kind { get; set; } = "";
    public string Signature { get; set; } = "";
    public Guid ResultId { get; set; }
    public Guid? StagingPlanId { get; set; }
    public string Notes { get; set; } = "";
}

public class ReplenishmentRequest : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid MaterialId { get; set; }
    public Guid DestinationBinId { get; set; }
    public Guid? StagingPlanId { get; set; }
    public decimal Quantity { get; set; }
    public decimal FulfilledQuantity { get; set; }
    public string Kind { get; set; } = "RMShortage";
    public string Status { get; set; } = "Requested";
    public string Reason { get; set; } = "";
}

public class StagingStockIssue : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid PalletId { get; set; }
    public string Kind { get; set; } = "StockNotFound";
    public string Status { get; set; } = "Open";
    public string Notes { get; set; } = "";
    public decimal SystemQuantity { get; set; }
    public decimal? CountedQuantity { get; set; }
    public string Resolution { get; set; } = "";
}

public class KanbanPolicy : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid BinId { get; set; }
    public Guid MaterialId { get; set; }
    public string Mode { get; set; } = "Static";
    public decimal LowStock { get; set; }
    public decimal ReorderStock { get; set; }
    public decimal MaxStock { get; set; }
}
