namespace GreenInk;

public class ReceivingPolicy : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public bool RequireTruck { get; set; }
    public bool RequireDeliveryNote { get; set; }
}

public class DockStation : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Maintenance { get; set; }
}

public class ReceivingCheck : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public string Prompt { get; set; } = "";
    public string Stage { get; set; } = "Arrival";
    public bool Required { get; set; } = true;
    public bool RequirePass { get; set; } = true;
    public bool Active { get; set; } = true;
}

public class TruckVisit : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public string TruckNumber { get; set; } = "";
    public string DriverName { get; set; } = "";
    public string Token { get; set; } = Guid.NewGuid().ToString("N");
    public string Status { get; set; } = "Expected";
    public DateTimeOffset ScheduledAt { get; set; }
    public DateTimeOffset? ArrivedAt { get; set; }
    public DateTimeOffset? UnloadingStartedAt { get; set; }
    public DateTimeOffset? UnloadingCompletedAt { get; set; }
    public DateTimeOffset? ExitedAt { get; set; }
    public Guid? DockStationId { get; set; }
    public Guid CommandId { get; set; }
    public string OrderSignature { get; set; } = "";
}

public class TruckOrder : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid TruckVisitId { get; set; }
    public Guid PurchaseOrderId { get; set; }
}

public class TruckCheckAnswer : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid TruckVisitId { get; set; }
    public string Prompt { get; set; } = "";
    public string Stage { get; set; } = "Arrival";
    public bool Required { get; set; }
    public bool RequirePass { get; set; }
    public bool? Passed { get; set; }
    public string Notes { get; set; } = "";
    public DateTimeOffset? AnsweredAt { get; set; }
}
