using Microsoft.EntityFrameworkCore;

namespace GreenInk;

public static partial class ManufacturingEndpoints
{
    public record TestRule(
        string Code,
        string Name,
        string Kind,
        string Uom,
        decimal? Minimum,
        decimal? Maximum
    );

    public record SpecificationInput(
        Guid PlantId,
        Guid MaterialId,
        string Name,
        TestRule[] Tests,
        Guid CommandId
    );

    public record SampleInput(
        Guid PlantId,
        Guid QualitySpecificationId,
        Guid? BatchId,
        Guid? ProductionPhaseId,
        Guid? RetestOfId,
        string Notes,
        Guid CommandId
    );

    public record SampleStateInput(Guid PlantId, string Action, Guid CommandId);

    public record ReadingInput(
        Guid PlantId,
        string TestCode,
        decimal? NumericValue,
        bool? PassedValue,
        string Notes,
        Guid CommandId
    );

    public record ReviewInput(Guid PlantId, string Decision, string Notes, Guid CommandId);

    public record CaseInput(
        Guid PlantId,
        Guid? BatchId,
        Guid? ProductionRunId,
        string Severity,
        string Reason,
        Guid CommandId
    );

    public record CloseCaseInput(
        Guid PlantId,
        string RootCause,
        string CorrectiveAction,
        string Resolution,
        Guid CommandId
    );

    static void MapQuality(RouteGroupBuilder g)
    {
        Read<QualitySpecification>(g, "/specifications");
        Read<QualitySample>(g, "/samples");
        Read<QualityCase>(g, "/quality-cases");
        g.MapPost(
            "/specifications",
            (SpecificationInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "Specification",
                    null,
                    r,
                    true,
                    async s =>
                    {
                        await Get<Material>(db, s, r.MaterialId);
                        Ops.Check(
                            r.Tests is { Length: > 0 and <= 30 } && r.Tests.All(x => x != null),
                            "Supply 1–30 quality tests."
                        );
                        foreach (var t in r.Tests!)
                        {
                            Ops.Check(t.Code == Ops.Code(t.Code), "Test codes must be uppercase.");
                            Ops.Name(t.Name);
                            Text(t.Uom, 32);
                            Ops.Check(
                                t.Kind is "Numeric" or "PassFail",
                                "Test kind must be Numeric or PassFail."
                            );
                            Ops.Check(
                                t.Kind == "PassFail"
                                    ? t.Minimum == null && t.Maximum == null
                                    : t.Minimum.HasValue
                                        && t.Maximum.HasValue
                                        && Number(t.Minimum.Value)
                                        && Number(t.Maximum.Value)
                                        && t.Minimum <= t.Maximum,
                                "Numeric tests need valid ordered limits; PassFail tests have no numeric limits."
                            );
                        }
                        Ops.Check(
                            r.Tests!.Select(x => x.Code).Distinct().Count() == r.Tests.Length,
                            "Test codes must be unique."
                        );
                        var revision =
                            await Ops.Rows<QualitySpecification>(db, s)
                                .Where(x => x.MaterialId == r.MaterialId)
                                .Select(x => (int?)x.Revision)
                                .MaxAsync()
                            ?? 0;
                        var row = Ops.Stamp(
                            new QualitySpecification
                            {
                                MaterialId = r.MaterialId,
                                Name = Ops.Name(r.Name),
                                Revision = revision + 1,
                                TestsJson = Serialize(r.Tests),
                            },
                            s
                        );
                        db.Add(row);
                        return row.Id;
                    }
                )
        );
        g.MapPost(
            "/specifications/{id:guid}/publish",
            (Guid id, BasicInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "PublishSpecification",
                    id,
                    r,
                    true,
                    async s =>
                    {
                        var row = await Get<QualitySpecification>(db, s, id);
                        Ops.Check(row.Status == "Draft", "Specification is already published.");
                        row.Status = "Published";
                        return id;
                    }
                )
        );
        g.MapPost(
            "/samples",
            (SampleInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "CollectSample",
                    null,
                    r,
                    false,
                    async s =>
                    {
                        Ops.Check(
                            r.BatchId.HasValue != r.ProductionPhaseId.HasValue,
                            "Choose either a batch or a production phase."
                        );
                        var spec = await Get<QualitySpecification>(db, s, r.QualitySpecificationId);
                        Ops.Check(
                            spec.Status == "Published",
                            "Select a published quality specification."
                        );
                        Guid materialId;
                        if (r.BatchId.HasValue)
                        {
                            var batch = await Get<Batch>(db, s, r.BatchId.Value);
                            materialId = batch.MaterialId;
                            Ops.Check(
                                await Ops.Rows<Pallet>(db, s)
                                    .AnyAsync(x => x.BatchId == batch.Id && x.Quantity > 0),
                                "Batch has no on-hand stock to sample."
                            );
                            var latest = await Ops.Rows<QualitySpecification>(db, s)
                                .Where(x => x.MaterialId == materialId && x.Status == "Published")
                                .OrderByDescending(x => x.Revision)
                                .FirstAsync();
                            Ops.Check(
                                spec.Id == latest.Id,
                                "Use the latest published specification for a new batch sample."
                            );
                            Ops.Check(
                                batch.QualityStatus != "Rejected",
                                "Rejected stock cannot be resampled as released stock."
                            );
                            batch.QualityStatus = "Hold";
                        }
                        else
                        {
                            var phase = await Get<ProductionPhase>(
                                db,
                                s,
                                r.ProductionPhaseId!.Value
                            );
                            var run = await Get<ProductionRun>(db, s, phase.ProductionRunId);
                            var order = await Get<ProductionOrder>(db, s, run.ProductionOrderId);
                            materialId = order.MaterialId;
                            Ops.Check(
                                run.Status == "Running" && phase.Status == "Running",
                                "Collect phase samples only during active execution."
                            );
                            Ops.Check(
                                run.QualitySpecificationId == spec.Id,
                                "Use the quality specification snapshotted for this run."
                            );
                        }
                        Ops.Check(
                            spec.MaterialId == materialId,
                            "Specification material does not match the sample."
                        );
                        var notes = Text(r.Notes);
                        if (r.RetestOfId.HasValue)
                        {
                            var old = await Get<QualitySample>(db, s, r.RetestOfId.Value);
                            Ops.Check(
                                old.Status == "Failed"
                                    && old.BatchId == r.BatchId
                                    && old.ProductionPhaseId == r.ProductionPhaseId,
                                "Retests must reference a failed sample for the same batch or phase."
                            );
                            Text(notes, 2000, true);
                        }
                        var row = Ops.Stamp(
                            new QualitySample
                            {
                                QualitySpecificationId = spec.Id,
                                MaterialId = materialId,
                                BatchId = r.BatchId,
                                ProductionPhaseId = r.ProductionPhaseId,
                                RetestOfId = r.RetestOfId,
                                TestsJson = spec.TestsJson,
                                Notes = notes,
                            },
                            s
                        );
                        db.Add(row);
                        return row.Id;
                    }
                )
        );
        g.MapGet(
            "/samples/{id:guid}",
            async (Guid id, Guid plantId, FactoryDb db, HttpContext h) =>
            {
                var s = await Ops.Require(db, h.User, plantId);
                var sample = await Get<QualitySample>(db, s, id);
                var readings = await Ops.Rows<QualityTestReading>(db, s)
                    .Where(x => x.QualitySampleId == id)
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.Id)
                    .Take(501)
                    .ToListAsync();
                var cases = await Ops.Rows<QualityCase>(db, s)
                    .Where(x => x.QualitySampleId == id)
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(201)
                    .ToListAsync();
                return Results.Ok(
                    new
                    {
                        sample,
                        material = await Get<Material>(db, s, sample.MaterialId),
                        readings = readings.Take(500),
                        readingsHasMore = readings.Count > 500,
                        cases = cases.Take(200),
                        casesHasMore = cases.Count > 200,
                    }
                );
            }
        );
        g.MapPost(
            "/samples/{id:guid}/state",
            (Guid id, SampleStateInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "SampleState",
                    id,
                    r,
                    false,
                    async s =>
                    {
                        var sample = await Get<QualitySample>(db, s, id);
                        var now = DateTimeOffset.UtcNow;
                        if (r.Action == "Handover")
                        {
                            Ops.Check(
                                sample.Status == "Collected",
                                "Only collected samples can be handed over."
                            );
                            sample.Status = "HandedOver";
                            sample.HandedOverAt = now;
                        }
                        else if (r.Action == "Receive")
                        {
                            Ops.Check(
                                sample.Status == "HandedOver",
                                "Hand the sample over before laboratory receipt."
                            );
                            sample.Status = "Received";
                            sample.ReceivedAt = now;
                        }
                        else if (r.Action == "StartTesting")
                        {
                            Ops.Check(
                                sample.Status == "Received",
                                "Receive the sample before starting tests."
                            );
                            sample.Status = "Testing";
                            sample.TestingAt = now;
                        }
                        else
                            throw new OpsError(400, "Choose Handover, Receive or StartTesting.");
                        return id;
                    }
                )
        );
        g.MapPost(
            "/samples/{id:guid}/readings",
            (Guid id, ReadingInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "TestReading",
                    id,
                    r,
                    false,
                    async s =>
                    {
                        var sample = await Get<QualitySample>(db, s, id);
                        Ops.Check(
                            sample.Status == "Testing",
                            "Readings can only be added while testing; reviewed samples are locked."
                        );
                        Ops.Check(
                            await Ops.Rows<QualityTestReading>(db, s)
                                .CountAsync(x => x.QualitySampleId == id) < 500,
                            "This sample reached its reading limit. Review it and collect a new sample if needed."
                        );
                        var test = Parse<TestRule[]>(sample.TestsJson)
                            .SingleOrDefault(x => x.Code == r.TestCode);
                        Ops.Check(test != null, "Unknown sample test code.");
                        Ops.Check(
                            test!.Kind == "Numeric"
                                ? r.NumericValue.HasValue
                                    && Number(r.NumericValue.Value)
                                    && r.PassedValue == null
                                : r.PassedValue.HasValue && r.NumericValue == null,
                            "Supply exactly the configured numeric or pass/fail result."
                        );
                        var inSpec =
                            test.Kind == "Numeric"
                                ? r.NumericValue >= test.Minimum && r.NumericValue <= test.Maximum
                                : r.PassedValue == true;
                        var row = Ops.Stamp(
                            new QualityTestReading
                            {
                                QualitySampleId = id,
                                TestCode = r.TestCode,
                                NumericValue = r.NumericValue,
                                PassedValue = r.PassedValue,
                                InSpec = inSpec,
                                Notes = Text(r.Notes),
                                ActorId = s.ActorId,
                            },
                            s
                        );
                        db.Add(row);
                        if (!inSpec)
                        {
                            Guid? runId = null;
                            if (sample.ProductionPhaseId.HasValue)
                                runId = (
                                    await Get<ProductionPhase>(
                                        db,
                                        s,
                                        sample.ProductionPhaseId.Value
                                    )
                                ).ProductionRunId;
                            await OpenCase(
                                db,
                                s,
                                sample.BatchId,
                                runId,
                                id,
                                $"Sample {sample.Number}: {test.Name} outside specification."
                            );
                        }
                        return row.Id;
                    }
                )
        );
        g.MapPost(
            "/samples/{id:guid}/review",
            (Guid id, ReviewInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "ReviewSample",
                    id,
                    r,
                    true,
                    async s =>
                    {
                        var sample = await Get<QualitySample>(db, s, id);
                        Ops.Check(
                            sample.Status == "Testing",
                            "Only testing samples can be reviewed."
                        );
                        Ops.Check(r.Decision is "Passed" or "Failed", "Choose Passed or Failed.");
                        var notes = Text(r.Notes, 2000, true);
                        var readings = await Ops.Rows<QualityTestReading>(db, s)
                            .Where(x => x.QualitySampleId == id)
                            .OrderByDescending(x => x.CreatedAt)
                            .ThenByDescending(x => x.Id)
                            .ToListAsync();
                        if (r.Decision == "Passed")
                        {
                            Ops.Check(
                                Parse<TestRule[]>(sample.TestsJson)
                                    .All(t =>
                                        readings.FirstOrDefault(x => x.TestCode == t.Code)?.InSpec
                                        == true
                                    ),
                                "Every required test needs a latest in-spec result."
                            );
                            Guid? run = null;
                            if (sample.ProductionPhaseId.HasValue)
                                run = (
                                    await Get<ProductionPhase>(
                                        db,
                                        s,
                                        sample.ProductionPhaseId.Value
                                    )
                                ).ProductionRunId;
                            Ops.Check(
                                !await Ops.Rows<QualityCase>(db, s)
                                    .AnyAsync(x =>
                                        x.Status == "Open"
                                        && (
                                            (sample.BatchId != null && x.BatchId == sample.BatchId)
                                            || (run != null && x.ProductionRunId == run)
                                        )
                                    ),
                                "Close open nonconformances before passing laboratory review."
                            );
                        }
                        sample.Status = r.Decision;
                        sample.ReviewNotes = notes;
                        sample.ReviewedBy = s.ActorId;
                        sample.ReviewedAt = DateTimeOffset.UtcNow;
                        // Passing a sample never silently releases the batch: disposition is a separate manager decision.
                        if (r.Decision == "Failed")
                        {
                            Guid? run = null;
                            if (sample.ProductionPhaseId.HasValue)
                                run = (
                                    await Get<ProductionPhase>(
                                        db,
                                        s,
                                        sample.ProductionPhaseId.Value
                                    )
                                ).ProductionRunId;
                            await OpenCase(
                                db,
                                s,
                                sample.BatchId,
                                run,
                                id,
                                "Failed laboratory review: " + sample.Number
                            );
                        }
                        return id;
                    }
                )
        );
        g.MapPost(
            "/quality-cases",
            (CaseInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "Nonconformance",
                    null,
                    r,
                    false,
                    async s =>
                    {
                        Ops.Check(
                            r.BatchId.HasValue != r.ProductionRunId.HasValue,
                            "Choose either a batch or an execution run."
                        );
                        Ops.Check(
                            r.Severity is "Minor" or "Major" or "Critical",
                            "Choose Minor, Major or Critical severity."
                        );
                        if (r.BatchId.HasValue)
                            await Get<Batch>(db, s, r.BatchId.Value);
                        if (r.ProductionRunId.HasValue)
                        {
                            var run = await Get<ProductionRun>(db, s, r.ProductionRunId.Value);
                            Ops.Check(
                                run.Status != "Completed",
                                "Use the finished batch for deviations after production completion."
                            );
                        }
                        return await OpenCase(
                            db,
                            s,
                            r.BatchId,
                            r.ProductionRunId,
                            null,
                            Text(r.Reason, 2000, true),
                            r.Severity,
                            false
                        );
                    }
                )
        );
        g.MapPost(
            "/quality-cases/{id:guid}/close",
            (Guid id, CloseCaseInput r, FactoryDb db, HttpContext h) =>
                Command(
                    db,
                    h,
                    r.PlantId,
                    r.CommandId,
                    "CloseNonconformance",
                    id,
                    r,
                    true,
                    async s =>
                    {
                        var row = await Get<QualityCase>(db, s, id);
                        Ops.Check(
                            row.Status == "Open",
                            "This nonconformance has already been closed."
                        );
                        row.RootCause = Text(r.RootCause, 2000, true);
                        row.CorrectiveAction = Text(r.CorrectiveAction, 2000, true);
                        row.Resolution = Text(r.Resolution, 2000, true);
                        row.Status = "Closed";
                        row.ClosedAt = DateTimeOffset.UtcNow;
                        row.ReviewedBy = s.ActorId;
                        return id;
                    }
                )
        );
    }

    static async Task<Guid> OpenCase(
        FactoryDb db,
        OpsScope s,
        Guid? batch,
        Guid? run,
        Guid? sample,
        string reason,
        string severity = "Major",
        bool reuse = true
    )
    {
        if (batch.HasValue)
            (await Get<Batch>(db, s, batch.Value)).QualityStatus = "Hold";
        if (reuse)
        {
            var old = await Ops.Rows<QualityCase>(db, s)
                .FirstOrDefaultAsync(x =>
                    x.Status == "Open"
                    && x.BatchId == batch
                    && x.ProductionRunId == run
                    && x.QualitySampleId == sample
                );
            if (old != null)
                return old.Id;
        }
        var row = Ops.Stamp(
            new QualityCase
            {
                BatchId = batch,
                ProductionRunId = run,
                QualitySampleId = sample,
                Reason = reason,
                Severity = severity,
            },
            s
        );
        db.Add(row);
        return row.Id;
    }

    public static async Task ReleaseGuard(FactoryDb db, OpsScope s, Guid batchId)
    {
        var batch = await Get<Batch>(db, s, batchId);
        Ops.Check(
            !await Ops.Rows<QualityCase>(db, s)
                .AnyAsync(x => x.BatchId == batchId && x.Status == "Open"),
            "Close open batch nonconformances before release."
        );
        var sample = await Ops.Rows<QualitySample>(db, s)
            .Where(x => x.BatchId == batchId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync();
        var specificationRequired = await Ops.Rows<QualitySpecification>(db, s)
            .AnyAsync(x => x.MaterialId == batch.MaterialId && x.Status == "Published");
        Ops.Check(
            !specificationRequired && sample == null || sample?.Status == "Passed",
            "The latest batch sample must pass all tests and laboratory review before release."
        );
    }
}
