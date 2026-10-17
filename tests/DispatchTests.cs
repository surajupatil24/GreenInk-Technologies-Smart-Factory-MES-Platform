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

public sealed class DispatchTests : IAsyncLifetime
{
    readonly TestFactory factory = new();
    const string Root = "/api/operations/",
        D = "dispatch/";

    public Task InitializeAsync() => factory.InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)factory).DisposeAsync();

    static Guid Id(JsonElement v) => v.GetProperty("id").GetGuid();

    static Guid Result(JsonElement v) => v.GetProperty("resultId").GetGuid();

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

    object Basic() => new { plantId = factory.PlantA, commandId = Guid.NewGuid() };

    record Setup(
        Guid MaterialA,
        Guid MaterialB,
        Guid SourceA,
        Guid SourceB,
        Guid BatchA,
        Guid Area,
        Guid Package,
        Guid OrderA,
        Guid OrderB,
        Guid Run,
        Guid Admin
    );

    async Task<Setup> Data(HttpClient c, bool overpick = false)
    {
        var plantId = factory.PlantA;
        var a = Id(
            await Post(
                c,
                "materials",
                new
                {
                    plantId,
                    code = "FG-A",
                    name = "Blend",
                    kind = "Finished",
                    uom = "kg",
                }
            )
        );
        var b = Id(
            await Post(
                c,
                "materials",
                new
                {
                    plantId,
                    code = "FG-B",
                    name = "Parts",
                    kind = "Finished",
                    uom = "pcs",
                }
            )
        );
        var customer = Id(
            await Post(
                c,
                "partners",
                new
                {
                    plantId,
                    code = "CUST",
                    name = "Test customer",
                    kind = "Customer",
                }
            )
        );
        var fg = Id(
            await Post(
                c,
                "bins",
                new
                {
                    plantId,
                    code = "FG",
                    name = "Finished rack",
                    warehouse = "MAIN",
                }
            )
        );
        var pack = Id(
            await Post(
                c,
                "bins",
                new
                {
                    plantId,
                    code = "PACK",
                    name = "Packing bay",
                    warehouse = "MAIN",
                }
            )
        );
        var load = Id(
            await Post(
                c,
                "bins",
                new
                {
                    plantId,
                    code = "LOAD",
                    name = "Truck gate",
                    warehouse = "MAIN",
                }
            )
        );
        await Post(
            c,
            D + "instructions",
            new
            {
                plantId,
                customerId = customer,
                picking = "Check labels",
                packing = "Protect seals",
                shipping = "Upright loading",
                safety = "Gloves and goggles",
                minimumShelfDays = 10,
                productPacking = false,
                commandId = Guid.NewGuid(),
            }
        );
        var area = Result(
            await Post(
                c,
                D + "areas",
                new
                {
                    plantId,
                    code = "AREA",
                    name = "Dispatch",
                    shippingPoint = "SHIP",
                    packBinId = pack,
                    loadingBinId = load,
                    pickMode = "Group",
                    packMode = "Collective",
                    allowOverpick = overpick,
                    commandId = Guid.NewGuid(),
                }
            )
        );
        var package = Result(
            await Post(
                c,
                D + "packages",
                new
                {
                    plantId,
                    code = "BOX",
                    name = "Test container",
                    lengthCm = 60m,
                    widthCm = 50m,
                    heightCm = 40m,
                    tareKg = 1m,
                    maximumGrossKg = 100m,
                    temperatureDeviceRequired = true,
                    commandId = Guid.NewGuid(),
                }
            )
        );
        Guid sourceA,
            sourceB,
            batchA,
            admin;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            admin = (await db.Users.SingleAsync(x => x.Email == "admin@example.test")).Id;
            var ba = new Batch
            {
                TenantId = factory.TenantA,
                PlantId = plantId,
                MaterialId = a,
                Number = "LOT-A",
                Kind = "Finished",
                QualityStatus = "Released",
                ExpiryDate = DateTimeOffset.UtcNow.AddDays(365),
            };
            var bb = new Batch
            {
                TenantId = factory.TenantA,
                PlantId = plantId,
                MaterialId = b,
                Number = "LOT-B",
                Kind = "Finished",
                QualityStatus = "Released",
                ExpiryDate = DateTimeOffset.UtcNow.AddDays(365),
            };
            var pa = new Pallet
            {
                TenantId = factory.TenantA,
                PlantId = plantId,
                BatchId = ba.Id,
                Code = "PAL-A",
                Location = "FG",
                Quantity = 100,
            };
            var pb = new Pallet
            {
                TenantId = factory.TenantA,
                PlantId = plantId,
                BatchId = bb.Id,
                Code = "PAL-B",
                Location = "FG",
                Quantity = 50,
            };
            db.AddRange(ba, bb, pa, pb);
            foreach (var p in new[] { pa, pb })
                db.Add(
                    new StockMovement
                    {
                        TenantId = factory.TenantA,
                        PlantId = plantId,
                        PalletId = p.Id,
                        Quantity = p.Quantity,
                        Kind = "Opening",
                        ToLocation = "FG",
                        CommandId = Guid.NewGuid(),
                    }
                );
            await db.SaveChangesAsync();
            sourceA = pa.Id;
            sourceB = pb.Id;
            batchA = ba.Id;
        }
        var oa = Id(
            await Post(
                c,
                "delivery-orders",
                new
                {
                    plantId,
                    number = "DO-A",
                    customerId = customer,
                    materialId = a,
                    quantity = 10m,
                }
            )
        );
        var ob = Id(
            await Post(
                c,
                "delivery-orders",
                new
                {
                    plantId,
                    number = "DO-B",
                    customerId = customer,
                    materialId = b,
                    quantity = 5m,
                }
            )
        );
        var run = Result(
            await Post(
                c,
                D + "runs",
                new
                {
                    plantId,
                    number = "SHIP-1",
                    dispatchAreaId = area,
                    kind = "Customer",
                    dispatchWarehouseId = (Guid?)null,
                    assignedToId = admin,
                    scheduledAt = DateTimeOffset.UtcNow,
                    priority = true,
                    route = "City route",
                    deliveryOrderIds = new[] { oa, ob },
                    shuttleLines = Array.Empty<object>(),
                    commandId = Guid.NewGuid(),
                }
            )
        );
        return new(a, b, sourceA, sourceB, batchA, area, package, oa, ob, run, admin);
    }

    async Task State(HttpClient c, Guid run, string action) =>
        await Post(
            c,
            D + $"runs/{run}/state",
            new
            {
                plantId = factory.PlantA,
                action,
                confirmed = true,
                notes = "Operator confirmation",
                commandId = Guid.NewGuid(),
            }
        );

    async Task Picks(HttpClient c, Setup a, decimal quantity = 10m)
    {
        var lines = (await Read(c, D + $"runs/{a.Run}")).GetProperty("lines");
        var la = lines
            .EnumerateArray()
            .Single(x => x.GetProperty("materialId").GetGuid() == a.MaterialA);
        var lb = lines
            .EnumerateArray()
            .Single(x => x.GetProperty("materialId").GetGuid() == a.MaterialB);
        await Post(
            c,
            D + $"runs/{a.Run}/picks",
            new
            {
                plantId = factory.PlantA,
                lines = new[]
                {
                    new
                    {
                        dispatchRunLineId = Id(la),
                        palletId = a.SourceA,
                        palletCode = "PAL-A",
                        sourceBin = "FG",
                        quantity,
                    },
                    new
                    {
                        dispatchRunLineId = Id(lb),
                        palletId = a.SourceB,
                        palletCode = "PAL-B",
                        sourceBin = "FG",
                        quantity = 5m,
                    },
                },
                instructionsConfirmed = true,
                safetyConfirmed = true,
                commandId = Guid.NewGuid(),
            }
        );
    }

    async Task<Guid> PackAndLoad(HttpClient c, Setup a)
    {
        var plantId = factory.PlantA;
        await State(c, a.Run, "CompletePicking");
        await State(c, a.Run, "StartPacking");
        await Bad(
            c,
            D + $"runs/{a.Run}/units",
            new
            {
                plantId,
                dispatchPackageId = a.Package,
                temperatureDevice = "",
                commandId = Guid.NewGuid(),
            },
            "nonempty"
        );
        var unit = Result(
            await Post(
                c,
                D + $"runs/{a.Run}/units",
                new
                {
                    plantId,
                    dispatchPackageId = a.Package,
                    temperatureDevice = "TEMP-001",
                    commandId = Guid.NewGuid(),
                }
            )
        );
        var detail = await Read(c, D + $"runs/{a.Run}");
        var picks = detail
            .GetProperty("picks")
            .EnumerateArray()
            .Where(x => x.GetProperty("quantity").GetDecimal() > 0)
            .ToArray();
        var pallets = detail.GetProperty("pallets").EnumerateArray().ToArray();
        var codes = picks
            .Select(p =>
                pallets
                    .Single(x => Id(x) == p.GetProperty("pickedPalletId").GetGuid())
                    .GetProperty("code")
                    .GetString()
            )
            .ToArray();
        await Post(
            c,
            D + $"units/{unit}/pack",
            new
            {
                plantId,
                pickIds = picks.Select(Id).ToArray(),
                palletCodes = codes,
                instructionsConfirmed = true,
                commandId = Guid.NewGuid(),
            }
        );
        var unitCode = (await Read(c, D + $"runs/{a.Run}"))
            .GetProperty("units")[0]
            .GetProperty("code")
            .GetString();
        await Bad(
            c,
            D + $"units/{unit}/state",
            new
            {
                plantId,
                action = "Close",
                unitCode,
                grossKg = 5m,
                notes = "",
                commandId = Guid.NewGuid(),
            },
            "net kilograms"
        );
        await Post(
            c,
            D + $"units/{unit}/state",
            new
            {
                plantId,
                action = "Close",
                unitCode,
                grossKg = 20m,
                notes = "Checked",
                commandId = Guid.NewGuid(),
            }
        );
        await State(c, a.Run, "CompletePacking");
        await Post(
            c,
            D + $"runs/{a.Run}/truck",
            new
            {
                plantId,
                truck = "MH12-AB-2048",
                carrier = "Local transport",
                instructionsConfirmed = true,
                commandId = Guid.NewGuid(),
            }
        );
        await Bad(c, D + $"runs/{a.Run}/loading-complete", Basic(), "every active handling unit");
        await Post(
            c,
            D + $"units/{unit}/state",
            new
            {
                plantId,
                action = "Load",
                unitCode,
                grossKg = (decimal?)null,
                notes = "Scanned at loading",
                commandId = Guid.NewGuid(),
            }
        );
        await Post(c, D + $"runs/{a.Run}/loading-complete", Basic());
        return unit;
    }

    [Fact]
    public async Task GroupPickPackLoadIssueAndQuarantinedReversalPreserveStockAndReports()
    {
        using var c = await factory.Login("admin@example.test");
        var a = await Data(c, true);
        var plantId = factory.PlantA;
        await Bad(
            c,
            D + $"runs/{a.Run}/goods-issue",
            new
            {
                plantId,
                action = "Issue",
                confirmed = true,
                notes = "",
                commandId = Guid.NewGuid(),
            },
            "loading completion"
        );
        await State(c, a.Run, "StartPicking");
        await Picks(c, a, 12m);
        await Bad(
            c,
            D + $"runs/{a.Run}/state",
            new
            {
                plantId,
                action = "CompletePicking",
                confirmed = true,
                notes = "",
                commandId = Guid.NewGuid(),
            },
            "Return any excess"
        );
        var detail = await Read(c, D + $"runs/{a.Run}");
        var pick = detail
            .GetProperty("picks")
            .EnumerateArray()
            .Single(x => x.GetProperty("sourcePalletId").GetGuid() == a.SourceA);
        var pallet = detail
            .GetProperty("pallets")
            .EnumerateArray()
            .Single(x => Id(x) == pick.GetProperty("pickedPalletId").GetGuid());
        await Post(
            c,
            D + $"picks/{Id(pick)}/return",
            new
            {
                plantId,
                quantity = 2m,
                palletCode = pallet.GetProperty("code").GetString(),
                destinationBin = "FG",
                reason = "Return overpick",
                commandId = Guid.NewGuid(),
            }
        );
        await Bad(
            c,
            "shipments",
            new
            {
                plantId,
                deliveryOrderId = a.OrderA,
                palletId = a.SourceA,
                quantity = 10m,
                truck = "BYPASS",
                commandId = Guid.NewGuid(),
            },
            "guided dispatch"
        );
        await Bad(
            c,
            "transfers",
            new
            {
                plantId,
                palletId = Id(pallet),
                location = "FG",
                commandId = Guid.NewGuid(),
            },
            "allocated to dispatch"
        );
        var unit = await PackAndLoad(c, a);
        var label = await Read(c, D + $"units/{unit}/label");
        Assert.Contains("<svg", label.GetProperty("qrSvg").GetString());
        Assert.Equal("NotConnected", label.GetProperty("erpStatus").GetString());
        var issue = new
        {
            plantId,
            action = "Issue",
            confirmed = true,
            notes = "Truck dispatched",
            commandId = Guid.NewGuid(),
        };
        await Post(c, D + $"runs/{a.Run}/goods-issue", issue);
        await Post(c, D + $"runs/{a.Run}/goods-issue", issue);
        var report = await Read(c, "reports");
        Assert.Equal(
            10m,
            report
                .GetProperty("byUom")
                .EnumerateArray()
                .Single(x => x.GetProperty("uom").GetString() == "kg")
                .GetProperty("shipped")
                .GetDecimal()
        );
        var reverse = new
        {
            plantId,
            action = "Reverse",
            confirmed = true,
            notes = "Truck returned; all stock physically checked",
            commandId = Guid.NewGuid(),
        };
        await Post(c, D + $"runs/{a.Run}/reverse", reverse);
        await Post(c, D + $"runs/{a.Run}/reverse", reverse);
        report = await Read(c, "reports");
        Assert.All(
            report.GetProperty("byUom").EnumerateArray(),
            r => Assert.Equal(0m, r.GetProperty("shipped").GetDecimal())
        );
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
        Assert.Equal(
            100m,
            await db.Set<Pallet>().Where(x => x.BatchId == a.BatchA).SumAsync(x => x.Quantity)
        );
        Assert.Equal(0m, await db.Set<Pallet>().SumAsync(x => x.ReservedQuantity));
        Assert.All(await db.Set<Batch>().ToListAsync(), b => Assert.Equal("Hold", b.QualityStatus));
        Assert.Equal(2, await db.Set<Shipment>().CountAsync());
        Assert.Equal(2, await db.Set<DispatchReversalLine>().CountAsync());
        Assert.All(
            await db.Set<DeliveryOrder>().ToListAsync(),
            o => Assert.Equal("Open", o.Status)
        );
        Assert.Equal("Reversed", (await db.Set<DispatchRun>().SingleAsync()).Status);
        Assert.Equal(
            0m,
            await db.Set<StockMovement>()
                .Where(x => x.Kind == "DispatchPickIn" || x.Kind == "DispatchPickOut")
                .SumAsync(x => x.Quantity)
        );
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync("DELETE FROM \"DispatchIssueLine\"")
        );
    }

    [Fact]
    public async Task ScopeAssignmentAndCurrentPermissionsProtectAllDispatchActionsAndReplays()
    {
        using var c = await factory.Login("admin@example.test");
        using var viewer = await factory.Login("viewer@example.test");
        var a = await Data(c);
        var plantId = factory.PlantA;
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await viewer.GetAsync(Root + D + "runs?plantId=" + factory.PlantB)).StatusCode
        );
        using var noWrite = await viewer.PostAsJsonAsync(
            Root + D + $"runs/{a.Run}/state",
            new
            {
                plantId,
                action = "StartPicking",
                confirmed = true,
                notes = "",
                commandId = Guid.NewGuid(),
            }
        );
        Assert.Equal(HttpStatusCode.Forbidden, noWrite.StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await c.GetAsync(Root + D + $"runs/{a.Run}?plantId=" + factory.PlantAOther)).StatusCode
        );
        Guid operatorId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            var user = await db.Users.SingleAsync(x => x.Email == "viewer@example.test");
            user.Role = "Operator";
            operatorId = user.Id;
            await db.SaveChangesAsync();
        }
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await viewer.GetAsync(Root + D + $"runs/{a.Run}?plantId=" + plantId)).StatusCode
        );
        var run = (await Read(c, D + $"runs/{a.Run}")).GetProperty("run");
        await Post(
            c,
            D + $"runs/{a.Run}/assignment",
            new
            {
                plantId,
                assignedToId = operatorId,
                scheduledAt = DateTimeOffset.UtcNow,
                priority = false,
                reason = "Picker shift",
                version = run.GetProperty("version").GetUInt32(),
                commandId = Guid.NewGuid(),
            }
        );
        var start = new
        {
            plantId,
            action = "StartPicking",
            confirmed = true,
            notes = "Accepted",
            commandId = Guid.NewGuid(),
        };
        await Post(viewer, D + $"runs/{a.Run}/state", start);
        run = (await Read(c, D + $"runs/{a.Run}")).GetProperty("run");
        await Post(
            c,
            D + $"runs/{a.Run}/assignment",
            new
            {
                plantId,
                assignedToId = a.Admin,
                scheduledAt = DateTimeOffset.UtcNow,
                priority = true,
                reason = "Reassigned",
                version = run.GetProperty("version").GetUInt32(),
                commandId = Guid.NewGuid(),
            }
        );
        using var deniedReplay = await viewer.PostAsJsonAsync(
            Root + D + $"runs/{a.Run}/state",
            start
        );
        Assert.Equal(HttpStatusCode.Forbidden, deniedReplay.StatusCode);
        var body = await Read(viewer, D + "runs");
        Assert.Empty(body.GetProperty("items").EnumerateArray());
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            db.Add(
                new DispatchRunLine
                {
                    TenantId = factory.TenantA,
                    PlantId = factory.PlantAOther,
                    DispatchRunId = a.Run,
                    MaterialId = a.MaterialA,
                    Quantity = 1,
                }
            );
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task ShelfLifeScansSafetyAndCombinedQuantitiesCannotBeBypassed()
    {
        using var c = await factory.Login("admin@example.test");
        var a = await Data(c);
        var plantId = factory.PlantA;
        await State(c, a.Run, "StartPicking");
        var line = (await Read(c, D + $"runs/{a.Run}"))
            .GetProperty("lines")
            .EnumerateArray()
            .Single(x => x.GetProperty("materialId").GetGuid() == a.MaterialA);
        object Input(decimal q, string scan = "PAL-A", bool safety = true) =>
            new
            {
                plantId,
                lines = new[]
                {
                    new
                    {
                        dispatchRunLineId = Id(line),
                        palletId = a.SourceA,
                        palletCode = scan,
                        sourceBin = "FG",
                        quantity = q,
                    },
                },
                instructionsConfirmed = true,
                safetyConfirmed = safety,
                commandId = Guid.NewGuid(),
            };
        await Bad(c, D + $"runs/{a.Run}/picks", Input(1, safety: false), "PPE");
        await Bad(c, D + $"runs/{a.Run}/picks", Input(1, "WRONG"), "source bin and pallet");
        await Bad(
            c,
            D + $"runs/{a.Run}/picks",
            new
            {
                plantId,
                lines = new[]
                {
                    new
                    {
                        dispatchRunLineId = Id(line),
                        palletId = a.SourceA,
                        palletCode = "PAL-A",
                        sourceBin = "FG",
                        quantity = 6m,
                    },
                    new
                    {
                        dispatchRunLineId = Id(line),
                        palletId = a.SourceA,
                        palletCode = "PAL-A",
                        sourceBin = "FG",
                        quantity = 6m,
                    },
                },
                instructionsConfirmed = true,
                safetyConfirmed = true,
                commandId = Guid.NewGuid(),
            },
            "exceeds"
        );
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            (await db.Set<Batch>().SingleAsync(x => x.Id == a.BatchA)).ExpiryDate = null;
            await db.SaveChangesAsync();
        }
        await Bad(c, D + $"runs/{a.Run}/picks", Input(1), "minimum shelf life");
        var stock = await c.GetFromJsonAsync<JsonElement>(
            Root + D + $"runs/{a.Run}/stock?plantId={plantId}&lineId={Id(line)}"
        );
        Assert.Empty(stock.GetProperty("items").EnumerateArray());
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            var batch = await db.Set<Batch>().SingleAsync(x => x.Id == a.BatchA);
            batch.ExpiryDate = DateTimeOffset.UtcNow.AddDays(20);
            batch.QualityStatus = "Hold";
            await db.SaveChangesAsync();
        }
        await Bad(c, D + $"runs/{a.Run}/picks", Input(1), "quality-released");
    }

    [Fact]
    public async Task ConcurrentPicksCannotOverdrawFreeStockAndClaimedDeliveriesStayExclusive()
    {
        using var c = await factory.Login("admin@example.test");
        var a = await Data(c);
        var plantId = factory.PlantA;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            var old = await db.Set<DeliveryOrder>().SingleAsync(x => x.Id == a.OrderA);
            var other = await Post(
                c,
                "delivery-orders",
                new
                {
                    plantId,
                    number = "DO-OTHER",
                    customerId = old.CustomerId,
                    materialId = a.MaterialA,
                    quantity = 10m,
                }
            );
            var second = Result(
                await Post(
                    c,
                    D + "runs",
                    new
                    {
                        plantId,
                        number = "SHIP-2",
                        dispatchAreaId = a.Area,
                        kind = "Customer",
                        dispatchWarehouseId = (Guid?)null,
                        assignedToId = a.Admin,
                        scheduledAt = DateTimeOffset.UtcNow,
                        priority = false,
                        route = "Other",
                        deliveryOrderIds = new[] { Id(other) },
                        shuttleLines = Array.Empty<object>(),
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
                    palletId = a.SourceA,
                    quantity = -85m,
                    reason = "Isolated test stock count",
                    commandId = Guid.NewGuid(),
                }
            );
            await State(c, a.Run, "StartPicking");
            await State(c, second, "StartPicking");
            async Task<HttpResponseMessage> Pick(Guid run)
            {
                var line = (await Read(c, D + $"runs/{run}"))
                    .GetProperty("lines")
                    .EnumerateArray()
                    .Single(x => x.GetProperty("materialId").GetGuid() == a.MaterialA);
                return await c.PostAsJsonAsync(
                    Root + D + $"runs/{run}/picks",
                    new
                    {
                        plantId,
                        lines = new[]
                        {
                            new
                            {
                                dispatchRunLineId = Id(line),
                                palletId = a.SourceA,
                                palletCode = "PAL-A",
                                sourceBin = "FG",
                                quantity = 10m,
                            },
                        },
                        instructionsConfirmed = true,
                        safetyConfirmed = true,
                        commandId = Guid.NewGuid(),
                    }
                );
            }
            var responses = await Task.WhenAll(Pick(a.Run), Pick(second));
            try
            {
                Assert.Single(responses, x => x.IsSuccessStatusCode);
                Assert.All(
                    responses.Where(x => !x.IsSuccessStatusCode),
                    x =>
                        Assert.True(
                            x.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest
                        )
                );
            }
            finally
            {
                foreach (var r in responses)
                    r.Dispose();
            }
            db.ChangeTracker.Clear();
            Assert.Equal(5m, (await db.Set<Pallet>().SingleAsync(x => x.Id == a.SourceA)).Quantity);
            Assert.Equal(10m, await db.Set<Pallet>().SumAsync(x => x.ReservedQuantity));
        }
        await Bad(
            c,
            D + "runs",
            new
            {
                plantId,
                number = "DUPLICATE",
                dispatchAreaId = a.Area,
                kind = "Customer",
                dispatchWarehouseId = (Guid?)null,
                assignedToId = a.Admin,
                scheduledAt = DateTimeOffset.UtcNow,
                priority = false,
                route = "",
                deliveryOrderIds = new[] { a.OrderA },
                shuttleLines = Array.Empty<object>(),
                commandId = Guid.NewGuid(),
            },
            "already assigned"
        );
    }

    [Fact]
    public async Task UnloadUnpackReturnAndCancelRestoreOriginalBalancesWithHistory()
    {
        using var c = await factory.Login("admin@example.test");
        var a = await Data(c);
        var plantId = factory.PlantA;
        await State(c, a.Run, "StartPicking");
        await Picks(c, a);
        var unit = await PackAndLoad(c, a);
        var detail = await Read(c, D + $"runs/{a.Run}");
        var code = detail.GetProperty("units")[0].GetProperty("code").GetString();
        await Bad(
            c,
            D + $"units/{unit}/state",
            new
            {
                plantId,
                action = "Unpack",
                unitCode = code,
                grossKg = (decimal?)null,
                notes = "Return",
                commandId = Guid.NewGuid(),
            },
            "Unload"
        );
        foreach (var action in new[] { "Unload", "Unpack" })
            await Post(
                c,
                D + $"units/{unit}/state",
                new
                {
                    plantId,
                    action,
                    unitCode = code,
                    grossKg = (decimal?)null,
                    notes = "Customer cancelled before departure",
                    commandId = Guid.NewGuid(),
                }
            );
        detail = await Read(c, D + $"runs/{a.Run}");
        foreach (var pick in detail.GetProperty("picks").EnumerateArray())
        {
            var pallet = detail
                .GetProperty("pallets")
                .EnumerateArray()
                .Single(x => Id(x) == pick.GetProperty("pickedPalletId").GetGuid());
            await Post(
                c,
                D + $"picks/{Id(pick)}/return",
                new
                {
                    plantId,
                    quantity = pick.GetProperty("quantity").GetDecimal(),
                    palletCode = pallet.GetProperty("code").GetString(),
                    destinationBin = "FG",
                    reason = "Order cancellation",
                    commandId = Guid.NewGuid(),
                }
            );
        }
        await State(c, a.Run, "Cancel");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
        Assert.Equal(100m, (await db.Set<Pallet>().SingleAsync(x => x.Id == a.SourceA)).Quantity);
        Assert.Equal(50m, (await db.Set<Pallet>().SingleAsync(x => x.Id == a.SourceB)).Quantity);
        Assert.Equal(0m, await db.Set<Pallet>().SumAsync(x => x.ReservedQuantity));
        Assert.All(await db.Set<DispatchRunLine>().ToListAsync(), x => Assert.False(x.Active));
        Assert.Empty(await db.Set<Shipment>().ToListAsync());
        Assert.True(await db.Set<DispatchEvent>().AnyAsync(x => x.Kind == "UnitUnload"));
    }

    [Fact]
    public async Task ShuttleIssuesAreExplicitlyLocalAndPriorSchemaUpgradePreservesStock()
    {
        using var c = await factory.Login("admin@example.test");
        var a = await Data(c);
        var plantId = factory.PlantA;
        var warehouse = Result(
            await Post(
                c,
                D + "warehouses",
                new
                {
                    plantId,
                    code = "EXT",
                    name = "External store",
                    receivingPlant = "PLANT2",
                    storageLocation = "ST01",
                    gate = "GATE1",
                    commandId = Guid.NewGuid(),
                }
            )
        );
        var run = Result(
            await Post(
                c,
                D + "runs",
                new
                {
                    plantId,
                    number = "SHUTTLE",
                    dispatchAreaId = a.Area,
                    kind = "Shuttle",
                    dispatchWarehouseId = warehouse,
                    assignedToId = a.Admin,
                    scheduledAt = DateTimeOffset.UtcNow,
                    priority = false,
                    route = "Warehouse route",
                    deliveryOrderIds = Array.Empty<Guid>(),
                    shuttleLines = new[] { new { materialId = a.MaterialA, quantity = 10m } },
                    commandId = Guid.NewGuid(),
                }
            )
        );
        var shuttle = a with { Run = run };
        await State(c, run, "StartPicking");
        var line = (await Read(c, D + $"runs/{run}")).GetProperty("lines")[0];
        await Post(
            c,
            D + $"runs/{run}/picks",
            new
            {
                plantId,
                lines = new[]
                {
                    new
                    {
                        dispatchRunLineId = Id(line),
                        palletId = a.SourceA,
                        palletCode = "PAL-A",
                        sourceBin = "FG",
                        quantity = 10m,
                    },
                },
                instructionsConfirmed = true,
                safetyConfirmed = true,
                commandId = Guid.NewGuid(),
            }
        );
        await PackAndLoad(c, shuttle);
        await Post(
            c,
            D + $"runs/{run}/goods-issue",
            new
            {
                plantId,
                action = "Issue",
                confirmed = true,
                notes = "Local warehouse dispatch",
                commandId = Guid.NewGuid(),
            }
        );
        var details = await Read(c, D + $"runs/{run}");
        Assert.Equal("NotConnected", details.GetProperty("erpStatus").GetString());
        Assert.StartsWith(
            "LOCAL-PGI-",
            details.GetProperty("run").GetProperty("goodsIssueReference").GetString()
        );
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
        Assert.Empty(await db.Set<Shipment>().ToListAsync());
        Assert.Single(await db.Set<DispatchIssueLine>().ToListAsync());
        var stock = await db.Set<Pallet>().SumAsync(x => x.Quantity);
        var movements = await db.Set<StockMovement>().CountAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261017144443_ManufacturingExecution");
        await migrator.MigrateAsync();
        db.ChangeTracker.Clear();
        Assert.Equal(stock, await db.Set<Pallet>().SumAsync(x => x.Quantity));
        Assert.Equal(movements, await db.Set<StockMovement>().CountAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task QualityIsRecheckedAtGoodsIssueAndConcurrentPostingCannotDispatchTwice()
    {
        using var c = await factory.Login("admin@example.test");
        var a = await Data(c);
        var plantId = factory.PlantA;
        await State(c, a.Run, "StartPicking");
        await Picks(c, a);
        await PackAndLoad(c, a);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            (await db.Set<Batch>().SingleAsync(x => x.Id == a.BatchA)).QualityStatus = "Hold";
            await db.SaveChangesAsync();
        }
        await Bad(
            c,
            D + $"runs/{a.Run}/goods-issue",
            new
            {
                plantId,
                action = "Issue",
                confirmed = true,
                notes = "",
                commandId = Guid.NewGuid(),
            },
            "quality-released"
        );
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            Assert.Empty(await db.Set<Shipment>().ToListAsync());
            Assert.Equal(15m, await db.Set<Pallet>().SumAsync(x => x.ReservedQuantity));
        }
        await Post(
            c,
            "quality-inspections",
            new
            {
                plantId,
                batchId = a.BatchA,
                decision = "Released",
                notes = "Manager reviewed stock",
                commandId = Guid.NewGuid(),
            }
        );
        var responses = await Task.WhenAll(
            Enumerable
                .Range(0, 2)
                .Select(_ =>
                    c.PostAsJsonAsync(
                        Root + D + $"runs/{a.Run}/goods-issue",
                        new
                        {
                            plantId,
                            action = "Issue",
                            confirmed = true,
                            notes = "Confirmed dispatch",
                            commandId = Guid.NewGuid(),
                        }
                    )
                )
        );
        try
        {
            Assert.Single(responses, x => x.IsSuccessStatusCode);
            Assert.All(
                responses.Where(x => !x.IsSuccessStatusCode),
                x =>
                    Assert.True(
                        x.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest
                    )
            );
        }
        finally
        {
            foreach (var r in responses)
                r.Dispose();
        }
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            Assert.Equal(2, await db.Set<Shipment>().CountAsync());
            Assert.Equal(135m, await db.Set<Pallet>().SumAsync(x => x.Quantity));
            Assert.Equal(0m, await db.Set<Pallet>().SumAsync(x => x.ReservedQuantity));
        }
    }

    [Fact]
    public async Task CustomerInstructionSnapshotsAndProductPackingPoliciesAreEnforced()
    {
        using var c = await factory.Login("admin@example.test");
        var a = await Data(c);
        var plantId = factory.PlantA;
        var instruction = (await Read(c, D + "instructions")).GetProperty("items")[0];
        var customer = instruction.GetProperty("customerId").GetGuid();
        await Post(
            c,
            D + "instructions",
            new
            {
                plantId,
                customerId = customer,
                picking = "New instructions",
                packing = "Validate products separately",
                shipping = "Upright",
                safety = "Gloves",
                minimumShelfDays = 20,
                productPacking = true,
                version = instruction.GetProperty("version").GetUInt32(),
                commandId = Guid.NewGuid(),
            }
        );
        var historical = (await Read(c, D + $"runs/{a.Run}")).GetProperty("lines")[0];
        using (
            var frozen = JsonDocument.Parse(historical.GetProperty("instructionsJson").GetString()!)
        )
        {
            Assert.Equal(10, frozen.RootElement.GetProperty("minimumShelfDays").GetInt32());
            Assert.False(frozen.RootElement.GetProperty("productPacking").GetBoolean());
        }
        var bins = await Read(c, "bins");
        var pack = Id(
            bins.EnumerateArray().Single(x => x.GetProperty("code").GetString() == "PACK")
        );
        var load = Id(
            bins.EnumerateArray().Single(x => x.GetProperty("code").GetString() == "LOAD")
        );
        var area = Result(
            await Post(
                c,
                D + "areas",
                new
                {
                    plantId,
                    code = "SINGLE",
                    name = "Single picker",
                    shippingPoint = "SINGLE",
                    packBinId = pack,
                    loadingBinId = load,
                    pickMode = "Single",
                    packMode = "Collective",
                    allowOverpick = false,
                    commandId = Guid.NewGuid(),
                }
            )
        );
        var orders = new List<Guid>();
        foreach (var material in new[] { a.MaterialA, a.MaterialB })
            orders.Add(
                Id(
                    await Post(
                        c,
                        "delivery-orders",
                        new
                        {
                            plantId,
                            number = material == a.MaterialA ? "DO-C" : "DO-D",
                            customerId = customer,
                            materialId = material,
                            quantity = 5m,
                        }
                    )
                )
            );
        var run = Result(
            await Post(
                c,
                D + "runs",
                new
                {
                    plantId,
                    number = "SINGLE-SHIP",
                    dispatchAreaId = area,
                    kind = "Customer",
                    dispatchWarehouseId = (Guid?)null,
                    assignedToId = a.Admin,
                    scheduledAt = DateTimeOffset.UtcNow,
                    priority = false,
                    route = "",
                    deliveryOrderIds = orders,
                    shuttleLines = Array.Empty<object>(),
                    commandId = Guid.NewGuid(),
                }
            )
        );
        await State(c, run, "StartPicking");
        var detail = await Read(c, D + $"runs/{run}");
        var inputs = detail
            .GetProperty("lines")
            .EnumerateArray()
            .Select(l => new
            {
                dispatchRunLineId = Id(l),
                palletId = l.GetProperty("materialId").GetGuid() == a.MaterialA
                    ? a.SourceA
                    : a.SourceB,
                palletCode = l.GetProperty("materialId").GetGuid() == a.MaterialA
                    ? "PAL-A"
                    : "PAL-B",
                sourceBin = "FG",
                quantity = 5m,
            })
            .ToArray();
        await Bad(
            c,
            D + $"runs/{run}/picks",
            new
            {
                plantId,
                lines = inputs,
                instructionsConfirmed = true,
                safetyConfirmed = true,
                commandId = Guid.NewGuid(),
            },
            "one delivery line"
        );
        foreach (var input in inputs)
            await Post(
                c,
                D + $"runs/{run}/picks",
                new
                {
                    plantId,
                    lines = new[] { input },
                    instructionsConfirmed = true,
                    safetyConfirmed = true,
                    commandId = Guid.NewGuid(),
                }
            );
        await State(c, run, "CompletePicking");
        await State(c, run, "StartPacking");
        var unit = Result(
            await Post(
                c,
                D + $"runs/{run}/units",
                new
                {
                    plantId,
                    dispatchPackageId = a.Package,
                    temperatureDevice = "SINGLE-TEMP",
                    commandId = Guid.NewGuid(),
                }
            )
        );
        detail = await Read(c, D + $"runs/{run}");
        var picks = detail.GetProperty("picks").EnumerateArray().ToArray();
        var pallets = detail.GetProperty("pallets").EnumerateArray().ToArray();
        var codes = picks
            .Select(p =>
                pallets
                    .Single(x => Id(x) == p.GetProperty("pickedPalletId").GetGuid())
                    .GetProperty("code")
                    .GetString()
            )
            .ToArray();
        await Bad(
            c,
            D + $"units/{unit}/pack",
            new
            {
                plantId,
                pickIds = picks.Select(Id).ToArray(),
                palletCodes = codes,
                instructionsConfirmed = true,
                commandId = Guid.NewGuid(),
            },
            "Product packing"
        );
        for (var i = 0; i < picks.Length; i++)
            await Post(
                c,
                D + $"units/{unit}/pack",
                new
                {
                    plantId,
                    pickIds = new[] { Id(picks[i]) },
                    palletCodes = new[] { codes[i] },
                    instructionsConfirmed = true,
                    commandId = Guid.NewGuid(),
                }
            );
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
        Assert.Equal(
            2,
            await db.Set<DispatchPick>().CountAsync(x => x.DispatchHandlingUnitId == unit)
        );
    }
}
