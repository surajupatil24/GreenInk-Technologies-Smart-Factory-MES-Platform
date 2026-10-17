using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace GreenInk;

public static partial class DispatchEndpoints
{
    public record AreaInput(
        Guid PlantId,
        string Code,
        string Name,
        string ShippingPoint,
        Guid PackBinId,
        Guid LoadingBinId,
        string PickMode,
        string PackMode,
        bool AllowOverpick,
        Guid CommandId
    );

    public record InstructionInput(
        Guid PlantId,
        Guid CustomerId,
        string Picking,
        string Packing,
        string Shipping,
        string Safety,
        int MinimumShelfDays,
        bool ProductPacking,
        Guid CommandId,
        uint Version = 0
    );

    public record PackageInput(
        Guid PlantId,
        string Code,
        string Name,
        decimal LengthCm,
        decimal WidthCm,
        decimal HeightCm,
        decimal TareKg,
        decimal MaximumGrossKg,
        bool TemperatureDeviceRequired,
        Guid CommandId
    );

    public record WarehouseInput(
        Guid PlantId,
        string Code,
        string Name,
        string ReceivingPlant,
        string StorageLocation,
        string Gate,
        Guid CommandId
    );

    public record ShuttleLine(Guid MaterialId, decimal Quantity);

    public record PlanInput(
        Guid PlantId,
        string Number,
        Guid DispatchAreaId,
        string Kind,
        Guid? DispatchWarehouseId,
        Guid AssignedToId,
        DateTimeOffset ScheduledAt,
        bool Priority,
        string Route,
        Guid[] DeliveryOrderIds,
        ShuttleLine[] ShuttleLines,
        Guid CommandId
    );

    public record ReassignInput(
        Guid PlantId,
        Guid AssignedToId,
        DateTimeOffset ScheduledAt,
        bool Priority,
        string Reason,
        uint Version,
        Guid CommandId
    );

    public record StateInput(
        Guid PlantId,
        string Action,
        bool Confirmed,
        string Notes,
        Guid CommandId
    );

    public record CommandInput(Guid PlantId, Guid CommandId);

    public record PickLine(
        Guid DispatchRunLineId,
        Guid PalletId,
        string PalletCode,
        string SourceBin,
        decimal Quantity
    );

    public record PickInput(
        Guid PlantId,
        PickLine[] Lines,
        bool InstructionsConfirmed,
        bool SafetyConfirmed,
        Guid CommandId
    );

    public record ReturnInput(
        Guid PlantId,
        decimal Quantity,
        string PalletCode,
        string DestinationBin,
        string Reason,
        Guid CommandId
    );

    public record UnitInput(
        Guid PlantId,
        Guid DispatchPackageId,
        string TemperatureDevice,
        Guid CommandId
    );

    public record PackInput(
        Guid PlantId,
        Guid[] PickIds,
        string[] PalletCodes,
        bool InstructionsConfirmed,
        Guid CommandId
    );

    public record UnitStateInput(
        Guid PlantId,
        string Action,
        string UnitCode,
        decimal? GrossKg,
        string Notes,
        Guid CommandId
    );

    public record TruckInput(
        Guid PlantId,
        string Truck,
        string Carrier,
        bool InstructionsConfirmed,
        Guid CommandId
    );

    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Json);

    static T Parse<T>(string value) => JsonSerializer.Deserialize<T>(value, Json)!;

    static string Text(string? value, int max = 2000, bool required = false)
    {
        Ops.Check(
            value != null && value.Trim().Length <= max && (!required || value.Trim().Length > 0),
            $"Supply {(required ? "nonempty " : "")}text up to {max} characters."
        );
        return value!.Trim();
    }

    static bool Manager(OpsScope s) => s.Role is "PlatformAdmin" or "TenantAdmin" or "PlantManager";

    static async Task<T> Get<T>(FactoryDb db, OpsScope s, Guid id)
        where T : class, IPlantRow =>
        await Ops.Rows<T>(db, s).SingleOrDefaultAsync(x => x.Id == id)
        ?? throw new OpsError(404, "Record not found in this plant.");

    static async Task<DispatchRun> Run(FactoryDb db, OpsScope s, Guid id)
    {
        var row = await Get<DispatchRun>(db, s, id);
        if (s.Role == "Operator" && row.AssignedToId != s.ActorId)
            throw new OpsError(403, "This dispatch is assigned to another operator.");
        return row;
    }

    static async Task<IResult> Command<T>(
        FactoryDb db,
        HttpContext h,
        Guid plantId,
        Guid command,
        string kind,
        Guid? target,
        T input,
        bool manager,
        Func<OpsScope, Task<Guid>> action
    )
    {
        var s = await Ops.Require(db, h.User, plantId, true, manager);
        Ops.Command(command);
        var signature = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    Serialize(
                        new
                        {
                            kind,
                            target,
                            input,
                        }
                    )
                )
            )
        );
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (s.Role == "Operator" && target.HasValue)
        {
            var runId = target.Value;
            if (kind is "Pack" or "HandlingUnitState")
                runId = (await Get<DispatchHandlingUnit>(db, s, target.Value)).DispatchRunId;
            await Run(db, s, runId);
            if (input is UnitStateInput state)
                Ops.Check(
                    state.Action is not ("Unload" or "Unpack"),
                    "Only a manager can reverse packing or loading."
                );
        }
        var old = await Ops.Rows<DispatchCommand>(db, s)
            .SingleOrDefaultAsync(x => x.CommandId == command);
        if (old != null)
        {
            Ops.Check(old.Signature == signature, "Command ID was used with different input.");
            return Results.Ok(new { old.ResultId });
        }
        var result = await action(s);
        db.Add(
            Ops.Stamp(
                new DispatchCommand
                {
                    CommandId = command,
                    Signature = signature,
                    Kind = kind,
                    ResultId = result,
                },
                s
            )
        );
        Ops.Audit(db, s, "Dispatch." + kind, result);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return Results.Ok(new { ResultId = result });
    }

    static void Event(FactoryDb db, OpsScope s, Guid run, string kind, string notes = "") =>
        db.Add(
            Ops.Stamp(
                new DispatchEvent
                {
                    DispatchRunId = run,
                    ActorId = s.ActorId,
                    Kind = kind,
                    Notes = notes,
                },
                s
            )
        );

    static void Read<T>(RouteGroupBuilder g, string path)
        where T : class, IPlantRow =>
        g.MapGet(
            path,
            async (Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                var rows = await Ops.Rows<T>(db, s)
                    .AsNoTracking()
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenBy(x => x.Id)
                    .Take(201)
                    .ToListAsync();
                return Results.Ok(new { items = rows.Take(200), hasMore = rows.Count > 200 });
            }
        );

    static async Task User(FactoryDb db, OpsScope s, Guid id) =>
        Ops.Check(
            await db.Users.AnyAsync(x =>
                x.Id == id
                && (
                    x.Role == "PlatformAdmin"
                    || x.TenantId == s.TenantId
                        && (
                            x.Role == "TenantAdmin"
                            || (x.Role == "Operator" || x.Role == "PlantManager")
                                && x.Plants.Any(p => p.PlantId == s.PlantId)
                        )
                )
            ),
            "Choose an active authorized operator or manager for this plant."
        );

    public static void MapDispatch(this WebApplication app)
    {
        var g = app.MapGroup("/api/operations/dispatch").RequireAuthorization();
        Read<DispatchArea>(g, "/areas");
        Read<DispatchInstruction>(g, "/instructions");
        Read<DispatchPackage>(g, "/packages");
        Read<DispatchWarehouse>(g, "/warehouses");
        g.MapGet(
            "/operators",
            async (Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                return Results.Ok(
                    await db
                        .Users.Where(x =>
                            x.Role == "PlatformAdmin"
                            || x.TenantId == s.TenantId
                                && (
                                    x.Role == "TenantAdmin"
                                    || (x.Role == "Operator" || x.Role == "PlantManager")
                                        && x.Plants.Any(p => p.PlantId == plantId)
                                )
                        )
                        .OrderBy(x => x.DisplayName)
                        .Select(x => new
                        {
                            x.Id,
                            name = x.DisplayName,
                            x.Role,
                        })
                        .Take(200)
                        .ToListAsync()
                );
            }
        );
        g.MapPost(
            "/areas",
            (AreaInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "Area",
                    null,
                    r,
                    true,
                    async s =>
                    {
                        await Get<WarehouseBin>(db, s, r.PackBinId);
                        await Get<WarehouseBin>(db, s, r.LoadingBinId);
                        Ops.Check(
                            r.PackBinId != r.LoadingBinId,
                            "Packing and loading bins must differ."
                        );
                        Ops.Check(
                            r.PickMode is "Single" or "Group"
                                && r.PackMode is "Product" or "Collective",
                            "Choose valid picking and packing modes."
                        );
                        var code = Ops.Code(r.Code);
                        Ops.Check(
                            !await Ops.Rows<DispatchArea>(db, s).AnyAsync(x => x.Code == code),
                            "Area code exists. Use a new code for revised policies."
                        );
                        var row = Ops.Stamp(
                            new DispatchArea
                            {
                                Code = code,
                                Name = Ops.Name(r.Name),
                                ShippingPoint = Ops.Code(r.ShippingPoint),
                                PackBinId = r.PackBinId,
                                LoadingBinId = r.LoadingBinId,
                                PickMode = r.PickMode,
                                PackMode = r.PackMode,
                                AllowOverpick = r.AllowOverpick,
                            },
                            s
                        );
                        db.Add(row);
                        return row.Id;
                    }
                )
        );
        g.MapPost(
            "/instructions",
            (InstructionInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "CustomerInstructions",
                    null,
                    r,
                    true,
                    async s =>
                    {
                        Ops.Check(
                            (await Get<Partner>(db, s, r.CustomerId)).Kind == "Customer",
                            "Choose a customer."
                        );
                        Ops.Check(
                            r.MinimumShelfDays >= 0 && r.MinimumShelfDays <= 3650,
                            "Minimum shelf life must be 0–3650 days."
                        );
                        var row = await Ops.Rows<DispatchInstruction>(db, s)
                            .SingleOrDefaultAsync(x => x.CustomerId == r.CustomerId);
                        if (row == null)
                        {
                            row = Ops.Stamp(
                                new DispatchInstruction { CustomerId = r.CustomerId },
                                s
                            );
                            db.Add(row);
                        }
                        else
                            Ops.Check(
                                row.Version == r.Version,
                                "Instructions changed. Refresh before saving."
                            );
                        row.Picking = Text(r.Picking);
                        row.Packing = Text(r.Packing);
                        row.Shipping = Text(r.Shipping);
                        row.Safety = Text(r.Safety);
                        row.MinimumShelfDays = r.MinimumShelfDays;
                        row.ProductPacking = r.ProductPacking;
                        return row.Id;
                    }
                )
        );
        g.MapPost(
            "/packages",
            (PackageInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "PackageType",
                    null,
                    r,
                    true,
                    async s =>
                    {
                        Ops.Check(
                            Ops.Positive(r.LengthCm)
                                && Ops.Positive(r.WidthCm)
                                && Ops.Positive(r.HeightCm)
                                && r.TareKg >= 0
                                && decimal.Round(r.TareKg, 6) == r.TareKg
                                && Ops.Positive(r.MaximumGrossKg)
                                && r.MaximumGrossKg > r.TareKg,
                            "Supply positive dimensions and maximum gross weight greater than nonnegative tare."
                        );
                        var code = Ops.Code(r.Code);
                        Ops.Check(
                            !await Ops.Rows<DispatchPackage>(db, s).AnyAsync(x => x.Code == code),
                            "Package code exists. Use a new code for revised dimensions."
                        );
                        var row = Ops.Stamp(
                            new DispatchPackage
                            {
                                Code = code,
                                Name = Ops.Name(r.Name),
                                LengthCm = r.LengthCm,
                                WidthCm = r.WidthCm,
                                HeightCm = r.HeightCm,
                                TareKg = r.TareKg,
                                MaximumGrossKg = r.MaximumGrossKg,
                                TemperatureDeviceRequired = r.TemperatureDeviceRequired,
                            },
                            s
                        );
                        db.Add(row);
                        return row.Id;
                    }
                )
        );
        g.MapPost(
            "/warehouses",
            (WarehouseInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "ExternalWarehouse",
                    null,
                    r,
                    true,
                    s =>
                    {
                        var row = Ops.Stamp(
                            new DispatchWarehouse
                            {
                                Code = Ops.Code(r.Code),
                                Name = Ops.Name(r.Name),
                                ReceivingPlant = Ops.Code(r.ReceivingPlant),
                                StorageLocation = Ops.Code(r.StorageLocation),
                                Gate = Ops.Code(r.Gate),
                            },
                            s
                        );
                        db.Add(row);
                        return Task.FromResult(row.Id);
                    }
                )
        );
        g.MapGet(
            "/runs",
            async (Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                var q = Ops.Rows<DispatchRun>(db, s);
                if (s.Role == "Operator")
                    q = q.Where(x => x.AssignedToId == s.ActorId);
                var rows = await q.AsNoTracking()
                    .OrderByDescending(x => x.Priority)
                    .ThenBy(x => x.ScheduledAt)
                    .ThenBy(x => x.Id)
                    .Take(201)
                    .ToListAsync();
                return Results.Ok(new { items = rows.Take(200), hasMore = rows.Count > 200 });
            }
        );
        g.MapGet(
            "/available-deliveries",
            async (Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                var rows = await Ops.Rows<DeliveryOrder>(db, s)
                    .Where(o =>
                        o.Status == "Open"
                        && !Ops.Rows<DispatchRunLine>(db, s)
                            .Any(l => l.Active && l.DeliveryOrderId == o.Id)
                    )
                    .OrderBy(x => x.CreatedAt)
                    .Take(201)
                    .ToListAsync();
                return Results.Ok(new { items = rows.Take(200), hasMore = rows.Count > 200 });
            }
        );
        g.MapPost(
            "/runs",
            (PlanInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "Plan",
                    null,
                    r,
                    true,
                    async s =>
                    {
                        var area = await Get<DispatchArea>(db, s, r.DispatchAreaId);
                        await User(db, s, r.AssignedToId);
                        Ops.Check(
                            r.ScheduledAt.Year >= 2000 && r.ScheduledAt.Year <= 2200,
                            "Supply a valid dispatch date."
                        );
                        Ops.Check(
                            r.Kind is "Customer" or "Shuttle",
                            "Choose Customer or Shuttle dispatch."
                        );
                        Ops.Check(
                            r.DeliveryOrderIds != null && r.ShuttleLines != null,
                            "Supply delivery and shuttle line arrays."
                        );
                        if (r.Kind == "Customer")
                            Ops.Check(
                                r.DispatchWarehouseId == null
                                    && r.ShuttleLines!.Length == 0
                                    && r.DeliveryOrderIds is { Length: > 0 and <= 50 }
                                    && r.DeliveryOrderIds.Distinct().Count()
                                        == r.DeliveryOrderIds.Length,
                                "Customer dispatch requires 1–50 distinct delivery orders."
                            );
                        else
                        {
                            Ops.Check(
                                r.DispatchWarehouseId.HasValue
                                    && r.DeliveryOrderIds!.Length == 0
                                    && r.ShuttleLines is { Length: > 0 and <= 50 }
                                    && r.ShuttleLines.All(x => x != null)
                                    && r.ShuttleLines.Select(x => x.MaterialId).Distinct().Count()
                                        == r.ShuttleLines.Length,
                                "Shuttle requires a destination and 1–50 distinct material lines."
                            );
                            await Get<DispatchWarehouse>(db, s, r.DispatchWarehouseId!.Value);
                        }
                        var number = Ops.Code(r.Number);
                        Ops.Check(
                            !await Ops.Rows<DispatchRun>(db, s).AnyAsync(x => x.Number == number),
                            "Dispatch number already exists."
                        );
                        var run = Ops.Stamp(
                            new DispatchRun
                            {
                                Number = number,
                                Kind = r.Kind,
                                DispatchAreaId = area.Id,
                                DispatchWarehouseId = r.DispatchWarehouseId,
                                AssignedToId = r.AssignedToId,
                                ScheduledAt = r.ScheduledAt,
                                Priority = r.Priority,
                                Route = Text(r.Route, 160),
                                SettingsJson = Serialize(area),
                            },
                            s
                        );
                        db.Add(run);
                        if (r.Kind == "Customer")
                            foreach (var id in r.DeliveryOrderIds!)
                            {
                                var order = await Get<DeliveryOrder>(db, s, id);
                                Ops.Check(
                                    order.Status == "Open",
                                    "Only open delivery orders can be planned."
                                );
                                Ops.Check(
                                    !await Ops.Rows<DispatchRunLine>(db, s)
                                        .AnyAsync(x => x.DeliveryOrderId == id && x.Active),
                                    "Delivery order is already assigned to a dispatch."
                                );
                                var instruction = await Ops.Rows<DispatchInstruction>(db, s)
                                    .SingleOrDefaultAsync(x => x.CustomerId == order.CustomerId);
                                db.Add(
                                    Ops.Stamp(
                                        new DispatchRunLine
                                        {
                                            DispatchRunId = run.Id,
                                            DeliveryOrderId = id,
                                            CustomerId = order.CustomerId,
                                            MaterialId = order.MaterialId,
                                            Quantity = order.Quantity,
                                            InstructionsJson = Serialize(
                                                instruction ?? new DispatchInstruction()
                                            ),
                                        },
                                        s
                                    )
                                );
                            }
                        else
                            foreach (var line in r.ShuttleLines!)
                            {
                                Ops.Check(
                                    Ops.Positive(line.Quantity),
                                    "Shuttle quantities must be positive."
                                );
                                Ops.Check(
                                    (await Get<Material>(db, s, line.MaterialId)).Kind
                                        == "Finished",
                                    "Shuttle materials must be finished goods."
                                );
                                db.Add(
                                    Ops.Stamp(
                                        new DispatchRunLine
                                        {
                                            DispatchRunId = run.Id,
                                            MaterialId = line.MaterialId,
                                            Quantity = line.Quantity,
                                            InstructionsJson = Serialize(new DispatchInstruction()),
                                        },
                                        s
                                    )
                                );
                            }
                        Event(
                            db,
                            s,
                            run.Id,
                            "Planned",
                            $"{r.Kind} dispatch. Local records only; ERP adapter is not connected."
                        );
                        return run.Id;
                    }
                )
        );
        g.MapPost(
            "/runs/{id:guid}/assignment",
            (Guid id, ReassignInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "Assignment",
                    id,
                    r,
                    true,
                    async s =>
                    {
                        var run = await Run(db, s, id);
                        Ops.Check(!Terminal(run), "Completed dispatch cannot be reassigned.");
                        Ops.Check(
                            run.Version == r.Version,
                            "Dispatch changed. Refresh before reassignment."
                        );
                        await User(db, s, r.AssignedToId);
                        Ops.Check(
                            r.ScheduledAt.Year >= 2000 && r.ScheduledAt.Year <= 2200,
                            "Supply a valid dispatch date."
                        );
                        Event(
                            db,
                            s,
                            id,
                            "Reassigned",
                            Serialize(
                                new
                                {
                                    oldOperator = run.AssignedToId,
                                    newOperator = r.AssignedToId,
                                    oldDate = run.ScheduledAt,
                                    newDate = r.ScheduledAt,
                                    reason = Text(r.Reason, 2000, true),
                                }
                            )
                        );
                        run.AssignedToId = r.AssignedToId;
                        run.ScheduledAt = r.ScheduledAt;
                        run.Priority = r.Priority;
                        return id;
                    }
                )
        );
        g.MapGet(
            "/runs/{id:guid}",
            async (Guid id, Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.RepeatableRead
                );
                var run = await Run(db, s, id);
                var lines = await Ops.Rows<DispatchRunLine>(db, s)
                    .Where(x => x.DispatchRunId == id)
                    .OrderBy(x => x.CreatedAt)
                    .ToListAsync();
                var picks = await Ops.Rows<DispatchPick>(db, s)
                    .Where(x => x.DispatchRunId == id)
                    .OrderBy(x => x.CreatedAt)
                    .ToListAsync();
                var units = await Ops.Rows<DispatchHandlingUnit>(db, s)
                    .Where(x => x.DispatchRunId == id)
                    .OrderBy(x => x.CreatedAt)
                    .ToListAsync();
                var events = await Ops.Rows<DispatchEvent>(db, s)
                    .Where(x => x.DispatchRunId == id)
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(201)
                    .ToListAsync();
                var palletIds = picks
                    .SelectMany(x => new[] { x.SourcePalletId, x.PickedPalletId })
                    .ToArray();
                var pallets = await Ops.Rows<Pallet>(db, s)
                    .Where(x => palletIds.Contains(x.Id))
                    .ToListAsync();
                var batchIds = pallets.Select(x => x.BatchId).ToArray();
                var batches = await Ops.Rows<Batch>(db, s)
                    .Where(x => batchIds.Contains(x.Id))
                    .ToListAsync();
                var materialIds = lines.Select(x => x.MaterialId).ToArray();
                var materials = await Ops.Rows<Material>(db, s)
                    .Where(x => materialIds.Contains(x.Id))
                    .ToListAsync();
                var customerIds = lines.Select(x => x.CustomerId).ToArray();
                var customers = await Ops.Rows<Partner>(db, s)
                    .Where(x => customerIds.Contains(x.Id))
                    .ToListAsync();
                var deliveryIds = lines.Select(x => x.DeliveryOrderId).ToArray();
                var deliveries = await Ops.Rows<DeliveryOrder>(db, s)
                    .Where(x => deliveryIds.Contains(x.Id))
                    .ToListAsync();
                var actorIds = events
                    .Select(x => x.ActorId)
                    .Append(run.AssignedToId)
                    .Distinct()
                    .ToArray();
                var actors = await db
                    .Users.Where(x => actorIds.Contains(x.Id))
                    .Select(x => new { x.Id, name = x.DisplayName })
                    .ToListAsync();
                await tx.CommitAsync();
                return Results.Ok(
                    new
                    {
                        run,
                        area = Parse<DispatchArea>(run.SettingsJson),
                        lines,
                        picks,
                        units,
                        pallets,
                        batches,
                        materials,
                        customers,
                        deliveries,
                        actors,
                        events = events.Take(200),
                        eventsHasMore = events.Count > 200,
                        erpStatus = "NotConnected",
                        destination = run.DispatchWarehouseId.HasValue
                            ? await Get<DispatchWarehouse>(db, s, run.DispatchWarehouseId.Value)
                            : null,
                    }
                );
            }
        );
        g.MapGet(
            "/runs/{id:guid}/stock",
            async (Guid id, Guid lineId, Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                await Run(db, s, id);
                var line = await Get<DispatchRunLine>(db, s, lineId);
                Ops.Check(line.DispatchRunId == id, "Line does not belong to this dispatch.");
                var shelfDays = Parse<DispatchInstruction>(line.InstructionsJson).MinimumShelfDays;
                var minDate = DateTimeOffset.UtcNow.AddDays(shelfDays);
                var rows = await (
                    from p in Ops.Rows<Pallet>(db, s)
                    join b in Ops.Rows<Batch>(db, s) on p.BatchId equals b.Id
                    where
                        b.MaterialId == line.MaterialId
                        && b.Kind == "Finished"
                        && b.QualityStatus == "Released"
                        && ((shelfDays == 0 && b.ExpiryDate == null) || b.ExpiryDate > minDate)
                        && p.Quantity > p.ReservedQuantity
                    orderby b.ExpiryDate == null, b.ExpiryDate, p.CreatedAt
                    select new { pallet = p, batch = b }
                )
                    .Take(201)
                    .ToListAsync();
                return Results.Ok(new { items = rows.Take(200), hasMore = rows.Count > 200 });
            }
        );
        g.MapPost(
            "/runs/{id:guid}/state",
            (Guid id, StateInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "State",
                    id,
                    r,
                    false,
                    async s =>
                    {
                        var run = await Run(db, s, id);
                        Ops.Check(r.Confirmed, "Confirm the action and applicable instructions.");
                        if (r.Action == "StartPicking")
                        {
                            Ops.Check(
                                run.Status == "Planned",
                                "Only planned dispatch can start picking."
                            );
                            run.Status = "Picking";
                        }
                        else if (r.Action == "CompletePicking")
                        {
                            Ops.Check(run.Status == "Picking", "Dispatch is not picking.");
                            await Balanced(db, s, run);
                            run.Status = "Picked";
                        }
                        else if (r.Action == "StartPacking")
                        {
                            Ops.Check(run.Status == "Picked", "Complete picking before packing.");
                            run.Status = "Packing";
                        }
                        else if (r.Action == "CompletePacking")
                        {
                            Ops.Check(run.Status == "Packing", "Start packing first.");
                            await Balanced(db, s, run);
                            var picks = await Ops.Rows<DispatchPick>(db, s)
                                .Where(x => x.DispatchRunId == id && x.Quantity > 0)
                                .ToListAsync();
                            Ops.Check(
                                picks.All(x =>
                                    x.DispatchHandlingUnitId.HasValue && x.Status == "Packed"
                                ),
                                "Pack every active picked allocation."
                            );
                            Ops.Check(
                                !await Ops.Rows<DispatchHandlingUnit>(db, s)
                                    .AnyAsync(x => x.DispatchRunId == id && x.Status == "Open"),
                                "Close or cancel every open handling unit."
                            );
                            run.Status = "Packed";
                        }
                        else if (r.Action == "Cancel")
                        {
                            Ops.Check(Manager(s), "Only a manager can cancel dispatch.");
                            Ops.Check(
                                !Terminal(run),
                                "Dispatch cannot be cancelled at this stage."
                            );
                            Ops.Check(
                                !await Ops.Rows<DispatchPick>(db, s)
                                    .AnyAsync(x => x.DispatchRunId == id && x.Quantity > 0),
                                "Unpack and return all picked stock before cancellation."
                            );
                            foreach (
                                var line in await Ops.Rows<DispatchRunLine>(db, s)
                                    .Where(x => x.DispatchRunId == id)
                                    .ToListAsync()
                            )
                                line.Active = false;
                            run.Status = "Cancelled";
                            Text(r.Notes, 2000, true);
                        }
                        else
                            throw new OpsError(400, "Choose a valid dispatch action.");
                        Event(db, s, id, r.Action, Text(r.Notes));
                        return id;
                    }
                )
        );
        MapPicking(g);
        MapPacking(g);
        MapGoodsIssue(g);
    }

    static bool Terminal(DispatchRun run) => run.Status is "Issued" or "Cancelled" or "Reversed";

    static async Task Balanced(FactoryDb db, OpsScope s, DispatchRun run)
    {
        var lines = await Ops.Rows<DispatchRunLine>(db, s)
            .Where(x => x.DispatchRunId == run.Id)
            .ToListAsync();
        foreach (var line in lines)
        {
            var quantity = await Ops.Rows<DispatchPick>(db, s)
                .Where(x => x.DispatchRunLineId == line.Id)
                .SumAsync(x => x.Quantity);
            Ops.Check(
                quantity == line.Quantity,
                $"Picked quantity {quantity:0.######} must equal required {line.Quantity:0.######}. Return any excess before completing picking."
            );
        }
    }

    static async Task Usable(FactoryDb db, OpsScope s, DispatchRunLine line, Batch batch)
    {
        Ops.Check(
            batch.Kind == "Finished"
                && batch.MaterialId == line.MaterialId
                && batch.QualityStatus == "Released",
            "Choose matching, quality-released finished stock."
        );
        var shelf = Parse<DispatchInstruction>(line.InstructionsJson).MinimumShelfDays;
        Ops.Check(
            (shelf == 0 && batch.ExpiryDate == null)
                || batch.ExpiryDate > DateTimeOffset.UtcNow.AddDays(shelf),
            "Stock is expired or does not meet customer minimum shelf life."
        );
        await ManufacturingEndpoints.ReleaseGuard(db, s, batch.Id);
    }

    static StockMovement Move(
        OpsScope s,
        Pallet pallet,
        decimal quantity,
        string kind,
        Guid run,
        string from,
        string to
    ) =>
        Ops.Stamp(
            new StockMovement
            {
                PalletId = pallet.Id,
                Quantity = quantity,
                Kind = kind,
                Reference = run.ToString(),
                FromLocation = from,
                ToLocation = to,
                CommandId = Guid.NewGuid(),
            },
            s
        );
}
