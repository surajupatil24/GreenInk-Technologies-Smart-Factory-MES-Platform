using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GreenInk;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GreenInk.Tests;

public sealed class ManufacturingTests : IAsyncLifetime
{
    readonly TestFactory factory = new();
    const string Root = "/api/operations/";
    const string M = "manufacturing/";

    public Task InitializeAsync() => factory.InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)factory).DisposeAsync();

    static Guid Id(JsonElement r) => r.GetProperty("id").GetGuid();

    static Guid Result(JsonElement r) => r.GetProperty("resultId").GetGuid();

    static async Task<JsonElement> Post(HttpClient c, string path, object input)
    {
        using var r = await c.PostAsJsonAsync(Root + path, input);
        Assert.True(
            r.IsSuccessStatusCode,
            $"{path}: {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}"
        );
        return await r.Content.ReadFromJsonAsync<JsonElement>();
    }

    static async Task Bad(HttpClient c, string path, object input, string message)
    {
        using var r = await c.PostAsJsonAsync(Root + path, input);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains(message, await r.Content.ReadAsStringAsync());
    }

    async Task<JsonElement> Read(HttpClient c, string path) =>
        await c.GetFromJsonAsync<JsonElement>(Root + path + "?plantId=" + factory.PlantA);

    object Basic() => new { plantId = factory.PlantA, commandId = Guid.NewGuid() };

    record Data(
        Guid Raw,
        Guid Finished,
        Guid Source,
        Guid Order,
        Guid Run,
        Guid Spec,
        Guid Recipe,
        Guid Resource
    );

    async Task<Data> Setup(HttpClient c, bool needsSample = true)
    {
        var plantId = factory.PlantA;
        var raw = Id(
            await Post(
                c,
                "materials",
                new
                {
                    plantId,
                    code = "RM",
                    name = "Test raw",
                    uom = "kg",
                    kind = "Raw",
                }
            )
        );
        var fg = Id(
            await Post(
                c,
                "materials",
                new
                {
                    plantId,
                    code = "FG",
                    name = "Test finished",
                    uom = "kg",
                    kind = "Finished",
                }
            )
        );
        var supplier = Id(
            await Post(
                c,
                "partners",
                new
                {
                    plantId,
                    code = "SUP",
                    name = "Test supplier",
                    kind = "Supplier",
                }
            )
        );
        await Post(
            c,
            "bins",
            new
            {
                plantId,
                code = "FG-01",
                name = "Finished bay",
                warehouse = "WH",
            }
        );
        await Post(
            c,
            "bins",
            new
            {
                plantId,
                code = "FG-02",
                name = "Finished rack",
                warehouse = "WH",
            }
        );
        var po = Id(
            await Post(
                c,
                "purchase-orders",
                new
                {
                    plantId,
                    number = "PO",
                    supplierId = supplier,
                    materialId = raw,
                    quantity = 100m,
                }
            )
        );
        var receipt = await Post(
            c,
            "receipts",
            new
            {
                plantId,
                purchaseOrderId = po,
                quantity = 100m,
                supplierBatch = "SUP-LOT",
                batchNumber = "BATCH",
                commandId = Guid.NewGuid(),
            }
        );
        await Post(
            c,
            "quality-inspections",
            new
            {
                plantId,
                batchId = receipt.GetProperty("batchId").GetGuid(),
                decision = "Released",
                notes = "Incoming passed",
                commandId = Guid.NewGuid(),
            }
        );
        var source = receipt.GetProperty("palletId").GetGuid();
        var order = await CreateIssued(c, fg, raw, source, "ORDER-1");
        var spec = Result(
            await Post(
                c,
                M + "specifications",
                new
                {
                    plantId,
                    materialId = fg,
                    name = "Finished specification",
                    tests = new object[]
                    {
                        new
                        {
                            code = "PH",
                            name = "Acidity",
                            kind = "Numeric",
                            uom = "pH",
                            minimum = 6m,
                            maximum = 8m,
                        },
                        new
                        {
                            code = "LOOK",
                            name = "Appearance",
                            kind = "PassFail",
                            uom = "",
                            minimum = (decimal?)null,
                            maximum = (decimal?)null,
                        },
                    },
                    commandId = Guid.NewGuid(),
                }
            )
        );
        await Post(c, M + $"specifications/{spec}/publish", Basic());
        var resource = Result(
            await Post(
                c,
                M + "resources",
                new
                {
                    plantId,
                    code = "MIX-01",
                    name = "Mixer",
                    maintenance = false,
                    commandId = Guid.NewGuid(),
                }
            )
        );
        var recipe = Result(
            await Post(
                c,
                M + "recipes",
                new
                {
                    plantId,
                    code = "MIX",
                    name = "Mix and finish",
                    materialId = fg,
                    precautions = "Wear PPE",
                    phases = new object[]
                    {
                        new
                        {
                            name = "Mixing",
                            instructions = "Confirm seal and temperature",
                            requiresSample = needsSample,
                            checks = new[] { "Seal checked" },
                            parameters = new[]
                            {
                                new
                                {
                                    code = "TEMP",
                                    name = "Temperature",
                                    uom = "C",
                                    minimum = 20m,
                                    maximum = 30m,
                                },
                            },
                        },
                        new
                        {
                            name = "Finish",
                            instructions = "Confirm clean outlet",
                            requiresSample = false,
                            checks = new[] { "Outlet checked" },
                            parameters = Array.Empty<object>(),
                        },
                    },
                    commandId = Guid.NewGuid(),
                }
            )
        );
        await Post(c, M + $"recipes/{recipe}/publish", Basic());
        var run = Result(
            await Post(
                c,
                M + "runs",
                new
                {
                    plantId,
                    productionOrderId = order,
                    productionRecipeId = recipe,
                    productionResourceId = resource,
                    scheduledAt = DateTimeOffset.UtcNow,
                    priority = false,
                    commandId = Guid.NewGuid(),
                }
            )
        );
        return new(raw, fg, source, order, run, spec, recipe, resource);
    }

    async Task<Guid> CreateIssued(HttpClient c, Guid finished, Guid raw, Guid pallet, string number)
    {
        var plantId = factory.PlantA;
        var order = Id(
            await Post(
                c,
                "production-orders",
                new
                {
                    plantId,
                    number,
                    materialId = finished,
                    plannedQuantity = 10m,
                    requirements = new[] { new { materialId = raw, quantity = 10m } },
                }
            )
        );
        await Post(c, $"production-orders/{order}/release", Basic());
        var reservation = Id(
            await Post(
                c,
                "reservations",
                new
                {
                    plantId,
                    productionOrderId = order,
                    palletId = pallet,
                    quantity = 10m,
                    commandId = Guid.NewGuid(),
                }
            )
        );
        await Post(c, $"reservations/{reservation}/issue", Basic());
        return order;
    }

    async Task State(HttpClient c, Guid run, string action, bool safe = true) =>
        await Post(
            c,
            M + $"runs/{run}/state",
            new
            {
                plantId = factory.PlantA,
                action,
                safetyConfirmed = safe,
                notes = "Operator note",
                commandId = Guid.NewGuid(),
            }
        );

    async Task<JsonElement> Phase(HttpClient c, Guid run, int index = 0) =>
        (await Read(c, M + $"runs/{run}")).GetProperty("phases")[index];

    async Task Evidence(HttpClient c, Guid run, int index, decimal temperature = 25m)
    {
        var phase = await Phase(c, run, index);
        await Post(
            c,
            M + $"phases/{Id(phase)}/evidence",
            new
            {
                plantId = factory.PlantA,
                checks = new[] { index == 0 ? "Seal checked" : "Outlet checked" },
                parameters = index == 0
                    ? new[] { new { code = "TEMP", value = temperature } }
                    : Array.Empty<object>(),
                notes = "Measured manually",
                version = phase.GetProperty("version").GetUInt32(),
                commandId = Guid.NewGuid(),
            }
        );
    }

    async Task<Guid> Sample(HttpClient c, Data a, Guid? phase, Guid? batch, Guid? retest = null)
    {
        var result = await Post(
            c,
            M + "samples",
            new
            {
                plantId = factory.PlantA,
                qualitySpecificationId = a.Spec,
                batchId = batch,
                productionPhaseId = phase,
                retestOfId = retest,
                notes = "Collected for review",
                commandId = Guid.NewGuid(),
            }
        );
        return Result(result);
    }

    async Task TestSample(HttpClient c, Guid id, decimal value = 7m, bool review = true)
    {
        var plantId = factory.PlantA;
        foreach (var action in new[] { "Handover", "Receive", "StartTesting" })
            await Post(
                c,
                M + $"samples/{id}/state",
                new
                {
                    plantId,
                    action,
                    commandId = Guid.NewGuid(),
                }
            );
        await Post(
            c,
            M + $"samples/{id}/readings",
            new
            {
                plantId,
                testCode = "PH",
                numericValue = value,
                passedValue = (bool?)null,
                notes = "Manual lab reading",
                commandId = Guid.NewGuid(),
            }
        );
        await Post(
            c,
            M + $"samples/{id}/readings",
            new
            {
                plantId,
                testCode = "LOOK",
                numericValue = (decimal?)null,
                passedValue = true,
                notes = "Visual review",
                commandId = Guid.NewGuid(),
            }
        );
        if (review)
            await Post(
                c,
                M + $"samples/{id}/review",
                new
                {
                    plantId,
                    decision = "Passed",
                    notes = "All tests accepted",
                    commandId = Guid.NewGuid(),
                }
            );
    }

    [Fact]
    public async Task GuidedExecutionBlocksMissingEvidenceAndLaboratoryThenConservesPackedStock()
    {
        using var c = await factory.Login("admin@example.test");
        var a = await Setup(c);
        var plantId = factory.PlantA;
        await Bad(
            c,
            M + $"runs/{a.Run}/state",
            new
            {
                plantId,
                action = "Start",
                safetyConfirmed = false,
                notes = "",
                commandId = Guid.NewGuid(),
            },
            "safety precautions"
        );
        await State(c, a.Run, "Start");
        await State(c, a.Run, "Pause");
        var phase = await Phase(c, a.Run);
        await Bad(c, M + $"phases/{Id(phase)}/start", Basic(), "resume");
        await State(c, a.Run, "Resume");
        var phase2 = await Phase(c, a.Run, 1);
        await Bad(c, M + $"phases/{Id(phase2)}/start", Basic(), "preceding");
        await Post(c, M + $"phases/{Id(phase)}/start", Basic());
        await Bad(c, M + $"phases/{Id(phase)}/complete", Basic(), "checklist");
        await Evidence(c, a.Run, 0, 99m);
        var changed = await Phase(c, a.Run);
        await Bad(
            c,
            M + $"phases/{Id(phase)}/evidence",
            new
            {
                plantId,
                checks = new[] { "Seal checked" },
                parameters = new[] { new { code = "TEMP", value = 25m } },
                notes = "stale",
                version = phase.GetProperty("version").GetUInt32(),
                commandId = Guid.NewGuid(),
            },
            "Phase changed"
        );
        await Evidence(c, a.Run, 0);
        await Bad(c, M + $"phases/{Id(phase)}/complete", Basic(), "latest phase sample");
        var sample = await Sample(c, a, Id(phase), null);
        await TestSample(c, sample, review: false);
        await Bad(
            c,
            M + $"samples/{sample}/review",
            new
            {
                plantId,
                decision = "Passed",
                notes = "Accept",
                commandId = Guid.NewGuid(),
            },
            "nonconformances"
        );
        var issue = (await Read(c, M + "quality-cases")).GetProperty("items")[0];
        await Post(
            c,
            M + $"quality-cases/{Id(issue)}/close",
            new
            {
                plantId,
                rootCause = "Incorrect manual entry",
                correctiveAction = "Remeasured calibrated vessel",
                resolution = "Verified evidence and acceptable reading",
                commandId = Guid.NewGuid(),
            }
        );
        await Post(
            c,
            M + $"samples/{sample}/review",
            new
            {
                plantId,
                decision = "Passed",
                notes = "Accepted",
                commandId = Guid.NewGuid(),
            }
        );
        await Post(c, M + $"phases/{Id(phase)}/complete", Basic());
        await Post(c, M + $"phases/{Id(phase2)}/start", Basic());
        var completion = new
        {
            plantId,
            quantity = 9m,
            scrapQuantity = 1m,
            location = "FG-01",
            commandId = Guid.NewGuid(),
        };
        await Bad(c, $"production-orders/{a.Order}/complete", completion, "every saved SOP phase");
        await Evidence(c, a.Run, 1);
        await Post(c, M + $"phases/{Id(phase2)}/complete", Basic());
        var finished = await Post(c, $"production-orders/{a.Order}/complete", completion);
        Assert.Equal(
            Id(finished),
            Id(await Post(c, $"production-orders/{a.Order}/complete", completion))
        );
        var batch = finished.GetProperty("batchId").GetGuid();
        var source = finished.GetProperty("palletId").GetGuid();
        var release = new
        {
            plantId,
            batchId = batch,
            decision = "Released",
            notes = "Quality accepted",
            commandId = Guid.NewGuid(),
        };
        await Bad(c, "quality-inspections", release, "latest batch sample");
        var batchSample = await Sample(c, a, null, batch);
        await TestSample(c, batchSample);
        await Post(c, "quality-inspections", release);
        var pallets = await Read(c, "pallets");
        var code = pallets
            .EnumerateArray()
            .Single(x => Id(x) == source)
            .GetProperty("code")
            .GetString();
        var pack = new
        {
            plantId,
            sourcePalletId = source,
            palletCode = code,
            packSize = 2m,
            packCount = 2,
            looseQuantity = 1m,
            packagingReference = "Food grade bags",
            commandId = Guid.NewGuid(),
        };
        var packed = Result(await Post(c, M + "packs", pack));
        Assert.Equal(packed, Result(await Post(c, M + "packs", pack)));
        await Bad(
            c,
            M + "packs",
            new
            {
                plantId,
                sourcePalletId = source,
                palletCode = code,
                packSize = 2m,
                packCount = 3,
                looseQuantity = 1m,
                packagingReference = "Food grade bags",
                commandId = pack.commandId,
            },
            "different input"
        );
        var label = await Read(c, M + $"packs/{packed}/label");
        var packedCode = label.GetProperty("palletCode").GetString();
        var packRow = (await Read(c, M + "packs")).GetProperty("items")[0];
        var output = packRow.GetProperty("outputPalletId").GetGuid();
        Assert.Contains("<svg", label.GetProperty("qrSvg").GetString());
        await Bad(
            c,
            M + "putaway",
            new
            {
                plantId,
                palletId = output,
                palletCode = "WRONG",
                destinationBin = "FG-02",
                commandId = Guid.NewGuid(),
            },
            "Scan the selected"
        );
        var putaway = new
        {
            plantId,
            palletId = output,
            palletCode = packedCode,
            destinationBin = "FG-02",
            commandId = Guid.NewGuid(),
        };
        await Post(c, M + "putaway", putaway);
        await Post(c, M + "putaway", putaway);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
        Assert.Equal("Completed", (await db.Set<ProductionRun>().SingleAsync()).Status);
        Assert.Equal(
            9m,
            await db.Set<Pallet>().Where(x => x.BatchId == batch).SumAsync(x => x.Quantity)
        );
        Assert.Equal(4m, (await db.Set<Pallet>().SingleAsync(x => x.Id == source)).Quantity);
        Assert.Equal(5m, (await db.Set<Pallet>().SingleAsync(x => x.Id == output)).Quantity);
        Assert.Equal("FG-02", (await db.Set<Pallet>().SingleAsync(x => x.Id == output)).Location);
        Assert.Equal(
            0m,
            await db.Set<StockMovement>()
                .Where(x => x.Kind == "PackIn" || x.Kind == "PackOut")
                .SumAsync(x => x.Quantity)
        );
        Assert.Equal(
            2,
            await db.Set<StockMovement>().CountAsync(x => x.Kind == "PackIn" || x.Kind == "PackOut")
        );
        Assert.Single(
            await db.Set<StockMovement>().Where(x => x.Kind == "FinishedPutaway").ToListAsync()
        );
    }

    [Fact]
    public async Task FailedTestsRequireReviewedCorrectionOrRetestAndCannotBypassBatchDisposition()
    {
        using var c = await factory.Login("admin@example.test");
        var a = await Setup(c, false);
        var plantId = factory.PlantA;
        // Seed finished output independently so this test isolates laboratory history and direct-release gates.
        Guid batch;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            var b = new Batch
            {
                TenantId = factory.TenantA,
                PlantId = plantId,
                MaterialId = a.Finished,
                Number = "FG-QC",
                Kind = "Finished",
                QualityStatus = "Hold",
            };
            batch = b.Id;
            db.Add(b);
            db.Add(
                new Pallet
                {
                    TenantId = factory.TenantA,
                    PlantId = plantId,
                    BatchId = batch,
                    Code = "FG-PAL",
                    Quantity = 10,
                    Location = "FG-01",
                }
            );
            await db.SaveChangesAsync();
        }
        var sample = await Sample(c, a, null, batch);
        await Bad(
            c,
            M + $"samples/{sample}/state",
            new
            {
                plantId,
                action = "StartTesting",
                commandId = Guid.NewGuid(),
            },
            "Receive"
        );
        await TestSample(c, sample, 20m, false);
        await Bad(
            c,
            M + $"samples/{sample}/review",
            new
            {
                plantId,
                decision = "Passed",
                notes = "Invalid approval",
                commandId = Guid.NewGuid(),
            },
            "in-spec"
        );
        await Post(
            c,
            M + $"samples/{sample}/review",
            new
            {
                plantId,
                decision = "Failed",
                notes = "Result outside limits",
                commandId = Guid.NewGuid(),
            }
        );
        await Bad(
            c,
            M + $"samples/{sample}/readings",
            new
            {
                plantId,
                testCode = "PH",
                numericValue = 7m,
                passedValue = (bool?)null,
                notes = "Overwrite",
                commandId = Guid.NewGuid(),
            },
            "locked"
        );
        var retest = await Sample(c, a, null, batch, sample);
        await TestSample(c, retest, 7m, false);
        var qc = (await Read(c, M + "quality-cases")).GetProperty("items")[0];
        await Post(
            c,
            M + $"quality-cases/{Id(qc)}/close",
            new
            {
                plantId,
                rootCause = "Sample contamination",
                correctiveAction = "New collection with clean vessel",
                resolution = "Retest confirms acceptable material",
                commandId = Guid.NewGuid(),
            }
        );
        await Post(
            c,
            M + $"samples/{retest}/review",
            new
            {
                plantId,
                decision = "Passed",
                notes = "Retest accepted",
                commandId = Guid.NewGuid(),
            }
        );
        var detail = await Read(c, M + $"samples/{sample}");
        Assert.Equal("Failed", detail.GetProperty("sample").GetProperty("status").GetString());
        Assert.Equal(
            20m,
            detail.GetProperty("readings")[1].GetProperty("numericValue").GetDecimal()
        );
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            Assert.Equal(
                "Hold",
                (await db.Set<Batch>().SingleAsync(x => x.Id == batch)).QualityStatus
            );
        }
        await Post(
            c,
            "quality-inspections",
            new
            {
                plantId,
                batchId = batch,
                decision = "Released",
                notes = "Approved after retest",
                commandId = Guid.NewGuid(),
            }
        );
        await Post(
            c,
            M + "quality-cases",
            new
            {
                plantId,
                batchId = batch,
                productionRunId = (Guid?)null,
                severity = "Critical",
                reason = "Packaging damage",
                commandId = Guid.NewGuid(),
            }
        );
        await Bad(
            c,
            "quality-inspections",
            new
            {
                plantId,
                batchId = batch,
                decision = "Released",
                notes = "Cannot bypass",
                commandId = Guid.NewGuid(),
            },
            "nonconformances"
        );
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            Assert.Equal(
                "Hold",
                (await db.Set<Batch>().SingleAsync(x => x.Id == batch)).QualityStatus
            );
        }
    }

    [Fact]
    public async Task ConcurrentStartsKeepOneResourceExclusiveAndRecipesStaySnapshotted()
    {
        using var c = await factory.Login("admin@example.test");
        var a = await Setup(c, false);
        var plantId = factory.PlantA;
        var order = await CreateIssued(c, a.Finished, a.Raw, a.Source, "ORDER-2");
        var second = Result(
            await Post(
                c,
                M + "runs",
                new
                {
                    plantId,
                    productionOrderId = order,
                    productionRecipeId = a.Recipe,
                    productionResourceId = a.Resource,
                    scheduledAt = DateTimeOffset.UtcNow,
                    priority = true,
                    commandId = Guid.NewGuid(),
                }
            )
        );
        var requests = new[] { a.Run, second }
            .Select(run =>
                c.PostAsJsonAsync(
                    Root + M + $"runs/{run}/state",
                    new
                    {
                        plantId,
                        action = "Start",
                        safetyConfirmed = true,
                        notes = "Concurrent",
                        commandId = Guid.NewGuid(),
                    }
                )
            )
            .ToArray();
        var responses = await Task.WhenAll(requests);
        try
        {
            Assert.Single(responses, r => r.IsSuccessStatusCode);
            Assert.All(
                responses.Where(r => !r.IsSuccessStatusCode),
                r =>
                    Assert.True(
                        r.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict
                    )
            );
        }
        finally
        {
            foreach (var r in responses)
                r.Dispose();
        }
        var newer = Result(
            await Post(
                c,
                M + "recipes",
                new
                {
                    plantId,
                    code = "MIX",
                    name = "Revised instructions",
                    materialId = a.Finished,
                    precautions = "New precautions",
                    phases = new[]
                    {
                        new
                        {
                            name = "New phase",
                            instructions = "New instructions",
                            requiresSample = false,
                            checks = Array.Empty<string>(),
                            parameters = Array.Empty<object>(),
                        },
                    },
                    commandId = Guid.NewGuid(),
                }
            )
        );
        await Post(c, M + $"recipes/{newer}/publish", Basic());
        var detail = await Read(c, M + $"runs/{a.Run}");
        Assert.Equal(1, detail.GetProperty("recipe").GetProperty("revision").GetInt32());
        Assert.Equal(2, detail.GetProperty("phases").GetArrayLength());
        await Bad(c, M + $"recipes/{a.Recipe}/publish", Basic(), "already published");
    }

    [Fact]
    public async Task NewWorkspaceScopeViewerAndOperatorPrivilegesAreEnforced()
    {
        using var c = await factory.Login("admin@example.test");
        using var viewer = await factory.Login("viewer@example.test");
        var a = await Setup(c, false);
        var plantId = factory.PlantA;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            var user = await db
                .Users.Include(x => x.Plants)
                .SingleAsync(x => x.Email == "viewer@example.test");
            user.Role = "Operator";
            await db.SaveChangesAsync();
        }
        using var denied = await viewer.PostAsJsonAsync(
            Root + M + "resources",
            new
            {
                plantId,
                code = "BAD",
                name = "Unauthorized",
                maintenance = false,
                commandId = Guid.NewGuid(),
            }
        );
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await viewer.GetAsync(Root + M + "runs?plantId=" + factory.PlantB)).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await c.GetAsync(Root + M + $"runs/{a.Run}?plantId=" + factory.PlantAOther)).StatusCode
        );
        await Bad(
            c,
            M + "runs",
            new
            {
                plantId,
                productionOrderId = a.Order,
                productionRecipeId = a.Recipe,
                productionResourceId = a.Resource,
                scheduledAt = DateTimeOffset.UtcNow,
                priority = false,
                commandId = Guid.NewGuid(),
            },
            "already exists"
        );
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            var user = await db.Users.SingleAsync(x => x.Email == "viewer@example.test");
            user.Role = "Viewer";
            await db.SaveChangesAsync();
        }
        using var noStart = await viewer.PostAsJsonAsync(
            Root + M + $"runs/{a.Run}/state",
            new
            {
                plantId,
                action = "Start",
                safetyConfirmed = true,
                notes = "",
                commandId = Guid.NewGuid(),
            }
        );
        Assert.Equal(HttpStatusCode.Forbidden, noStart.StatusCode);
        var detail = await Read(viewer, M + $"runs/{a.Run}");
        Assert.Equal(a.Run, Id(detail.GetProperty("run")));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            db.Add(
                new ProductionRun
                {
                    TenantId = factory.TenantA,
                    PlantId = factory.PlantAOther,
                    ProductionOrderId = a.Order,
                    ProductionRecipeId = a.Recipe,
                    ProductionResourceId = a.Resource,
                    ScheduledAt = DateTimeOffset.UtcNow,
                }
            );
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task PackingRacesNeverDuplicateStockAndRejectHeldOrExpiredBatches()
    {
        using var c = await factory.Login("admin@example.test");
        var plantId = factory.PlantA;
        Guid palletId;
        Guid batchId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            var material = new Material
            {
                TenantId = factory.TenantA,
                PlantId = plantId,
                Code = "FG",
                Name = "Finished",
                Kind = "Finished",
                Uom = "kg",
            };
            var batch = new Batch
            {
                TenantId = factory.TenantA,
                PlantId = plantId,
                MaterialId = material.Id,
                Number = "FG",
                Kind = "Finished",
                QualityStatus = "Hold",
            };
            var pallet = new Pallet
            {
                TenantId = factory.TenantA,
                PlantId = plantId,
                BatchId = batch.Id,
                Code = "PACK-SOURCE",
                Quantity = 10,
                Location = "FG",
            };
            db.AddRange(material, batch, pallet);
            await db.SaveChangesAsync();
            palletId = pallet.Id;
            batchId = batch.Id;
        }
        object Input() =>
            new
            {
                plantId,
                sourcePalletId = palletId,
                palletCode = "PACK-SOURCE",
                packSize = 2m,
                packCount = 3,
                looseQuantity = 0m,
                packagingReference = "Bags",
                commandId = Guid.NewGuid(),
            };
        await Bad(c, M + "packs", Input(), "released finished");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            var b = await db.Set<Batch>().SingleAsync();
            b.QualityStatus = "Released";
            b.ExpiryDate = DateTimeOffset.UtcNow.AddDays(-1);
            await db.SaveChangesAsync();
        }
        await Bad(c, M + "packs", Input(), "expired");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            (await db.Set<Batch>().SingleAsync()).ExpiryDate = null;
            await db.SaveChangesAsync();
        }
        var results = await Task.WhenAll(
            c.PostAsJsonAsync(Root + M + "packs", Input()),
            c.PostAsJsonAsync(Root + M + "packs", Input())
        );
        try
        {
            Assert.Single(results, r => r.IsSuccessStatusCode);
        }
        finally
        {
            foreach (var r in results)
                r.Dispose();
        }
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            Assert.Equal(
                10m,
                await db.Set<Pallet>().Where(x => x.BatchId == batchId).SumAsync(x => x.Quantity)
            );
            Assert.Equal(4m, (await db.Set<Pallet>().SingleAsync(x => x.Id == palletId)).Quantity);
            Assert.Single(await db.Set<FinishedPack>().ToListAsync());
        }
    }

    [Fact]
    public async Task AdditiveUpgradePreservesExistingStockAndManufacturingBaseline()
    {
        using var c = await factory.Login("admin@example.test");
        var a = await Setup(c, false);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
        var before = await db.Set<Pallet>().SumAsync(x => x.Quantity);
        var movements = await db.Set<StockMovement>().CountAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261017085223_MaterialPreparation");
        await migrator.MigrateAsync();
        db.ChangeTracker.Clear();
        Assert.Equal(before, await db.Set<Pallet>().SumAsync(x => x.Quantity));
        Assert.Equal(movements, await db.Set<StockMovement>().CountAsync());
        Assert.Equal("Released", (await db.Set<ProductionOrder>().SingleAsync()).Status);
        Assert.Equal(10m, (await db.Set<Consumption>().SingleAsync()).Quantity);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Empty(await db.Set<ProductionRun>().ToListAsync());
    }

    [Fact]
    public async Task PublishedLimitsStayWithSamplesAndEvidenceCannotBeRewritten()
    {
        using var c = await factory.Login("admin@example.test");
        var a = await Setup(c);
        var plantId = factory.PlantA;
        var resource = (await Read(c, M + "resources")).GetProperty("items")[0];
        await Post(
            c,
            M + "resources",
            new
            {
                plantId,
                code = "MIX-01",
                name = "Mixer",
                maintenance = true,
                version = resource.GetProperty("version").GetUInt32(),
                commandId = Guid.NewGuid(),
            }
        );
        await Bad(
            c,
            M + $"runs/{a.Run}/state",
            new
            {
                plantId,
                action = "Start",
                safetyConfirmed = true,
                notes = "",
                commandId = Guid.NewGuid(),
            },
            "maintenance"
        );
        resource = (await Read(c, M + "resources")).GetProperty("items")[0];
        await Post(
            c,
            M + "resources",
            new
            {
                plantId,
                code = "MIX-01",
                name = "Mixer",
                maintenance = false,
                version = resource.GetProperty("version").GetUInt32(),
                commandId = Guid.NewGuid(),
            }
        );
        var run = (await Read(c, M + $"runs/{a.Run}")).GetProperty("run");
        await Post(
            c,
            M + $"runs/{a.Run}/reschedule",
            new
            {
                plantId,
                productionResourceId = a.Resource,
                scheduledAt = DateTimeOffset.UtcNow.AddDays(1),
                priority = true,
                reason = "Shift plan changed",
                version = run.GetProperty("version").GetUInt32(),
                commandId = Guid.NewGuid(),
            }
        );
        await State(c, a.Run, "Start");
        var phase = await Phase(c, a.Run);
        await Post(c, M + $"phases/{Id(phase)}/start", Basic());
        await Evidence(c, a.Run, 0);
        var sample = await Sample(c, a, Id(phase), null);
        var revised = Result(
            await Post(
                c,
                M + "specifications",
                new
                {
                    plantId,
                    materialId = a.Finished,
                    name = "Revised laboratory limits",
                    tests = new[]
                    {
                        new
                        {
                            code = "PH",
                            name = "Acidity",
                            kind = "Numeric",
                            uom = "pH",
                            minimum = 100m,
                            maximum = 200m,
                        },
                    },
                    commandId = Guid.NewGuid(),
                }
            )
        );
        await Post(c, M + $"specifications/{revised}/publish", Basic());
        await TestSample(c, sample);
        var detail = await Read(c, M + $"samples/{sample}");
        Assert.Equal("Passed", detail.GetProperty("sample").GetProperty("status").GetString());
        using var snapshottedTests = JsonDocument.Parse(
            detail.GetProperty("sample").GetProperty("testsJson").GetString()!
        );
        Assert.Equal(6m, snapshottedTests.RootElement[0].GetProperty("minimum").GetDecimal());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
        var reading = await db.Set<QualityTestReading>().FirstAsync();
        reading.Notes = "Rewritten";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync(
                "UPDATE \"QualityTestReading\" SET \"Notes\"='Rewritten'"
            )
        );
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync("DELETE FROM \"ProductionEvent\"")
        );
        Assert.True(await db.Set<ProductionEvent>().AnyAsync(x => x.Kind == "Rescheduled"));
        Assert.DoesNotContain(
            await db.Set<QualityTestReading>().ToListAsync(),
            x => x.Notes == "Rewritten"
        );
    }
}
