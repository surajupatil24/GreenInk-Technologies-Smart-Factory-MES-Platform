namespace GreenInk;

public abstract class DispatchRow : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
}

public class DispatchArea : DispatchRow
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string ShippingPoint { get; set; } = "";
    public Guid PackBinId { get; set; }
    public Guid LoadingBinId { get; set; }
    public string PickMode { get; set; } = "Single";
    public string PackMode { get; set; } = "Product";
    public bool AllowOverpick { get; set; }
}

public class DispatchInstruction : DispatchRow
{
    public Guid CustomerId { get; set; }
    public string Picking { get; set; } = "";
    public string Packing { get; set; } = "";
    public string Shipping { get; set; } = "";
    public string Safety { get; set; } = "";
    public int MinimumShelfDays { get; set; }
    public bool ProductPacking { get; set; }
}

public class DispatchPackage : DispatchRow
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal LengthCm { get; set; }
    public decimal WidthCm { get; set; }
    public decimal HeightCm { get; set; }
    public decimal TareKg { get; set; }
    public decimal MaximumGrossKg { get; set; }
    public bool TemperatureDeviceRequired { get; set; }
}

public class DispatchWarehouse : DispatchRow
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string ReceivingPlant { get; set; } = "";
    public string StorageLocation { get; set; } = "";
    public string Gate { get; set; } = "";
}

public class DispatchRun : DispatchRow
{
    public string Number { get; set; } = "";
    public string Kind { get; set; } = "Customer";
    public Guid DispatchAreaId { get; set; }
    public Guid? DispatchWarehouseId { get; set; }
    public Guid AssignedToId { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public bool Priority { get; set; }
    public string Route { get; set; } = "";
    public string Status { get; set; } = "Planned";
    public string Carrier { get; set; } = "";
    public string Truck { get; set; } = "";
    public string SettingsJson { get; set; } = "{}";
    public DateTimeOffset? IssuedAt { get; set; }
    public DateTimeOffset? ReversedAt { get; set; }
    public string GoodsIssueReference { get; set; } = "";
}

public class DispatchRunLine : DispatchRow
{
    public Guid DispatchRunId { get; set; }
    public Guid? DeliveryOrderId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid? CustomerId { get; set; }
    public decimal Quantity { get; set; }
    public bool Active { get; set; } = true;
    public string InstructionsJson { get; set; } = "{}";
}

public class DispatchPick : DispatchRow
{
    public Guid DispatchRunId { get; set; }
    public Guid DispatchRunLineId { get; set; }
    public Guid SourcePalletId { get; set; }
    public Guid PickedPalletId { get; set; }
    public decimal PickedQuantity { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public Guid? DispatchHandlingUnitId { get; set; }
    public string Status { get; set; } = "Picked";
}

public class DispatchHandlingUnit : DispatchRow
{
    public Guid DispatchRunId { get; set; }
    public Guid DispatchPackageId { get; set; }
    public string Code { get; set; } =
        "HU-" + Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();
    public string PackageJson { get; set; } = "{}";
    public string Status { get; set; } = "Open";
    public string TemperatureDevice { get; set; } = "";
    public decimal? GrossKg { get; set; }
}

public class DispatchIssueLine : DispatchRow
{
    public Guid DispatchRunId { get; set; }
    public Guid DispatchPickId { get; set; }
    public Guid? ShipmentId { get; set; }
    public decimal Quantity { get; set; }
}

public class DispatchReversalLine : DispatchRow
{
    public Guid DispatchIssueLineId { get; set; }
    public Guid? ShipmentId { get; set; }
    public decimal Quantity { get; set; }
}

public class DispatchEvent : DispatchRow
{
    public Guid DispatchRunId { get; set; }
    public Guid ActorId { get; set; }
    public string Kind { get; set; } = "";
    public string Notes { get; set; } = "";
}

public class DispatchCommand : DispatchRow
{
    public Guid CommandId { get; set; }
    public string Signature { get; set; } = "";
    public string Kind { get; set; } = "";
    public Guid ResultId { get; set; }
}
