using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GreenInk;

public interface IPlantRow
{
    Guid Id { get; set; }
    Guid TenantId { get; set; }
    Guid PlantId { get; set; }
    DateTimeOffset CreatedAt { get; set; }
    uint Version { get; set; }
}

public class Material : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Uom { get; set; } = "kg";
    public string Kind { get; set; } = "Raw";
}

public class Partner : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "Supplier";
}

public class WarehouseBin : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Warehouse { get; set; } = "";
}

public class PurchaseOrder : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public string Number { get; set; } = "";
    public Guid SupplierId { get; set; }
    public Guid MaterialId { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public string Status { get; set; } = "Open";
}

public class GoodsReceipt : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid BatchId { get; set; }
    public Guid PalletId { get; set; }
    public decimal Quantity { get; set; }
    public Guid CommandId { get; set; }
    public string SupplierBatch { get; set; } = "";
}

public class Batch : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid MaterialId { get; set; }
    public string Number { get; set; } = "";
    public string Kind { get; set; } = "Raw";
    public string QualityStatus { get; set; } = "Hold";
    public DateTimeOffset? ExpiryDate { get; set; }
    public Guid? ProductionOrderId { get; set; }
}

public class Pallet : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid BatchId { get; set; }
    public string Code { get; set; } = "";
    public string Location { get; set; } = "RECEIVING";
    public decimal Quantity { get; set; }
    public decimal ReservedQuantity { get; set; }
}

public class StockMovement : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid PalletId { get; set; }
    public decimal Quantity { get; set; }
    public string Kind { get; set; } = "";
    public string FromLocation { get; set; } = "";
    public string ToLocation { get; set; } = "";
    public string Reference { get; set; } = "";
    public Guid CommandId { get; set; }
}

public class ProductionOrder : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public string Number { get; set; } = "";
    public Guid MaterialId { get; set; }
    public decimal PlannedQuantity { get; set; }
    public decimal CompletedQuantity { get; set; }
    public string Status { get; set; } = "Draft";
}

public class BomLine : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid ProductionOrderId { get; set; }
    public Guid MaterialId { get; set; }
    public decimal Quantity { get; set; }
}

public class Reservation : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid ProductionOrderId { get; set; }
    public Guid PalletId { get; set; }
    public decimal Quantity { get; set; }
    public string Status { get; set; } = "Reserved";
    public Guid CommandId { get; set; }
}

public class Consumption : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid ProductionOrderId { get; set; }
    public Guid PalletId { get; set; }
    public decimal Quantity { get; set; }
    public Guid CommandId { get; set; }
}

public class ProductionConfirmation : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid ProductionOrderId { get; set; }
    public Guid BatchId { get; set; }
    public Guid PalletId { get; set; }
    public decimal Quantity { get; set; }
    public decimal ScrapQuantity { get; set; }
    public Guid CommandId { get; set; }
}

public class DowntimeEvent : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid ProductionOrderId { get; set; }
    public int Minutes { get; set; }
    public string Reason { get; set; } = "";
}

public class QualityInspection : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid BatchId { get; set; }
    public string Decision { get; set; } = "";
    public string Notes { get; set; } = "";
    public Guid ActorId { get; set; }
    public Guid CommandId { get; set; }
}

public class DeliveryOrder : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public string Number { get; set; } = "";
    public Guid CustomerId { get; set; }
    public Guid MaterialId { get; set; }
    public decimal Quantity { get; set; }
    public string Status { get; set; } = "Open";
}

public class Shipment : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid DeliveryOrderId { get; set; }
    public Guid PalletId { get; set; }
    public decimal Quantity { get; set; }
    public string Truck { get; set; } = "";
    public Guid CommandId { get; set; }
}

public class LabelTemplate : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public string Name { get; set; } = "";
    public string Content { get; set; } = "";
    public int Revision { get; set; } = 1;
}

public class PrintJob : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public Guid PalletId { get; set; }
    public Guid LabelTemplateId { get; set; }
    public string Status { get; set; } = "Queued";
    public string Printer { get; set; } = "Local preview";
    public int Copies { get; set; } = 1;
    public Guid? ReprintOfId { get; set; }
    public string Reason { get; set; } = "";
    public string RenderedContent { get; set; } = "";
    public int TemplateRevision { get; set; } = 1;
    public Guid CommandId { get; set; }
}

public class IntegrationMessage : IPlantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint Version { get; set; }
    public string ExternalId { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Payload { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public int Attempts { get; set; }
    public string Error { get; set; } = "";
    public Guid CorrelationId { get; set; }
}
