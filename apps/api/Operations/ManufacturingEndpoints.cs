using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace GreenInk;

public static partial class ManufacturingEndpoints
{
    public record ParameterRule(
        string Code,
        string Name,
        string Uom,
        decimal Minimum,
        decimal Maximum
    );

    public record PhaseDefinition(
        string Name,
        string Instructions,
        bool RequiresSample,
        string[] Checks,
        ParameterRule[] Parameters
    );

    public record ResourceInput(
        Guid PlantId,
        string Code,
        string Name,
        bool Maintenance,
        Guid CommandId,
        uint Version = 0
    );

    public record RecipeInput(
        Guid PlantId,
        string Code,
        string Name,
        Guid MaterialId,
        string Precautions,
        PhaseDefinition[] Phases,
        Guid CommandId
    );

    public record BasicInput(Guid PlantId, Guid CommandId);

    public record RunInput(
        Guid PlantId,
        Guid ProductionOrderId,
        Guid ProductionRecipeId,
        Guid ProductionResourceId,
        DateTimeOffset ScheduledAt,
        bool Priority,
        Guid CommandId
    );

    public record RescheduleInput(
        Guid PlantId,
        Guid ProductionResourceId,
        DateTimeOffset ScheduledAt,
        bool Priority,
        string Reason,
        uint Version,
        Guid CommandId
    );

    public record StateInput(
        Guid PlantId,
        string Action,
        bool SafetyConfirmed,
        string Notes,
        Guid CommandId
    );

    public record ParameterValue(string Code, decimal Value);

    public record EvidenceInput(
        Guid PlantId,
        string[] Checks,
        ParameterValue[] Parameters,
        string Notes,
        uint Version,
        Guid CommandId
    );

    public record PackInput(
        Guid PlantId,
        Guid SourcePalletId,
        string PalletCode,
        decimal PackSize,
        int PackCount,
        decimal LooseQuantity,
        string PackagingReference,
        Guid CommandId
    );

    public record PutawayInput(
        Guid PlantId,
        Guid PalletId,
        string PalletCode,
        string DestinationBin,
        Guid CommandId
    );

    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Json);

    static T Parse<T>(string value) => JsonSerializer.Deserialize<T>(value, Json)!;

    static string Text(string? value, int max = 2000, bool required = false)
    {
        Ops.Check(
            value is not null
                && value.Trim().Length <= max
                && (!required || value.Trim().Length > 0),
            $"Supply {(required ? "nonempty " : "")}text up to {max} characters."
        );
        return value!.Trim();
    }

    static async Task<T> Get<T>(FactoryDb db, OpsScope s, Guid id)
        where T : class, IPlantRow =>
        await Ops.Rows<T>(db, s).SingleOrDefaultAsync(x => x.Id == id)
        ?? throw new OpsError(404, "Record not found in this plant.");

    static async Task<IResult> Command<T>(
        FactoryDb db,
        HttpContext h,
        Guid plantId,
        Guid commandId,
        string kind,
        Guid? target,
        T input,
        bool manager,
        Func<OpsScope, Task<Guid>> action
    )
    {
        var s = await Ops.Require(db, h.User, plantId, true, manager);
        Ops.Command(commandId);
        var hash = Convert.ToHexString(
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
        var prior = await Ops.Rows<ManufacturingCommand>(db, s)
            .SingleOrDefaultAsync(x => x.CommandId == commandId);
        if (prior != null)
        {
            Ops.Check(prior.Signature == hash, "Command ID was used with different input.");
            return Results.Ok(new { prior.ResultId });
        }
        var result = await action(s);
        db.Add(
            Ops.Stamp(
                new ManufacturingCommand
                {
                    CommandId = commandId,
                    Signature = hash,
                    Kind = kind,
                    ResultId = result,
                },
                s
            )
        );
        Ops.Audit(db, s, "Manufacturing." + kind, result);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return Results.Ok(new { ResultId = result });
    }

    static void Event(FactoryDb db, OpsScope s, Guid run, string kind, string notes = "") =>
        db.Add(
            Ops.Stamp(
                new ProductionEvent
                {
                    ProductionRunId = run,
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

    static bool Number(decimal value) =>
        value >= -1000000000m && value <= 1000000000m && decimal.Round(value, 6) == value;

    static void Rules(ParameterRule[]? rules)
    {
        Ops.Check(rules is { Length: <= 20 }, "Supply at most 20 parameter rules.");
        Ops.Check(rules!.All(x => x != null), "Null rules are not allowed.");
        foreach (var r in rules!)
        {
            Ops.Check(r.Code == Ops.Code(r.Code), "Parameter codes must be uppercase.");
            Ops.Name(r.Name);
            Text(r.Uom, 32);
            Ops.Check(
                Number(r.Minimum) && Number(r.Maximum) && r.Minimum <= r.Maximum,
                "Parameter limits must be valid ordered numbers."
            );
        }
        Ops.Check(
            rules.Select(x => x.Code).Distinct().Count() == rules.Length,
            "Parameter codes must be unique."
        );
    }

    public static void MapManufacturing(this WebApplication app)
    {
        var g = app.MapGroup("/api/operations/manufacturing").RequireAuthorization();
        Read<ProductionResource>(g, "/resources");
        Read<ProductionRecipe>(g, "/recipes");
        Read<ProductionRun>(g, "/runs");
        Read<FinishedPack>(g, "/packs");
        g.MapPost(
            "/resources",
            (ResourceInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "Resource",
                    null,
                    r,
                    true,
                    async s =>
                    {
                        var code = Ops.Code(r.Code);
                        var row = await Ops.Rows<ProductionResource>(db, s)
                            .SingleOrDefaultAsync(x => x.Code == code);
                        if (row == null)
                        {
                            row = Ops.Stamp(new ProductionResource { Code = code }, s);
                            db.Add(row);
                        }
                        else
                            Ops.Check(
                                row.Version == r.Version,
                                "Resource changed. Refresh before saving."
                            );
                        row.Name = Ops.Name(r.Name);
                        row.Maintenance = r.Maintenance;
                        return row.Id;
                    }
                )
        );
        g.MapPost(
            "/recipes",
            (RecipeInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "Recipe",
                    null,
                    r,
                    true,
                    async s =>
                    {
                        var material = await Get<Material>(db, s, r.MaterialId);
                        Ops.Check(material.Kind == "Finished", "A finished material is required.");
                        Ops.Check(
                            r.Phases is { Length: > 0 and <= 30 } && r.Phases.All(x => x != null),
                            "Supply 1–30 phases."
                        );
                        foreach (var phase in r.Phases)
                        {
                            Ops.Name(phase.Name);
                            Text(phase.Instructions);
                            Rules(phase.Parameters);
                            Ops.Check(
                                phase.Checks is { Length: <= 20 }
                                    && phase.Checks.All(x =>
                                        !string.IsNullOrWhiteSpace(x) && x.Length <= 160
                                    )
                                    && phase.Checks.Distinct().Count() == phase.Checks.Length,
                                "Supply up to 20 unique checklist prompts."
                            );
                        }
                        var code = Ops.Code(r.Code);
                        var prior = await Ops.Rows<ProductionRecipe>(db, s)
                            .Where(x => x.Code == code)
                            .ToListAsync();
                        Ops.Check(
                            prior.All(x => x.MaterialId == material.Id),
                            "Use a new recipe code for a different material."
                        );
                        var row = Ops.Stamp(
                            new ProductionRecipe
                            {
                                Code = code,
                                Name = Ops.Name(r.Name),
                                MaterialId = material.Id,
                                Revision =
                                    prior.Select(x => x.Revision).DefaultIfEmpty(0).Max() + 1,
                                Precautions = Text(r.Precautions),
                                PhasesJson = Serialize(r.Phases),
                            },
                            s
                        );
                        db.Add(row);
                        return row.Id;
                    }
                )
        );
        g.MapPost(
            "/recipes/{id:guid}/publish",
            (Guid id, BasicInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "PublishRecipe",
                    id,
                    r,
                    true,
                    async s =>
                    {
                        var row = await Get<ProductionRecipe>(db, s, id);
                        Ops.Check(row.Status == "Draft", "This revision is already published.");
                        row.Status = "Published";
                        row.PublishedAt = DateTimeOffset.UtcNow;
                        return row.Id;
                    }
                )
        );
        g.MapPost(
            "/runs",
            (RunInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "Schedule",
                    null,
                    r,
                    true,
                    async s =>
                    {
                        var order = await Get<ProductionOrder>(db, s, r.ProductionOrderId);
                        var recipe = await Get<ProductionRecipe>(db, s, r.ProductionRecipeId);
                        await Get<ProductionResource>(db, s, r.ProductionResourceId);
                        Ops.Check(
                            order.Status == "Released",
                            "Release the production order before scheduling execution."
                        );
                        Ops.Check(
                            recipe.Status == "Published" && recipe.MaterialId == order.MaterialId,
                            "Select a published recipe for this finished material."
                        );
                        Ops.Check(
                            !await Ops.Rows<ProductionRun>(db, s)
                                .AnyAsync(x => x.ProductionOrderId == order.Id),
                            "An execution run already exists for this order."
                        );
                        Ops.Check(
                            r.ScheduledAt.Year >= 2000 && r.ScheduledAt.Year <= 2200,
                            "Supply a valid scheduled date."
                        );
                        var definitions = Parse<PhaseDefinition[]>(recipe.PhasesJson);
                        var spec = await Ops.Rows<QualitySpecification>(db, s)
                            .Where(x => x.MaterialId == order.MaterialId && x.Status == "Published")
                            .OrderByDescending(x => x.Revision)
                            .FirstOrDefaultAsync();
                        Ops.Check(
                            !definitions.Any(x => x.RequiresSample) || spec != null,
                            "Publish a quality specification before scheduling phases with laboratory approval."
                        );
                        var run = Ops.Stamp(
                            new ProductionRun
                            {
                                ProductionOrderId = order.Id,
                                ProductionRecipeId = recipe.Id,
                                ProductionResourceId = r.ProductionResourceId,
                                QualitySpecificationId = spec?.Id,
                                ScheduledAt = r.ScheduledAt,
                                Priority = r.Priority,
                            },
                            s
                        );
                        db.Add(run);
                        foreach (var (phase, i) in definitions.Select((x, i) => (x, i)))
                            db.Add(
                                Ops.Stamp(
                                    new ProductionPhase
                                    {
                                        ProductionRunId = run.Id,
                                        Sequence = i + 1,
                                        Name = phase.Name,
                                        Instructions = phase.Instructions,
                                        RequiresSample = phase.RequiresSample,
                                        ChecksJson = Serialize(phase.Checks),
                                        ParametersJson = Serialize(phase.Parameters),
                                    },
                                    s
                                )
                            );
                        Event(
                            db,
                            s,
                            run.Id,
                            "Scheduled",
                            $"Recipe {recipe.Code} revision {recipe.Revision}"
                        );
                        return run.Id;
                    }
                )
        );
        g.MapPost(
            "/runs/{id:guid}/reschedule",
            (Guid id, RescheduleInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "Reschedule",
                    id,
                    r,
                    true,
                    async s =>
                    {
                        var run = await Get<ProductionRun>(db, s, id);
                        Ops.Check(
                            run.Status == "Scheduled",
                            "Only scheduled execution can be rescheduled."
                        );
                        Ops.Check(
                            run.Version == r.Version,
                            "Run changed. Refresh before rescheduling."
                        );
                        await Get<ProductionResource>(db, s, r.ProductionResourceId);
                        Ops.Check(
                            r.ScheduledAt.Year >= 2000 && r.ScheduledAt.Year <= 2200,
                            "Supply a valid scheduled date."
                        );
                        var reason = Text(r.Reason, 2000, true);
                        Event(
                            db,
                            s,
                            id,
                            "Rescheduled",
                            Serialize(
                                new
                                {
                                    oldResource = run.ProductionResourceId,
                                    oldSchedule = run.ScheduledAt,
                                    newResource = r.ProductionResourceId,
                                    newSchedule = r.ScheduledAt,
                                    reason,
                                }
                            )
                        );
                        run.ProductionResourceId = r.ProductionResourceId;
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
                var run = await Get<ProductionRun>(db, s, id);
                var order = await Get<ProductionOrder>(db, s, run.ProductionOrderId);
                var resource = await Get<ProductionResource>(db, s, run.ProductionResourceId);
                var recipe = await Get<ProductionRecipe>(db, s, run.ProductionRecipeId);
                var phases = await Ops.Rows<ProductionPhase>(db, s)
                    .Where(x => x.ProductionRunId == id)
                    .OrderBy(x => x.Sequence)
                    .ToListAsync();
                var ids = phases.Select(x => x.Id).ToArray();
                var samples = await Ops.Rows<QualitySample>(db, s)
                    .Where(x =>
                        x.ProductionPhaseId != null && ids.Contains(x.ProductionPhaseId.Value)
                    )
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(201)
                    .ToListAsync();
                var cases = await Ops.Rows<QualityCase>(db, s)
                    .Where(x => x.ProductionRunId == id && x.Status == "Open")
                    .Take(201)
                    .ToListAsync();
                var events = await Ops.Rows<ProductionEvent>(db, s)
                    .Where(x => x.ProductionRunId == id)
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(201)
                    .ToListAsync();
                await tx.CommitAsync();
                return Results.Ok(
                    new
                    {
                        run,
                        order,
                        resource,
                        recipe,
                        phases,
                        samples = samples.Take(200),
                        cases = cases.Take(200),
                        events = events.Take(200),
                        historyHasMore = events.Count > 200,
                        samplesHasMore = samples.Count > 200,
                        casesHasMore = cases.Count > 200,
                    }
                );
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
                    "RunState",
                    id,
                    r,
                    false,
                    async s =>
                    {
                        var run = await Get<ProductionRun>(db, s, id);
                        var order = await Get<ProductionOrder>(db, s, run.ProductionOrderId);
                        Ops.Check(order.Status == "Released", "Order must be released.");
                        var now = DateTimeOffset.UtcNow;
                        var notes = Text(r.Notes);
                        if (r.Action == "Start")
                        {
                            Ops.Check(run.Status == "Scheduled", "Only scheduled runs can start.");
                            Ops.Check(
                                r.SafetyConfirmed,
                                "Confirm the production notes and safety precautions before starting."
                            );
                            var resource = await Get<ProductionResource>(
                                db,
                                s,
                                run.ProductionResourceId
                            );
                            Ops.Check(!resource.Maintenance, "Resource is under maintenance.");
                            Ops.Check(
                                !await Ops.Rows<ProductionRun>(db, s)
                                    .AnyAsync(x =>
                                        x.Id != id
                                        && x.ProductionResourceId == resource.Id
                                        && (x.Status == "Running" || x.Status == "Paused")
                                    ),
                                "This resource is occupied by another run."
                            );
                            await MaterialReady(db, s, order);
                            run.SafetyConfirmed = true;
                            run.StartedAt = now;
                            run.Status = "Running";
                        }
                        else if (r.Action == "Pause")
                        {
                            Ops.Check(
                                run.Status == "Running",
                                "Only running production can pause."
                            );
                            Text(notes, 2000, true);
                            run.Status = "Paused";
                            run.PausedAt = now;
                        }
                        else if (r.Action == "Resume")
                        {
                            Ops.Check(run.Status == "Paused", "Only paused production can resume.");
                            var resource = await Get<ProductionResource>(
                                db,
                                s,
                                run.ProductionResourceId
                            );
                            Ops.Check(!resource.Maintenance, "Resource is under maintenance.");
                            run.PausedMinutes = decimal.Round(
                                run.PausedMinutes
                                    + (decimal)(now - run.PausedAt!.Value).TotalMinutes,
                                6
                            );
                            run.PausedAt = null;
                            run.Status = "Running";
                        }
                        else
                            throw new OpsError(400, "Choose Start, Pause or Resume.");
                        Event(db, s, id, r.Action, notes);
                        return id;
                    }
                )
        );
        g.MapPost(
            "/phases/{id:guid}/start",
            (Guid id, BasicInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "StartPhase",
                    id,
                    r,
                    false,
                    async s =>
                    {
                        var phase = await Get<ProductionPhase>(db, s, id);
                        var run = await Get<ProductionRun>(db, s, phase.ProductionRunId);
                        Ops.Check(
                            run.Status == "Running",
                            "Start or resume the production run first."
                        );
                        Ops.Check(phase.Status == "Pending", "This phase has already started.");
                        Ops.Check(
                            !await Ops.Rows<ProductionPhase>(db, s)
                                .AnyAsync(x =>
                                    x.ProductionRunId == run.Id
                                    && x.Sequence < phase.Sequence
                                    && x.Status != "Completed"
                                ),
                            "Complete all preceding phases first."
                        );
                        Ops.Check(
                            !await Ops.Rows<QualityCase>(db, s)
                                .AnyAsync(x => x.ProductionRunId == run.Id && x.Status == "Open"),
                            "Resolve open quality deviations before starting a phase."
                        );
                        phase.Status = "Running";
                        phase.StartedAt = DateTimeOffset.UtcNow;
                        Event(db, s, run.Id, "PhaseStarted", phase.Name);
                        return id;
                    }
                )
        );
        g.MapPost(
            "/phases/{id:guid}/evidence",
            (Guid id, EvidenceInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "PhaseEvidence",
                    id,
                    r,
                    false,
                    async s =>
                    {
                        var phase = await Get<ProductionPhase>(db, s, id);
                        var run = await Get<ProductionRun>(db, s, phase.ProductionRunId);
                        Ops.Check(
                            run.Status == "Running" && phase.Status == "Running",
                            "Evidence can only be recorded for a running phase and run."
                        );
                        Ops.Check(
                            phase.Version == r.Version,
                            "Phase changed. Refresh before recording evidence."
                        );
                        var checks = Parse<string[]>(phase.ChecksJson);
                        var rules = Parse<ParameterRule[]>(phase.ParametersJson);
                        Ops.Check(
                            r.Checks != null
                                && r.Checks.Length <= 20
                                && r.Checks.Distinct().Count() == r.Checks.Length
                                && r.Checks.All(checks.Contains),
                            "Checklist contains unknown or duplicate prompts."
                        );
                        Ops.Check(
                            r.Parameters != null
                                && r.Parameters.Length <= 20
                                && r.Parameters.All(x =>
                                    x != null && Number(x.Value) && rules.Any(y => y.Code == x.Code)
                                )
                                && r.Parameters.Select(x => x.Code).Distinct().Count()
                                    == r.Parameters.Length,
                            "Parameters contain unknown, duplicate or invalid readings."
                        );
                        var notes = Text(r.Notes);
                        phase.EvidenceJson = Serialize(
                            new
                            {
                                checks = r.Checks,
                                parameters = r.Parameters,
                                notes,
                            }
                        );
                        var outOfSpec = r.Parameters!.Where(x =>
                            {
                                var rule = rules.Single(y => y.Code == x.Code);
                                return x.Value < rule.Minimum || x.Value > rule.Maximum;
                            })
                            .Select(x => x.Code)
                            .ToArray();
                        if (outOfSpec.Length > 0)
                            await OpenCase(
                                db,
                                s,
                                null,
                                run.Id,
                                null,
                                "Out-of-range phase parameters: " + string.Join(", ", outOfSpec)
                            );
                        Event(
                            db,
                            s,
                            run.Id,
                            "PhaseEvidence",
                            Serialize(
                                new
                                {
                                    phaseId = id,
                                    evidence = new
                                    {
                                        checks = r.Checks,
                                        parameters = r.Parameters,
                                        notes,
                                    },
                                    outOfSpec,
                                }
                            )
                        );
                        return id;
                    }
                )
        );
        g.MapPost(
            "/phases/{id:guid}/complete",
            (Guid id, BasicInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "CompletePhase",
                    id,
                    r,
                    false,
                    async s =>
                    {
                        var phase = await Get<ProductionPhase>(db, s, id);
                        var run = await Get<ProductionRun>(db, s, phase.ProductionRunId);
                        Ops.Check(
                            run.Status == "Running" && phase.Status == "Running",
                            "Only a running phase and run can complete."
                        );
                        var evidence = Parse<EvidenceInput>(phase.EvidenceJson);
                        var checks = Parse<string[]>(phase.ChecksJson);
                        var rules = Parse<ParameterRule[]>(phase.ParametersJson);
                        Ops.Check(
                            checks.All(x => evidence.Checks?.Contains(x) == true),
                            "Complete every required phase checklist."
                        );
                        Ops.Check(
                            rules.All(x =>
                                evidence.Parameters?.Any(y =>
                                    y.Code == x.Code && y.Value >= x.Minimum && y.Value <= x.Maximum
                                ) == true
                            ),
                            "Record every process parameter within its limits."
                        );
                        var sample = await Ops.Rows<QualitySample>(db, s)
                            .Where(x => x.ProductionPhaseId == id)
                            .OrderByDescending(x => x.CreatedAt)
                            .ThenByDescending(x => x.Id)
                            .FirstOrDefaultAsync();
                        if (phase.RequiresSample || sample != null)
                            Ops.Check(
                                sample?.Status == "Passed",
                                "The latest phase sample must pass laboratory review before phase completion."
                            );
                        Ops.Check(
                            !await Ops.Rows<QualityCase>(db, s)
                                .AnyAsync(x => x.ProductionRunId == run.Id && x.Status == "Open"),
                            "Resolve open quality deviations before phase completion."
                        );
                        phase.Status = "Completed";
                        phase.CompletedAt = DateTimeOffset.UtcNow;
                        Event(db, s, run.Id, "PhaseCompleted", phase.Name);
                        return id;
                    }
                )
        );
        MapQuality(g);
        MapFinished(g);
    }

    static async Task MaterialReady(FactoryDb db, OpsScope s, ProductionOrder order)
    {
        Ops.Check(
            !await Ops.Rows<StagingPlan>(db, s)
                .AnyAsync(x =>
                    x.ProductionOrderId == order.Id && x.ApprovalRequired && !x.Approved
                ),
            "Engineering approval is pending."
        );
        var requirements = await Ops.Rows<BomLine>(db, s)
            .Where(x => x.ProductionOrderId == order.Id)
            .ToListAsync();
        Ops.Check(requirements.Count > 0, "Order has no material requirements.");
        foreach (var line in requirements)
        {
            var issued = await (
                from c in Ops.Rows<Consumption>(db, s)
                join p in Ops.Rows<Pallet>(db, s) on c.PalletId equals p.Id
                join b in Ops.Rows<Batch>(db, s) on p.BatchId equals b.Id
                where c.ProductionOrderId == order.Id && b.MaterialId == line.MaterialId
                select c.RequirementQuantity ?? c.Quantity
            ).SumAsync();
            Ops.Check(
                issued >= line.Quantity,
                "Issue all saved BOM requirements in Material Preparation before starting production."
            );
        }
    }

    public static async Task CompleteGuard(FactoryDb db, OpsScope s, Guid orderId)
    {
        var run = await Ops.Rows<ProductionRun>(db, s)
            .SingleOrDefaultAsync(x => x.ProductionOrderId == orderId);
        if (run == null)
            return;
        Ops.Check(
            run.Status == "Running",
            "Guided production must be running to report final output."
        );
        Ops.Check(
            !await Ops.Rows<ProductionPhase>(db, s)
                .AnyAsync(x => x.ProductionRunId == run.Id && x.Status != "Completed"),
            "Complete every saved SOP phase before final output."
        );
        Ops.Check(
            !await Ops.Rows<QualityCase>(db, s)
                .AnyAsync(x => x.ProductionRunId == run.Id && x.Status == "Open"),
            "Resolve open quality deviations before final output."
        );
        run.Status = "Completed";
        run.CompletedAt = DateTimeOffset.UtcNow;
        Event(
            db,
            s,
            run.Id,
            "Completed",
            "Final output reported; finished stock starts on quality hold."
        );
    }

    public static string OutputSignature(Guid orderId, ExecutionEndpoints.CompleteInput input) =>
        Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(Serialize(new { orderId, input })))
        );

    public static async Task CheckOutputCommand(
        FactoryDb db,
        OpsScope s,
        Guid command,
        string signature
    )
    {
        var prior = await Ops.Rows<ManufacturingCommand>(db, s)
            .SingleOrDefaultAsync(x => x.CommandId == command);
        Ops.Check(
            prior == null || prior.Kind == "FinalOutput" && prior.Signature == signature,
            "Command ID was used with different input."
        );
    }

    public static void RecordOutputCommand(
        FactoryDb db,
        OpsScope s,
        Guid command,
        string signature,
        Guid result
    ) =>
        db.Add(
            Ops.Stamp(
                new ManufacturingCommand
                {
                    CommandId = command,
                    Signature = signature,
                    Kind = "FinalOutput",
                    ResultId = result,
                },
                s
            )
        );
}
