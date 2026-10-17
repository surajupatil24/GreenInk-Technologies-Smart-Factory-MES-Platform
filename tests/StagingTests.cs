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

public sealed class StagingTests : IAsyncLifetime
{
    readonly TestFactory factory = new();
    const string Root = "/api/operations/";

    public Task InitializeAsync() => factory.InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)factory).DisposeAsync();

    static Guid Id(JsonElement e) => e.GetProperty("id").GetGuid();

    static Guid Result(JsonElement e) => e.GetProperty("resultId").GetGuid();

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

    Task<JsonElement> Read(HttpClient c, string path) =>
        c.GetFromJsonAsync<JsonElement>(Root + path + "?plantId=" + factory.PlantA);

    record Setup(
        Guid Raw,
        Guid Finished,
        Guid Source,
        Guid Batch,
        Guid Order,
        Guid Area,
        Guid Plan,
        Guid Warehouse,
        Guid Stage,
        Guid Supply,
        string SourceCode,
        uint RuleVersion,
        uint FactorVersion
    );

    async Task<Setup> SetupData(HttpClient c, string workflow = "Separate", bool factor = false)
    {
        var plantId = factory.PlantA;
        var raw = Id(
            await Post(
                c,
                "materials",
                new
                {
                    plantId,
                    code = "RAW",
                    name = "Staging resin",
                    uom = "kg",
                    kind = "Raw",
                }
            )
        );
        var finished = Id(
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
                    name = "Staging supplier",
                    kind = "Supplier",
                }
            )
        );
        var wh = Id(
            await Post(
                c,
                "bins",
                new
                {
                    plantId,
                    code = "WH-01",
                    name = "Warehouse bay",
                    warehouse = "Main",
                }
            )
        );
        var stage = Id(
            await Post(
                c,
                "bins",
                new
                {
                    plantId,
                    code = "STAGE-01",
                    name = "Preparation bay",
                    warehouse = "Main",
                }
            )
        );
        var supply = Id(
            await Post(
                c,
                "bins",
                new
                {
                    plantId,
                    code = "PSA-01",
                    name = "Mixer supply",
                    warehouse = "Main",
                }
            )
        );
        var po = Id(
            await Post(
                c,
                "purchase-orders",
                new
                {
                    plantId,
                    number = "PURCHASE",
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
                supplierBatch = "SOURCE-LOT",
                batchNumber = "RAW-BATCH",
                commandId = Guid.NewGuid(),
            }
        );
        var batch = receipt.GetProperty("batchId").GetGuid();
        var source = receipt.GetProperty("palletId").GetGuid();
        await Post(
            c,
            "quality-inspections",
            new
            {
                plantId,
                batchId = batch,
                decision = "Released",
                notes = "Validation release",
                commandId = Guid.NewGuid(),
            }
        );
        await Post(
            c,
            "transfers",
            new
            {
                plantId,
                palletId = source,
                location = "WH-01",
                commandId = Guid.NewGuid(),
            }
        );
        var order = Id(
            await Post(
                c,
                "production-orders",
                new
                {
                    plantId,
                    number = "ORDER-1",
                    materialId = finished,
                    plannedQuantity = 25m,
                    requirements = new[] { new { materialId = raw, quantity = 10m } },
                }
            )
        );
        await Post(
            c,
            $"production-orders/{order}/release",
            new { plantId, commandId = Guid.NewGuid() }
        );
        var area = Id(
            await Post(
                c,
                "staging/areas",
                new
                {
                    plantId,
                    code = "PREP",
                    name = "Resin preparation",
                    stagingBinId = stage,
                    supplyBinId = supply,
                    workflow,
                }
            )
        );
        await Post(
            c,
            "staging/rules",
            new
            {
                plantId,
                materialId = raw,
                packSize = 20m,
                preweighRequired = true,
                tolerancePercent = 1m,
                correctionRequired = factor,
                safetyNotes = "Wear gloves and eye protection",
                handlingNotes = "Keep the container sealed",
            }
        );
        if (factor)
            await Post(
                c,
                "staging/factors",
                new
                {
                    plantId,
                    batchId = batch,
                    factor = 1.1m,
                    evidence = "QC density record",
                }
            );
        var plan = Id(
            await Post(
                c,
                "staging/plans",
                new
                {
                    plantId,
                    productionOrderId = order,
                    stagingAreaId = area,
                    scheduledAt = DateTimeOffset.UtcNow.AddHours(1),
                    priority = true,
                    approvalRequired = false,
                    approved = false,
                }
            )
        );
        var inventory = await Read(c, "inventory");
        var sourceCode = inventory
            .EnumerateArray()
            .Single(x => Id(x.GetProperty("pallet")) == source)
            .GetProperty("pallet")
            .GetProperty("code")
            .GetString()!;
        var ruleVersion = (await Read(c, "staging/rules"))[0].GetProperty("version").GetUInt32();
        var factorVersion = factor
            ? (await Read(c, "staging/factors"))[0].GetProperty("version").GetUInt32()
            : 0u;
        return new(
            raw,
            finished,
            source,
            batch,
            order,
            area,
            plan,
            wh,
            stage,
            supply,
            sourceCode,
            ruleVersion,
            factorVersion
        );
    }

    [Fact]
    public async Task SeparatePickingWeighingAndIssueConservePhysicalStockAndCreditTheBom()
    {
        using var c = await factory.Login("admin@example.test");
        var a = await SetupData(c, factor: true);
        var plantId = factory.PlantA;
        var path = $"staging/plans/{a.Plan}/pick";
        var input = new
        {
            plantId,
            palletId = a.Source,
            palletCode = a.SourceCode,
            ruleVersion = a.RuleVersion,
            factorVersion = a.FactorVersion,
            sourceBin = "WH-01",
            quantity = 20m,
            requirementQuantity = 10m,
            gross = (decimal?)null,
            tare = (decimal?)null,
            scaleReference = (string?)null,
            safetyConfirmed = true,
            commandId = Guid.NewGuid(),
        };
        await Bad(c, path, input with { safetyConfirmed = false }, "safety");
        await Bad(c, path, input with { sourceBin = "WRONG" }, "source bin");
        await Bad(c, path, input with { palletCode = "WRONG-LABEL" }, "material label");
        await Bad(c, path, input with { ruleVersion = 0u }, "rules changed");
        await Bad(c, path, input with { factorVersion = 0u }, "factor changed");
        var picked = await Post(c, path, input);
        var pickId = Result(picked);
        Assert.Equal(pickId, Result(await Post(c, path, input)));
        await Bad(c, path, input with { quantity = 19m }, "different input");
        await Bad(
            c,
            "reservations",
            new
            {
                plantId,
                productionOrderId = a.Order,
                palletId = a.Source,
                quantity = 1m,
                commandId = Guid.NewGuid(),
            },
            "guided picking"
        );
        var detail = await Read(c, $"staging/plans/{a.Plan}");
        var res = Id(detail.GetProperty("reservations")[0]);
        var pickedPallet = detail.GetProperty("picks")[0].GetProperty("currentPalletId").GetGuid();
        await Bad(
            c,
            $"reservations/{res}/issue",
            new { plantId, commandId = Guid.NewGuid() },
            "Deliver"
        );
        await Bad(
            c,
            $"reservations/{res}/cancel",
            new { plantId, commandId = Guid.NewGuid() },
            "guided return"
        );
        await Bad(
            c,
            "transfers",
            new
            {
                plantId,
                palletId = pickedPallet,
                location = "PSA-01",
                commandId = Guid.NewGuid(),
            },
            "guided staging"
        );
        // Rules and factors are snapshots for active picks.
        await Post(
            c,
            "staging/factors",
            new
            {
                plantId,
                batchId = a.Batch,
                factor = 2m,
                evidence = "New QC result for future picks",
            }
        );
        await Post(
            c,
            "staging/rules",
            new
            {
                plantId,
                materialId = a.Raw,
                packSize = 20m,
                preweighRequired = true,
                tolerancePercent = 100m,
                correctionRequired = true,
                safetyNotes = "New instructions",
                handlingNotes = "New handling",
            }
        );
        await Bad(
            c,
            $"staging/picks/{pickId}/weigh",
            new
            {
                plantId,
                gross = 12.5m,
                tare = 1m,
                commandId = Guid.NewGuid(),
            },
            "outside"
        );
        var weighing = new
        {
            plantId,
            gross = 12.055m,
            tare = 1m,
            scaleReference = "Bench scale A / manual",
            commandId = Guid.NewGuid(),
        };
        var weighed = await Post(c, $"staging/picks/{pickId}/weigh", weighing);
        Assert.Equal(Id(weighed), Id(await Post(c, $"staging/picks/{pickId}/weigh", weighing)));
        detail = await Read(c, $"staging/plans/{a.Plan}");
        Assert.Equal(
            11.055m,
            detail.GetProperty("measurements")[0].GetProperty("net").GetDecimal()
        );
        Assert.Equal(
            1.1m,
            detail.GetProperty("picks")[0].GetProperty("correctionFactor").GetDecimal()
        );
        Assert.Equal(10m, detail.GetProperty("progress")[0].GetProperty("ready").GetDecimal());
        var kit = detail.GetProperty("plan").GetProperty("kitCode").GetString();
        await Bad(
            c,
            $"staging/plans/{a.Plan}/putaway",
            new
            {
                plantId,
                kitCode = kit,
                destinationBin = "WH-01",
                commandId = Guid.NewGuid(),
            },
            "supply bin"
        );
        var delivery = new
        {
            plantId,
            kitCode = kit,
            destinationBin = "PSA-01",
            commandId = Guid.NewGuid(),
        };
        await Post(c, $"staging/plans/{a.Plan}/putaway", delivery);
        await Post(c, $"staging/plans/{a.Plan}/putaway", delivery);
        using var qr = await c.GetAsync(
            Root + $"staging/plans/{a.Plan}/label.svg?plantId={plantId}"
        );
        Assert.Contains("<svg", await qr.Content.ReadAsStringAsync());
        var issueCommand = Guid.NewGuid();
        await Post(c, $"reservations/{res}/issue", new { plantId, commandId = issueCommand });
        await Post(c, $"reservations/{res}/issue", new { plantId, commandId = issueCommand });
        var readiness = await Read(c, $"production-orders/{a.Order}/requirements");
        Assert.True(readiness.GetProperty("canComplete").GetBoolean());
        Assert.Equal(
            10m,
            readiness.GetProperty("requirements")[0].GetProperty("issuedQuantity").GetDecimal()
        );
        await Bad(
            c,
            $"staging/picks/{pickId}/return",
            new
            {
                plantId,
                destinationBin = "WH-01",
                reason = "Too late",
                commandId = Guid.NewGuid(),
            },
            "Issued"
        );
        await Post(
            c,
            $"production-orders/{a.Order}/complete",
            new
            {
                plantId,
                quantity = 25m,
                scrapQuantity = 0m,
                location = "PSA-01",
                commandId = Guid.NewGuid(),
            }
        );
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
        var consumption = await db.Set<Consumption>().SingleAsync();
        Assert.Equal(11.055m, consumption.Quantity);
        Assert.Equal(10m, consumption.RequirementQuantity);
        var rawStock = await db.Set<Pallet>()
            .Where(p => p.BatchId == a.Batch)
            .SumAsync(p => p.Quantity);
        Assert.Equal(88.945m, rawStock);
        Assert.Equal(
            0m,
            await db.Set<Pallet>()
                .Where(p => p.BatchId == a.Batch)
                .SumAsync(p => p.ReservedQuantity)
        );
        Assert.Equal(
            rawStock,
            await db.Set<StockMovement>()
                .Where(m => db.Set<Pallet>().Any(p => p.Id == m.PalletId && p.BatchId == a.Batch))
                .SumAsync(m => m.Quantity)
        );
    }

    [Fact]
    public async Task TogetherPickingRequiresApprovalAndCompetingPicksCannotOverallocate()
    {
        using var c = await factory.Login("admin@example.test");
        using var viewer = await factory.Login("viewer@example.test");
        var a = await SetupData(c, "Together");
        var plantId = factory.PlantA;
        var planning = new
        {
            plantId,
            productionOrderId = a.Order,
            stagingAreaId = a.Area,
            scheduledAt = DateTimeOffset.UtcNow,
            priority = false,
            approvalRequired = true,
            approved = false,
            approvalNotes = "",
        };
        await Post(c, "staging/plans", planning);
        var input = new
        {
            plantId,
            palletId = a.Source,
            palletCode = a.SourceCode,
            ruleVersion = a.RuleVersion,
            factorVersion = a.FactorVersion,
            sourceBin = "WH-01",
            quantity = 10m,
            requirementQuantity = 10m,
            gross = 11m,
            tare = 1m,
            scaleReference = "Manual",
            safetyConfirmed = true,
            commandId = Guid.NewGuid(),
        };
        var path = $"staging/plans/{a.Plan}/pick";
        await Bad(c, path, input, "approval");
        await Bad(
            c,
            $"production-orders/{a.Order}/complete",
            new
            {
                plantId,
                quantity = 25m,
                scrapQuantity = 0m,
                location = "PSA-01",
                commandId = Guid.NewGuid(),
            },
            "Engineering approval"
        );
        await Post(
            c,
            "staging/plans",
            planning with
            {
                approved = true,
                approvalNotes = "Engineering record E-01",
            }
        );
        await Bad(c, path, input with { quantity = 9m }, "gross minus tare");
        await Bad(c, path, input with { quantity = 11m, gross = 12m }, "outside");
        var races = await Task.WhenAll(
            c.PostAsJsonAsync(Root + path, input),
            c.PostAsJsonAsync(Root + path, input with { commandId = Guid.NewGuid() })
        );
        try
        {
            Assert.Single(races, r => r.IsSuccessStatusCode);
            Assert.All(
                races.Where(r => !r.IsSuccessStatusCode),
                r =>
                    Assert.True(
                        r.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict
                    )
            );
        }
        finally
        {
            foreach (var r in races)
                r.Dispose();
        }
        var detail = await Read(c, $"staging/plans/{a.Plan}");
        Assert.Single(detail.GetProperty("picks").EnumerateArray());
        Assert.Equal("Supply", detail.GetProperty("picks")[0].GetProperty("status").GetString());
        Assert.Equal(0m, detail.GetProperty("progress")[0].GetProperty("remaining").GetDecimal());
        await Bad(c, "staging/plans", planning with { approved = false }, "Approval settings");
        using var denied = await viewer.PostAsJsonAsync(
            Root + path,
            input with
            {
                commandId = Guid.NewGuid(),
            }
        );
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (
                await viewer.GetAsync(Root + $"staging/plans/{a.Plan}?plantId={factory.PlantB}")
            ).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.NotFound,
            (
                await c.GetAsync(Root + $"staging/plans/{a.Plan}?plantId={factory.PlantAOther}")
            ).StatusCode
        );
        var orders = await Read(c, "staging/orders");
        Assert.Single(orders.GetProperty("items").EnumerateArray());
        Assert.Equal(1, orders.GetProperty("items")[0].GetProperty("supplied").GetInt32());
    }

    [Fact]
    public async Task ReplenishmentPartialDeliveryCancellationAndKanbanUseActualBalances()
    {
        using var c = await factory.Login("admin@example.test");
        var a = await SetupData(c);
        var plantId = factory.PlantA;
        await Post(
            c,
            "staging/kanban",
            new
            {
                plantId,
                binId = a.Stage,
                materialId = a.Raw,
                mode = "Static",
                lowStock = 10m,
                reorderStock = 20m,
                maxStock = 40m,
            }
        );
        var dashboard = await Read(c, "staging/kanban");
        Assert.Equal("Urgent", dashboard[0].GetProperty("status").GetString());
        Assert.Equal(40m, dashboard[0].GetProperty("requestQuantity").GetDecimal());
        var request = new
        {
            plantId,
            materialId = a.Raw,
            destinationBinId = a.Stage,
            stagingPlanId = (Guid?)a.Plan,
            quantity = 40m,
            kind = "Kanban",
            reason = "Fill buffer",
            commandId = Guid.NewGuid(),
        };
        var req = Result(await Post(c, "staging/requests", request));
        Assert.Equal(req, Result(await Post(c, "staging/requests", request)));
        dashboard = await Read(c, "staging/kanban");
        Assert.Equal(0m, dashboard[0].GetProperty("requestQuantity").GetDecimal());
        var delivery = new
        {
            plantId,
            palletId = a.Source,
            sourceBin = "WH-01",
            quantity = 10m,
            commandId = Guid.NewGuid(),
        };
        await Post(c, $"staging/requests/{req}/fulfil", delivery);
        await Post(c, $"staging/requests/{req}/fulfil", delivery);
        await Bad(
            c,
            $"staging/requests/{req}/cancel",
            new
            {
                plantId,
                reason = "Already started",
                commandId = Guid.NewGuid(),
            },
            "unstarted"
        );
        await Bad(
            c,
            $"staging/requests/{req}/fulfil",
            delivery with
            {
                quantity = 31m,
                commandId = Guid.NewGuid(),
            },
            "outstanding"
        );
        dashboard = await Read(c, "staging/kanban");
        Assert.Equal(10m, dashboard[0].GetProperty("available").GetDecimal());
        Assert.Equal(30m, dashboard[0].GetProperty("openQuantity").GetDecimal());
        await Post(
            c,
            $"staging/requests/{req}/fulfil",
            delivery with
            {
                quantity = 30m,
                commandId = Guid.NewGuid(),
            }
        );
        dashboard = await Read(c, "staging/kanban");
        Assert.Equal("Healthy", dashboard[0].GetProperty("status").GetString());
        Assert.Equal(40m, dashboard[0].GetProperty("available").GetDecimal());
        await Bad(
            c,
            "staging/transfers",
            new
            {
                plantId,
                palletId = a.Source,
                sourceBin = "WH-01",
                destinationBin = "STAGE-01",
                quantity = 1m,
                reason = "Capacity test",
                commandId = Guid.NewGuid(),
            },
            "maximum"
        );
        await Bad(
            c,
            "transfers",
            new
            {
                plantId,
                palletId = a.Source,
                location = "STAGE-01",
                commandId = Guid.NewGuid(),
            },
            "maximum"
        );
        var another = Result(
            await Post(
                c,
                "staging/requests",
                request with
                {
                    quantity = 2m,
                    commandId = Guid.NewGuid(),
                }
            )
        );
        await Post(
            c,
            $"staging/requests/{another}/cancel",
            new
            {
                plantId,
                reason = "Not required",
                commandId = Guid.NewGuid(),
            }
        );
        await Bad(
            c,
            $"staging/requests/{another}/fulfil",
            delivery with
            {
                quantity = 2m,
                commandId = Guid.NewGuid(),
            },
            "cancelled"
        );
        await Post(
            c,
            "staging/kanban",
            new
            {
                plantId,
                binId = a.Supply,
                materialId = a.Raw,
                mode = "Demand",
                lowStock = 0m,
                reorderStock = 5m,
                maxStock = 50m,
            }
        );
        dashboard = await Read(c, "staging/kanban");
        var demand = dashboard
            .EnumerateArray()
            .Single(x => x.GetProperty("policy").GetProperty("mode").GetString() == "Demand");
        Assert.Equal(10m, demand.GetProperty("demand").GetDecimal());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
        Assert.Equal(100m, await db.Set<Pallet>().SumAsync(x => x.Quantity));
        Assert.Equal(0m, await db.Set<Pallet>().SumAsync(x => x.ReservedQuantity));
    }

    [Fact]
    public async Task ReturnsAndReviewedDiscrepanciesNeverSilentlyOverwriteStock()
    {
        using var c = await factory.Login("admin@example.test");
        var a = await SetupData(c, "PickOnly");
        var plantId = factory.PlantA;
        var picked = Result(
            await Post(
                c,
                $"staging/plans/{a.Plan}/pick",
                new
                {
                    plantId,
                    palletId = a.Source,
                    palletCode = a.SourceCode,
                    ruleVersion = a.RuleVersion,
                    factorVersion = a.FactorVersion,
                    sourceBin = "WH-01",
                    quantity = 10m,
                    requirementQuantity = 10m,
                    gross = 10m,
                    tare = 0m,
                    safetyConfirmed = true,
                    commandId = Guid.NewGuid(),
                }
            )
        );
        var back = new
        {
            plantId,
            destinationBin = "WH-01",
            reason = "Plan rescheduled",
            commandId = Guid.NewGuid(),
        };
        var returned = await Post(c, $"staging/picks/{picked}/return", back);
        Assert.Equal(Id(returned), Id(await Post(c, $"staging/picks/{picked}/return", back)));
        var detail = await Read(c, $"staging/plans/{a.Plan}");
        Assert.Equal(10m, detail.GetProperty("progress")[0].GetProperty("remaining").GetDecimal());
        var gain = new
        {
            plantId,
            palletId = a.Source,
            kind = "Gain",
            notes = "Verified extra stock",
            countedQuantity = 91m,
            commandId = Guid.NewGuid(),
        };
        var report = Result(await Post(c, "staging/stock-issues", gain));
        detail = await Read(c, $"staging/plans/{a.Plan}");
        Assert.DoesNotContain(
            detail.GetProperty("sources").EnumerateArray(),
            x => Id(x) == a.Source
        );
        var resolve = new
        {
            plantId,
            applyCount = true,
            resolution = "Supervisor recount matches",
            commandId = Guid.NewGuid(),
        };
        await Post(c, $"staging/stock-issues/{report}/resolve", resolve);
        await Post(c, $"staging/stock-issues/{report}/resolve", resolve);
        var loss = Result(
            await Post(
                c,
                "staging/stock-issues",
                gain with
                {
                    kind = "Loss",
                    countedQuantity = 89m,
                    commandId = Guid.NewGuid(),
                }
            )
        );
        await Post(
            c,
            "adjustments",
            new
            {
                plantId,
                palletId = a.Source,
                quantity = 1m,
                reason = "Later audited correction",
                commandId = Guid.NewGuid(),
            }
        );
        await Bad(
            c,
            $"staging/stock-issues/{loss}/resolve",
            resolve with
            {
                commandId = Guid.NewGuid(),
            },
            "Stock changed"
        );
        await Post(
            c,
            $"staging/stock-issues/{loss}/resolve",
            resolve with
            {
                applyCount = false,
                commandId = Guid.NewGuid(),
            }
        );
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
        Assert.Equal(102m, await db.Set<Pallet>().SumAsync(x => x.Quantity));
        Assert.Equal(0m, await db.Set<Pallet>().SumAsync(x => x.ReservedQuantity));
        Assert.Equal(1, await db.Set<StockMovement>().CountAsync(x => x.Kind == "StagingGain"));
    }

    [Fact]
    public async Task MultipleBatchFactorsReconcileNominalCreditsAndPhysicalConsumption()
    {
        using var c = await factory.Login("admin@example.test");
        var a = await SetupData(c, "Together", true);
        var plantId = factory.PlantA;
        var partners = await Read(c, "partners");
        var supplier = Id(partners[0]);
        var po = Id(
            await Post(
                c,
                "purchase-orders",
                new
                {
                    plantId,
                    number = "SECOND-PO",
                    supplierId = supplier,
                    materialId = a.Raw,
                    quantity = 100m,
                }
            )
        );
        var rec = await Post(
            c,
            "receipts",
            new
            {
                plantId,
                purchaseOrderId = po,
                quantity = 100m,
                supplierBatch = "SECOND",
                batchNumber = "SECOND",
                commandId = Guid.NewGuid(),
            }
        );
        var batch = rec.GetProperty("batchId").GetGuid();
        var pallet = rec.GetProperty("palletId").GetGuid();
        await Post(
            c,
            "quality-inspections",
            new
            {
                plantId,
                batchId = batch,
                decision = "Released",
                notes = "Second batch release",
                commandId = Guid.NewGuid(),
            }
        );
        await Bad(
            c,
            $"staging/plans/{a.Plan}/pick",
            new
            {
                plantId,
                palletId = pallet,
                palletCode = "WRONG",
                sourceBin = "RECEIVING",
                quantity = 12m,
                requirementQuantity = 6m,
                gross = 12m,
                tare = 0m,
                safetyConfirmed = true,
                commandId = Guid.NewGuid(),
            },
            "material label"
        );
        await Post(
            c,
            "staging/factors",
            new
            {
                plantId,
                batchId = batch,
                factor = 2m,
                evidence = "Second batch density",
            }
        );
        var inventory = await Read(c, "inventory");
        var code = inventory
            .EnumerateArray()
            .Single(x => Id(x.GetProperty("pallet")) == pallet)
            .GetProperty("pallet")
            .GetProperty("code")
            .GetString();
        await Post(
            c,
            $"staging/plans/{a.Plan}/pick",
            new
            {
                plantId,
                palletId = a.Source,
                palletCode = a.SourceCode,
                ruleVersion = a.RuleVersion,
                factorVersion = a.FactorVersion,
                sourceBin = "WH-01",
                quantity = 4.4m,
                requirementQuantity = 4m,
                gross = 4.4m,
                tare = 0m,
                safetyConfirmed = true,
                commandId = Guid.NewGuid(),
            }
        );
        await Post(
            c,
            $"staging/plans/{a.Plan}/pick",
            new
            {
                plantId,
                palletId = pallet,
                palletCode = code,
                ruleVersion = a.RuleVersion,
                factorVersion = (await Read(c, "staging/factors"))
                    .EnumerateArray()
                    .Single(x => x.GetProperty("batchId").GetGuid() == batch)
                    .GetProperty("version")
                    .GetUInt32(),
                sourceBin = "RECEIVING",
                quantity = 12m,
                requirementQuantity = 6m,
                gross = 12m,
                tare = 0m,
                safetyConfirmed = true,
                commandId = Guid.NewGuid(),
            }
        );
        var detail = await Read(c, $"staging/plans/{a.Plan}");
        foreach (var res in detail.GetProperty("reservations").EnumerateArray())
            await Post(
                c,
                $"reservations/{Id(res)}/issue",
                new { plantId, commandId = Guid.NewGuid() }
            );
        var readiness = await Read(c, $"production-orders/{a.Order}/requirements");
        Assert.True(readiness.GetProperty("canComplete").GetBoolean());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
        Assert.Equal(16.4m, await db.Set<Consumption>().SumAsync(x => x.Quantity));
        Assert.Equal(
            10m,
            await db.Set<Consumption>().SumAsync(x => x.RequirementQuantity ?? x.Quantity)
        );
        Assert.Equal(183.6m, await db.Set<Pallet>().SumAsync(x => x.Quantity));
    }

    [Fact]
    public async Task CancellationAndDeliveryRaceHasOneConsistentOutcome()
    {
        using var c = await factory.Login("admin@example.test");
        var a = await SetupData(c);
        var plantId = factory.PlantA;
        var id = Result(
            await Post(
                c,
                "staging/requests",
                new
                {
                    plantId,
                    materialId = a.Raw,
                    destinationBinId = a.Stage,
                    quantity = 5m,
                    kind = "RMShortage",
                    reason = "Concurrent request",
                    commandId = Guid.NewGuid(),
                }
            )
        );
        var responses = await Task.WhenAll(
            c.PostAsJsonAsync(
                Root + $"staging/requests/{id}/cancel",
                new
                {
                    plantId,
                    reason = "Schedule changed",
                    commandId = Guid.NewGuid(),
                }
            ),
            c.PostAsJsonAsync(
                Root + $"staging/requests/{id}/fulfil",
                new
                {
                    plantId,
                    palletId = a.Source,
                    sourceBin = "WH-01",
                    quantity = 5m,
                    commandId = Guid.NewGuid(),
                }
            )
        );
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
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
        var request = await db.Set<ReplenishmentRequest>().SingleAsync();
        Assert.True(request.Status is "Cancelled" or "Completed");
        Assert.Equal(request.Status == "Completed" ? 5m : 0m, request.FulfilledQuantity);
        Assert.Equal(100m, await db.Set<Pallet>().SumAsync(x => x.Quantity));
        Assert.Equal(
            request.Status == "Completed" ? 1 : 0,
            await db.Set<StockMovement>().CountAsync(x => x.Kind == "ReplenishmentIn")
        );
    }

    [Fact]
    public async Task UpgradePreservesExistingQuickReservationsAndConsumption()
    {
        using var c = await factory.Login("admin@example.test");
        var a = await SetupData(c);
        var plantId = factory.PlantA;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            db.Remove(await db.Set<StagingPlan>().SingleAsync());
            await db.SaveChangesAsync();
        }
        var reserved = await Post(
            c,
            "reservations",
            new
            {
                plantId,
                productionOrderId = a.Order,
                palletId = a.Source,
                quantity = 5m,
                commandId = Guid.NewGuid(),
            }
        );
        await Post(
            c,
            $"reservations/{Id(reserved)}/issue",
            new { plantId, commandId = Guid.NewGuid() }
        );
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261016211846_ReceivingGate");
            await migrator.MigrateAsync();
            db.ChangeTracker.Clear();
            var res = await db.Set<Reservation>().SingleAsync();
            var used = await db.Set<Consumption>().SingleAsync();
            Assert.Equal(5m, res.Quantity);
            Assert.Null(res.RequirementQuantity);
            Assert.Null(res.StagingPickId);
            Assert.Equal(5m, used.Quantity);
            Assert.Null(used.RequirementQuantity);
            Assert.Equal(95m, await db.Set<Pallet>().SumAsync(x => x.Quantity));
            Assert.Equal(0m, await db.Set<Pallet>().SumAsync(x => x.ReservedQuantity));
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        }
        var readiness = await Read(c, $"production-orders/{a.Order}/requirements");
        Assert.Equal(
            5m,
            readiness.GetProperty("requirements")[0].GetProperty("issuedQuantity").GetDecimal()
        );
    }
}
