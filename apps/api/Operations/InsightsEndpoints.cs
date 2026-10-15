using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace GreenInk;

public interface ILabelCodeRenderer
{
    string Render(string value);
}

public sealed class LocalQrLabelCodeRenderer : ILabelCodeRenderer
{
    public string Render(string value)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q);
        using var code = new SvgQRCode(data);
        return code.GetGraphic(4);
    }
}

public static class InsightsEndpoints
{
    public record InspectRequest(
        Guid PlantId,
        Guid BatchId,
        string Decision,
        string Notes,
        Guid CommandId
    );

    public record TemplateRequest(Guid PlantId, string Name, string Content);

    public record PrintRequest(
        Guid PlantId,
        Guid PalletId,
        Guid LabelTemplateId,
        int Copies,
        string Printer,
        Guid CommandId
    );

    public record ReprintRequest(Guid PlantId, string Reason, Guid CommandId);

    public record MessageRequest(Guid PlantId, string ExternalId, string Kind, string Payload);

    public record ProcessRequest(Guid PlantId);

    public static void MapInsights(this WebApplication app)
    {
        var g = app.MapGroup("/api/operations").RequireAuthorization();
        g.MapGet(
            "/quality-inspections",
            async (Guid plantId, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, plantId);
                return Results.Ok(
                    await Ops.Rows<QualityInspection>(db, s)
                        .AsNoTracking()
                        .OrderByDescending(x => x.CreatedAt)
                        .Take(200)
                        .ToListAsync()
                );
            }
        );
        g.MapPost(
            "/quality-inspections",
            async (InspectRequest r, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, r.PlantId, true, true);
                Ops.Command(r.CommandId);
                Ops.Check(
                    r.Decision is "Released" or "Rejected" or "Hold",
                    "Decision must be Released, Rejected or Hold."
                );
                Ops.Check(
                    r.Notes is not null && r.Notes.Length <= 2000,
                    "Notes must be at most 2000 characters."
                );
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var prior = await Ops.Rows<QualityInspection>(db, s)
                    .SingleOrDefaultAsync(x => x.CommandId == r.CommandId);
                if (prior is not null)
                {
                    Ops.Check(
                        prior.BatchId == r.BatchId
                            && prior.Decision == r.Decision
                            && prior.Notes == r.Notes,
                        "commandId already used for a different inspection."
                    );
                    return Results.Ok(prior);
                }
                var batch =
                    await Ops.Rows<Batch>(db, s).SingleOrDefaultAsync(x => x.Id == r.BatchId)
                    ?? throw new OpsError(404, "Batch not found.");
                Ops.Check(
                    batch.QualityStatus == "Hold" && r.Decision != "Hold"
                        || batch.QualityStatus == "Released" && r.Decision == "Hold",
                    "Only held batches can be released/rejected; only released batches can be quarantined."
                );
                Ops.Check(
                    await Ops.Rows<Pallet>(db, s)
                        .AnyAsync(x => x.BatchId == batch.Id && x.Quantity > 0),
                    "The batch has no on-hand stock to inspect."
                );
                batch.QualityStatus = r.Decision;
                var row = Ops.Stamp(
                    new QualityInspection
                    {
                        BatchId = batch.Id,
                        Decision = r.Decision,
                        Notes = r.Notes!,
                        ActorId = s.ActorId,
                        CommandId = r.CommandId,
                    },
                    s
                );
                db.Add(row);
                Ops.Audit(db, s, "quality." + r.Decision.ToLowerInvariant(), batch.Id);
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(row);
            }
        );
        g.MapGet("/traceability", Trace);
        g.MapGet("/reports", Reports);
        g.MapGet(
            "/label-templates",
            async (Guid plantId, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, plantId);
                return Results.Ok(
                    await Ops.Rows<LabelTemplate>(db, s)
                        .AsNoTracking()
                        .OrderByDescending(x => x.CreatedAt)
                        .Take(200)
                        .ToListAsync()
                );
            }
        );
        g.MapPost(
            "/label-templates",
            async (TemplateRequest r, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, r.PlantId, true, true);
                var name = Ops.Name(r.Name);
                Ops.Check(
                    !string.IsNullOrWhiteSpace(r.Content) && r.Content.Length <= 4000,
                    "Template content must contain 1–4000 characters."
                );
                var clean = Regex.Replace(
                    r.Content,
                    @"\{\{(palletCode|batchNumber|materialName|quantity|uom|location)\}\}",
                    ""
                );
                Ops.Check(
                    !clean.Contains("{{") && !clean.Contains("}}"),
                    "Unsupported template variable."
                );
                var row = Ops.Stamp(new LabelTemplate { Name = name, Content = r.Content! }, s);
                db.Add(row);
                Ops.Audit(db, s, "label-template.created", row.Id);
                await db.SaveChangesAsync();
                return Results.Ok(row);
            }
        );
        g.MapGet(
            "/print-jobs",
            async (Guid plantId, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, plantId);
                return Results.Ok(
                    await Ops.Rows<PrintJob>(db, s)
                        .AsNoTracking()
                        .OrderByDescending(x => x.CreatedAt)
                        .Take(200)
                        .ToListAsync()
                );
            }
        );
        g.MapPost(
            "/print-jobs",
            async (PrintRequest r, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, r.PlantId, true);
                await using var snapshot = await db.Database.BeginTransactionAsync(
                    IsolationLevel.RepeatableRead
                );
                Ops.Command(r.CommandId);
                Ops.Check(r.Copies is >= 1 and <= 100, "Copies must be 1–100.");
                Ops.Check(
                    r.Printer == "Local preview",
                    "Only the Local preview adapter is configured. No physical printer is connected."
                );
                var prior = await Ops.Rows<PrintJob>(db, s)
                    .SingleOrDefaultAsync(x => x.CommandId == r.CommandId);
                if (prior is not null)
                {
                    Ops.Check(
                        prior.PalletId == r.PalletId
                            && prior.LabelTemplateId == r.LabelTemplateId
                            && prior.Copies == r.Copies
                            && prior.Printer == r.Printer
                            && prior.ReprintOfId == null,
                        "commandId already used for another job."
                    );
                    return Results.Ok(prior);
                }
                var pallet =
                    await Ops.Rows<Pallet>(db, s)
                        .AsNoTracking()
                        .SingleOrDefaultAsync(x => x.Id == r.PalletId)
                    ?? throw new OpsError(404, "Pallet not found.");
                var template =
                    await Ops.Rows<LabelTemplate>(db, s)
                        .AsNoTracking()
                        .SingleOrDefaultAsync(x => x.Id == r.LabelTemplateId)
                    ?? throw new OpsError(404, "Label template not found.");
                var rendered = await RenderSnapshot(db, s, template, pallet);
                var row = Ops.Stamp(
                    new PrintJob
                    {
                        RenderedContent = rendered,
                        TemplateRevision = template.Revision,
                        PalletId = r.PalletId,
                        LabelTemplateId = r.LabelTemplateId,
                        Copies = r.Copies,
                        Printer = "Local preview",
                        CommandId = r.CommandId,
                    },
                    s
                );
                db.Add(row);
                Ops.Audit(db, s, "print-job.queued", row.Id);
                await db.SaveChangesAsync();
                await snapshot.CommitAsync();
                return Results.Ok(row);
            }
        );
        g.MapGet(
            "/print-jobs/{id:guid}/preview",
            async (Guid id, Guid plantId, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, plantId);
                var job =
                    await Ops.Rows<PrintJob>(db, s)
                        .AsNoTracking()
                        .SingleOrDefaultAsync(x => x.Id == id)
                    ?? throw new OpsError(404, "Print job not found.");
                return Results.Ok(
                    new
                    {
                        job.Id,
                        adapter = "Local preview",
                        physicalPrint = false,
                        renderedContent = job.RenderedContent,
                        snapshotAvailable = !string.IsNullOrEmpty(job.RenderedContent),
                        templateRevision = job.TemplateRevision,
                        copies = job.Copies,
                    }
                );
            }
        );
        g.MapGet(
            "/print-jobs/{id:guid}/barcode.svg",
            async (Guid id, Guid plantId, FactoryDb db, ClaimsPrincipal p, HttpContext context) =>
            {
                var s = await Ops.Require(db, p, plantId);
                var job =
                    await Ops.Rows<PrintJob>(db, s)
                        .AsNoTracking()
                        .SingleOrDefaultAsync(x => x.Id == id)
                    ?? throw new OpsError(404, "Print job not found.");
                var pallet =
                    await Ops.Rows<Pallet>(db, s)
                        .AsNoTracking()
                        .SingleOrDefaultAsync(x => x.Id == job.PalletId)
                    ?? throw new OpsError(404, "Pallet not found.");
                context.Response.Headers.CacheControl = "no-store";
                ILabelCodeRenderer renderer = new LocalQrLabelCodeRenderer();
                return Results.Text(renderer.Render(pallet.Code), "image/svg+xml");
            }
        );
        g.MapPost(
            "/print-jobs/{id:guid}/reprint",
            async (Guid id, ReprintRequest r, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, r.PlantId, true, true);
                Ops.Command(r.CommandId);
                var reason = Ops.Name(r.Reason);
                var prior = await Ops.Rows<PrintJob>(db, s)
                    .SingleOrDefaultAsync(x => x.CommandId == r.CommandId);
                if (prior is not null)
                {
                    Ops.Check(
                        prior.ReprintOfId == id && prior.Reason == reason,
                        "commandId already used for another reprint."
                    );
                    return Results.Ok(prior);
                }
                var original =
                    await Ops.Rows<PrintJob>(db, s)
                        .AsNoTracking()
                        .SingleOrDefaultAsync(x => x.Id == id)
                    ?? throw new OpsError(404, "Print job not found.");
                var row = Ops.Stamp(
                    new PrintJob
                    {
                        RenderedContent = original.RenderedContent,
                        TemplateRevision = original.TemplateRevision,
                        PalletId = original.PalletId,
                        LabelTemplateId = original.LabelTemplateId,
                        Copies = original.Copies,
                        Printer = "Local preview",
                        ReprintOfId = id,
                        Reason = reason,
                        CommandId = r.CommandId,
                    },
                    s
                );
                db.Add(row);
                Ops.Audit(db, s, "print-job.reprinted", row.Id);
                await db.SaveChangesAsync();
                return Results.Ok(row);
            }
        );
        g.MapGet(
            "/integration-messages",
            async (Guid plantId, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, plantId);
                return Results.Ok(
                    await Ops.Rows<IntegrationMessage>(db, s)
                        .AsNoTracking()
                        .OrderByDescending(x => x.CreatedAt)
                        .Take(200)
                        .ToListAsync()
                );
            }
        );
        g.MapPost(
            "/integration-messages",
            async (MessageRequest r, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, r.PlantId, true, true);
                var external = Ops.Name(r.ExternalId);
                var kind = Ops.Name(r.Kind);
                Ops.Check(
                    r.Payload is not null
                        && System.Text.Encoding.UTF8.GetByteCount(r.Payload) <= 32768,
                    "Payload must be JSON of at most 32 KiB."
                );
                try
                {
                    using var document = JsonDocument.Parse(r.Payload!);
                }
                catch (JsonException)
                {
                    throw new OpsError(400, "Payload must be valid JSON.");
                }
                var prior = await Ops.Rows<IntegrationMessage>(db, s)
                    .SingleOrDefaultAsync(x => x.ExternalId == external);
                if (prior is not null)
                {
                    Ops.Check(
                        prior.Kind == kind && prior.Payload == r.Payload,
                        "External ID already exists with different content."
                    );
                    return Results.Ok(prior);
                }
                var row = Ops.Stamp(
                    new IntegrationMessage
                    {
                        ExternalId = external,
                        Kind = kind,
                        Payload = r.Payload!,
                        CorrelationId = Guid.NewGuid(),
                    },
                    s
                );
                db.Add(row);
                Ops.Audit(db, s, "integration.received", row.Id);
                await db.SaveChangesAsync();
                return Results.Ok(row);
            }
        );
        g.MapPost(
            "/integration-messages/{id:guid}/process",
            async (Guid id, ProcessRequest r, FactoryDb db, ClaimsPrincipal p) =>
            {
                var s = await Ops.Require(db, p, r.PlantId, true, true);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );
                var row =
                    await Ops.Rows<IntegrationMessage>(db, s).SingleOrDefaultAsync(x => x.Id == id)
                    ?? throw new OpsError(404, "Message not found.");
                Ops.Check(
                    row.Status is "Pending" or "Failed",
                    "Only Pending or Failed messages can be processed."
                );
                Ops.Check(row.Attempts < 3, "Message retry limit reached.");
                row.Attempts++;
                try
                {
                    using var document = JsonDocument.Parse(row.Payload);
                    if (document.RootElement.ValueKind != JsonValueKind.Object)
                        throw new JsonException("Local validation requires a JSON object.");
                    row.Status = "Validated";
                    row.Error = "";
                }
                catch (JsonException)
                {
                    row.Status = row.Attempts >= 3 ? "DeadLetter" : "Failed";
                    row.Error = "Local validation requires a valid JSON object.";
                }
                Ops.Audit(db, s, "integration." + row.Status.ToLowerInvariant(), row.Id);
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return Results.Ok(
                    new
                    {
                        message = row,
                        adapter = "Local validation only",
                        deliveredToExternalSystem = false,
                    }
                );
            }
        );
    }

    static async Task<string> RenderSnapshot(
        FactoryDb db,
        OpsScope s,
        LabelTemplate template,
        Pallet pallet
    )
    {
        var batch =
            await Ops.Rows<Batch>(db, s)
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == pallet.BatchId)
            ?? throw new OpsError(404, "Batch not found.");
        var material = await Ops.Rows<Material>(db, s)
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == batch.MaterialId);
        var values = new Dictionary<string, string>
        {
            { "palletCode", pallet.Code },
            { "batchNumber", batch.Number },
            { "materialName", material?.Name ?? "" },
            { "quantity", pallet.Quantity.ToString(CultureInfo.InvariantCulture) },
            { "uom", material?.Uom ?? "" },
            { "location", pallet.Location },
        };
        var rendered = Regex.Replace(
            template.Content,
            @"\{\{(\w+)\}\}",
            m => values.GetValueOrDefault(m.Groups[1].Value, "")
        );
        Ops.Check(rendered.Length <= 32768, "Rendered label exceeds 32768 characters.");
        return rendered;
    }

    static async Task<IResult> Trace(Guid plantId, Guid batchId, FactoryDb db, ClaimsPrincipal p)
    {
        var s = await Ops.Require(db, p, plantId);
        await using var snapshot = await db.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead
        );
        var batch =
            await Ops.Rows<Batch>(db, s).AsNoTracking().SingleOrDefaultAsync(x => x.Id == batchId)
            ?? throw new OpsError(404, "Batch not found.");
        var batches = new HashSet<Guid> { batchId };
        var orders = new HashSet<Guid>();
        bool changed = true;
        int depth = 0;
        // Follow both sides of every manufacturing transformation, bounded explicitly.
        while (changed && depth++ < 20)
        {
            var oldB = batches.Count;
            var oldO = orders.Count;
            var producerIds = await Ops.Rows<Batch>(db, s)
                .Where(x => batches.Contains(x.Id) && x.ProductionOrderId != null)
                .Select(x => x.ProductionOrderId!.Value)
                .Distinct()
                .Take(201)
                .ToListAsync();
            orders.UnionWith(producerIds);
            var consumerIds = await (
                from c in Ops.Rows<Consumption>(db, s)
                join pl in Ops.Rows<Pallet>(db, s) on c.PalletId equals pl.Id
                where batches.Contains(pl.BatchId)
                select c.ProductionOrderId
            )
                .Distinct()
                .Take(201)
                .ToListAsync();
            orders.UnionWith(consumerIds);
            var outputs = await Ops.Rows<Batch>(db, s)
                .Where(x =>
                    x.ProductionOrderId != null && orders.Contains(x.ProductionOrderId.Value)
                )
                .Select(x => x.Id)
                .Take(201)
                .ToListAsync();
            batches.UnionWith(outputs);
            var inputs = await (
                from c in Ops.Rows<Consumption>(db, s)
                join pl in Ops.Rows<Pallet>(db, s) on c.PalletId equals pl.Id
                where orders.Contains(c.ProductionOrderId)
                select pl.BatchId
            )
                .Distinct()
                .Take(201)
                .ToListAsync();
            batches.UnionWith(inputs);
            Ops.Check(
                batches.Count <= 200 && orders.Count <= 200,
                "Trace exceeds the 200-node limit; use a narrower batch investigation."
            );
            changed = oldB != batches.Count || oldO != orders.Count;
        }
        var pallets = await Ops.Rows<Pallet>(db, s)
            .AsNoTracking()
            .Where(x => batches.Contains(x.BatchId))
            .OrderBy(x => x.CreatedAt)
            .Take(200)
            .ToListAsync();
        var palletIds = pallets.Select(x => x.Id).ToArray();
        var shipments = await Ops.Rows<Shipment>(db, s)
            .AsNoTracking()
            .Where(x => palletIds.Contains(x.PalletId))
            .OrderBy(x => x.CreatedAt)
            .Take(200)
            .ToListAsync();
        var deliveryIds = shipments.Select(x => x.DeliveryOrderId).ToArray();
        var deliveries = await Ops.Rows<DeliveryOrder>(db, s)
            .AsNoTracking()
            .Where(x => deliveryIds.Contains(x.Id))
            .Take(200)
            .ToListAsync();
        var customerIds = deliveries.Select(x => x.CustomerId).ToArray();
        var graphPalletIds = Ops.Rows<Pallet>(db, s)
            .Where(x => batches.Contains(x.BatchId))
            .Select(x => x.Id);
        var graphDeliveryIds = Ops.Rows<Shipment>(db, s)
            .Where(x => graphPalletIds.Contains(x.PalletId))
            .Select(x => x.DeliveryOrderId);
        var graphCustomerIds = Ops.Rows<DeliveryOrder>(db, s)
            .Where(x => graphDeliveryIds.Contains(x.Id))
            .Select(x => x.CustomerId);
        var perListTruncated = new Dictionary<string, bool>
        {
            ["pallets"] =
                await Ops.Rows<Pallet>(db, s).CountAsync(x => batches.Contains(x.BatchId)) > 200,
            ["receipts"] =
                await Ops.Rows<GoodsReceipt>(db, s).CountAsync(x => batches.Contains(x.BatchId))
                > 200,
            ["productionOrders"] =
                await Ops.Rows<ProductionOrder>(db, s).CountAsync(x => orders.Contains(x.Id)) > 200,
            ["consumptions"] =
                await Ops.Rows<Consumption>(db, s)
                    .CountAsync(x => orders.Contains(x.ProductionOrderId)) > 200,
            ["outputBatches"] =
                await Ops.Rows<Batch>(db, s)
                    .CountAsync(x => batches.Contains(x.Id) && x.ProductionOrderId != null) > 200,
            ["sourceBatches"] =
                await Ops.Rows<Batch>(db, s)
                    .CountAsync(x => batches.Contains(x.Id) && x.ProductionOrderId == null) > 200,
            ["shipments"] =
                await Ops.Rows<Shipment>(db, s).CountAsync(x => graphPalletIds.Contains(x.PalletId))
                > 200,
            ["deliveries"] =
                await Ops.Rows<DeliveryOrder>(db, s)
                    .CountAsync(x => graphDeliveryIds.Contains(x.Id)) > 200,
            ["customers"] =
                await Ops.Rows<Partner>(db, s).CountAsync(x => graphCustomerIds.Contains(x.Id))
                > 200,
            ["inspections"] =
                await Ops.Rows<QualityInspection>(db, s)
                    .CountAsync(x => batches.Contains(x.BatchId)) > 200,
            ["movements"] =
                await Ops.Rows<StockMovement>(db, s)
                    .CountAsync(x => graphPalletIds.Contains(x.PalletId)) > 200,
        };
        // Propagate incompleteness where the response derives children from a bounded parent list.
        perListTruncated["shipments"] |= perListTruncated["pallets"];
        perListTruncated["movements"] |= perListTruncated["pallets"];
        perListTruncated["deliveries"] |= perListTruncated["shipments"];
        perListTruncated["customers"] |= perListTruncated["deliveries"];

        return Results.Ok(
            new
            {
                batch,
                material = await Ops.Rows<Material>(db, s)
                    .AsNoTracking()
                    .SingleOrDefaultAsync(x => x.Id == batch.MaterialId),
                pallets,
                receipts = await Ops.Rows<GoodsReceipt>(db, s)
                    .AsNoTracking()
                    .Where(x => batches.Contains(x.BatchId))
                    .OrderBy(x => x.CreatedAt)
                    .Take(200)
                    .ToListAsync(),
                productionOrder = batch.ProductionOrderId is Guid oid
                    ? await Ops.Rows<ProductionOrder>(db, s)
                        .AsNoTracking()
                        .SingleOrDefaultAsync(x => x.Id == oid)
                    : null,
                productionOrders = await Ops.Rows<ProductionOrder>(db, s)
                    .AsNoTracking()
                    .Where(x => orders.Contains(x.Id))
                    .Take(200)
                    .ToListAsync(),
                consumptions = await Ops.Rows<Consumption>(db, s)
                    .AsNoTracking()
                    .Where(x => orders.Contains(x.ProductionOrderId))
                    .OrderBy(x => x.CreatedAt)
                    .Take(200)
                    .ToListAsync(),
                outputBatches = await Ops.Rows<Batch>(db, s)
                    .AsNoTracking()
                    .Where(x => batches.Contains(x.Id) && x.ProductionOrderId != null)
                    .Take(200)
                    .ToListAsync(),
                sourceBatches = await Ops.Rows<Batch>(db, s)
                    .AsNoTracking()
                    .Where(x => batches.Contains(x.Id) && x.ProductionOrderId == null)
                    .Take(200)
                    .ToListAsync(),
                shipments,
                deliveries,
                customers = await Ops.Rows<Partner>(db, s)
                    .AsNoTracking()
                    .Where(x => customerIds.Contains(x.Id))
                    .Take(200)
                    .ToListAsync(),
                inspections = await Ops.Rows<QualityInspection>(db, s)
                    .AsNoTracking()
                    .Where(x => batches.Contains(x.BatchId))
                    .OrderBy(x => x.CreatedAt)
                    .Take(200)
                    .ToListAsync(),
                movements = await Ops.Rows<StockMovement>(db, s)
                    .AsNoTracking()
                    .Where(x => palletIds.Contains(x.PalletId))
                    .OrderBy(x => x.CreatedAt)
                    .Take(200)
                    .ToListAsync(),
                listLimit = 200,
                graphDepthLimit = 20,
                graphDepthLimitReached = changed,
                perListTruncated,
                collectionsTruncated = perListTruncated.Values.Any(x => x),
                isComplete = !changed && !perListTruncated.Values.Any(x => x),
            }
        );
    }

    static async Task<IResult> Reports(Guid plantId, FactoryDb db, ClaimsPrincipal p)
    {
        var s = await Ops.Require(db, p, plantId);
        await using var snapshot = await db.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead
        );
        var now = DateTimeOffset.UtcNow;
        var inventory = await (
            from pl in Ops.Rows<Pallet>(db, s)
            join b in Ops.Rows<Batch>(db, s) on pl.BatchId equals b.Id
            join m in Ops.Rows<Material>(db, s) on b.MaterialId equals m.Id
            select new
            {
                pl.Quantity,
                pl.ReservedQuantity,
                b.QualityStatus,
                b.ExpiryDate,
                m.Uom,
            }
        ).ToListAsync();
        var receipts = await (
            from r in Ops.Rows<GoodsReceipt>(db, s)
            join b in Ops.Rows<Batch>(db, s) on r.BatchId equals b.Id
            join m in Ops.Rows<Material>(db, s) on b.MaterialId equals m.Id
            select new { r.Quantity, m.Uom }
        ).ToListAsync();
        var consumed = await (
            from c in Ops.Rows<Consumption>(db, s)
            join pl in Ops.Rows<Pallet>(db, s) on c.PalletId equals pl.Id
            join b in Ops.Rows<Batch>(db, s) on pl.BatchId equals b.Id
            join m in Ops.Rows<Material>(db, s) on b.MaterialId equals m.Id
            select new { c.Quantity, m.Uom }
        ).ToListAsync();
        var produced = await (
            from c in Ops.Rows<ProductionConfirmation>(db, s)
            join b in Ops.Rows<Batch>(db, s) on c.BatchId equals b.Id
            join m in Ops.Rows<Material>(db, s) on b.MaterialId equals m.Id
            select new
            {
                c.Quantity,
                c.ScrapQuantity,
                m.Uom,
            }
        ).ToListAsync();
        var shipped = await (
            from c in Ops.Rows<Shipment>(db, s)
            join pl in Ops.Rows<Pallet>(db, s) on c.PalletId equals pl.Id
            join b in Ops.Rows<Batch>(db, s) on pl.BatchId equals b.Id
            join m in Ops.Rows<Material>(db, s) on b.MaterialId equals m.Id
            select new { c.Quantity, m.Uom }
        ).ToListAsync();
        var units = inventory
            .Select(x => x.Uom)
            .Concat(receipts.Select(x => x.Uom))
            .Concat(consumed.Select(x => x.Uom))
            .Concat(produced.Select(x => x.Uom))
            .Concat(shipped.Select(x => x.Uom))
            .Distinct()
            .OrderBy(x => x)
            .ToList();
        var byUom = units
            .Select(u => new
            {
                uom = u,
                receipts = receipts.Where(x => x.Uom == u).Sum(x => x.Quantity),
                inventory = inventory.Where(x => x.Uom == u).Sum(x => x.Quantity),
                released = inventory
                    .Where(x =>
                        x.Uom == u
                        && x.QualityStatus == "Released"
                        && (x.ExpiryDate == null || x.ExpiryDate > now)
                    )
                    .Sum(x => x.Quantity),
                hold = inventory
                    .Where(x => x.Uom == u && x.QualityStatus == "Hold")
                    .Sum(x => x.Quantity),
                rejected = inventory
                    .Where(x => x.Uom == u && x.QualityStatus == "Rejected")
                    .Sum(x => x.Quantity),
                expired = inventory
                    .Where(x => x.Uom == u && x.ExpiryDate <= now)
                    .Sum(x => x.Quantity),
                reserved = inventory.Where(x => x.Uom == u).Sum(x => x.ReservedQuantity),
                consumed = consumed.Where(x => x.Uom == u).Sum(x => x.Quantity),
                produced = produced.Where(x => x.Uom == u).Sum(x => x.Quantity),
                scrap = produced.Where(x => x.Uom == u).Sum(x => x.ScrapQuantity),
                shipped = shipped.Where(x => x.Uom == u).Sum(x => x.Quantity),
                yieldPercentage = produced
                    .Where(x => x.Uom == u)
                    .Sum(x => x.Quantity + x.ScrapQuantity) > 0
                    ? (decimal?)(
                        100m
                        * produced.Where(x => x.Uom == u).Sum(x => x.Quantity)
                        / produced.Where(x => x.Uom == u).Sum(x => x.Quantity + x.ScrapQuantity)
                    )
                    : null,
            })
            .ToArray();
        return Results.Ok(
            new
            {
                asOf = now,
                byUom,
                quantityNote = "Quantities are grouped by unit; no conversion or mixed-unit total. Expired quantity may overlap quality categories. Yield is good output / (good output + scrap).",
                openOrders = await Ops.Rows<ProductionOrder>(db, s)
                    .CountAsync(x => x.Status != "Completed" && x.Status != "Cancelled"),
                completedOrders = await Ops.Rows<ProductionOrder>(db, s)
                    .CountAsync(x => x.Status == "Completed"),
                qualityHoldCount = await Ops.Rows<Batch>(db, s)
                    .CountAsync(x => x.QualityStatus == "Hold"),
                qualityRejectedCount = await Ops.Rows<Batch>(db, s)
                    .CountAsync(x => x.QualityStatus == "Rejected"),
                downtimeMinutes = await Ops.Rows<DowntimeEvent>(db, s)
                    .SumAsync(x => (long)x.Minutes),
                oee = (decimal?)null,
                oeeNote = "OEE requires scheduled time, ideal cycle time and machine integration; it is not calculated.",
            }
        );
    }
}
