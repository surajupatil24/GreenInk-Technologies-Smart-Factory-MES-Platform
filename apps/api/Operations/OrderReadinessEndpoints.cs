using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace GreenInk;

public record MaterialReadiness(
    Guid MaterialId,
    string Code,
    string Name,
    string Uom,
    decimal RequiredQuantity,
    decimal ReservedQuantity,
    decimal IssuedQuantity,
    decimal ToReserve,
    decimal ToIssue,
    decimal AvailableQuantity
);

public record OrderReadiness(
    Guid OrderId,
    string Number,
    string Status,
    decimal PlannedQuantity,
    decimal CompletedQuantity,
    string OutputUom,
    bool CanComplete,
    List<MaterialReadiness> Requirements
);

public static class OrderReadinessEndpoints
{
    public static void MapOrderReadiness(this WebApplication app) =>
        app.MapGet(
                "/api/operations/production-orders/{id:guid}/requirements",
                async (Guid id, Guid plantId, FactoryDb db, ClaimsPrincipal user) =>
                {
                    var scope = await Ops.Require(db, user, plantId);
                    await using var snapshot = await db.Database.BeginTransactionAsync(
                        IsolationLevel.RepeatableRead
                    );
                    var order =
                        await Ops.Rows<ProductionOrder>(db, scope)
                            .AsNoTracking()
                            .SingleOrDefaultAsync(x => x.Id == id)
                        ?? throw new OpsError(404, "Production order not found in this plant.");
                    var output = await Ops.Rows<Material>(db, scope)
                        .AsNoTracking()
                        .SingleAsync(x => x.Id == order.MaterialId);
                    var lines = await (
                        from line in Ops.Rows<BomLine>(db, scope)
                        join material in Ops.Rows<Material>(db, scope)
                            on line.MaterialId equals material.Id
                        where line.ProductionOrderId == id
                        orderby material.Code
                        select new
                        {
                            line.MaterialId,
                            line.Quantity,
                            material.Code,
                            material.Name,
                            material.Uom,
                        }
                    ).ToListAsync();
                    var reserved = await (
                        from r in Ops.Rows<Reservation>(db, scope)
                        join pallet in Ops.Rows<Pallet>(db, scope) on r.PalletId equals pallet.Id
                        join batch in Ops.Rows<Batch>(db, scope) on pallet.BatchId equals batch.Id
                        where r.ProductionOrderId == id && r.Status == "Reserved"
                        group r by batch.MaterialId into amounts
                        select new
                        {
                            MaterialId = amounts.Key,
                            Quantity = amounts.Sum(x => x.RequirementQuantity ?? x.Quantity),
                        }
                    ).ToDictionaryAsync(x => x.MaterialId, x => x.Quantity);
                    var issued = await (
                        from c in Ops.Rows<Consumption>(db, scope)
                        join pallet in Ops.Rows<Pallet>(db, scope) on c.PalletId equals pallet.Id
                        join batch in Ops.Rows<Batch>(db, scope) on pallet.BatchId equals batch.Id
                        where c.ProductionOrderId == id
                        group c by batch.MaterialId into amounts
                        select new
                        {
                            MaterialId = amounts.Key,
                            Quantity = amounts.Sum(x => x.RequirementQuantity ?? x.Quantity),
                        }
                    ).ToDictionaryAsync(x => x.MaterialId, x => x.Quantity);
                    var now = DateTimeOffset.UtcNow;
                    var available = await (
                        from pallet in Ops.Rows<Pallet>(db, scope)
                        join batch in Ops.Rows<Batch>(db, scope) on pallet.BatchId equals batch.Id
                        where
                            batch.Kind == "Raw"
                            && batch.QualityStatus == "Released"
                            && (batch.ExpiryDate == null || batch.ExpiryDate > now)
                            && pallet.Quantity > pallet.ReservedQuantity
                        group pallet by batch.MaterialId into amounts
                        select new
                        {
                            MaterialId = amounts.Key,
                            Quantity = amounts.Sum(x => x.Quantity - x.ReservedQuantity),
                        }
                    ).ToDictionaryAsync(x => x.MaterialId, x => x.Quantity);
                    var requirements = lines
                        .Select(x => new MaterialReadiness(
                            x.MaterialId,
                            x.Code,
                            x.Name,
                            x.Uom,
                            x.Quantity,
                            reserved.GetValueOrDefault(x.MaterialId),
                            issued.GetValueOrDefault(x.MaterialId),
                            Math.Max(
                                0,
                                x.Quantity
                                    - reserved.GetValueOrDefault(x.MaterialId)
                                    - issued.GetValueOrDefault(x.MaterialId)
                            ),
                            Math.Max(0, x.Quantity - issued.GetValueOrDefault(x.MaterialId)),
                            available.GetValueOrDefault(x.MaterialId)
                        ))
                        .ToList();
                    return Results.Ok(
                        new OrderReadiness(
                            id,
                            order.Number,
                            order.Status,
                            order.PlannedQuantity,
                            order.CompletedQuantity,
                            output.Uom,
                            order.Status == "Released"
                                && requirements.Count > 0
                                && requirements.All(x => x.ToIssue == 0),
                            requirements
                        )
                    );
                }
            )
            .RequireAuthorization();
}
