using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GreenInk;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace GreenInk.Tests;

public sealed class ReceivingTests : IAsyncLifetime
{
    readonly TestFactory factory = new();
    const string Root = "/api/operations/";

    public Task InitializeAsync() => factory.InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)factory).DisposeAsync();

    static Guid Id(JsonElement row) => row.GetProperty("id").GetGuid();

    static async Task<JsonElement> Post(HttpClient c, string path, object input)
    {
        using var response = await c.PostAsJsonAsync(Root + path, input);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    static async Task Bad(HttpClient c, string path, object input, string text)
    {
        using var response = await c.PostAsJsonAsync(Root + path, input);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(text, await response.Content.ReadAsStringAsync());
    }

    async Task<Guid> Order(HttpClient c, string suffix = "A")
    {
        var plantId = factory.PlantA;
        var m = await Post(
            c,
            "materials",
            new
            {
                plantId,
                code = "RAW-" + suffix,
                name = "Inbound test raw",
                uom = "kg",
                kind = "Raw",
            }
        );
        var p = await Post(
            c,
            "partners",
            new
            {
                plantId,
                code = "SUP-" + suffix,
                name = "Inbound test supplier",
                kind = "Supplier",
            }
        );
        return Id(
            await Post(
                c,
                "purchase-orders",
                new
                {
                    plantId,
                    number = "PO-" + suffix,
                    supplierId = Id(p),
                    materialId = Id(m),
                    quantity = 100,
                }
            )
        );
    }

    [Fact]
    public async Task TruckGatesAndPackedPostingAreAtomicScopedAndReplaySafe()
    {
        using var c = await factory.Login("admin@example.test");
        using var viewer = await factory.Login("viewer@example.test");
        var plantId = factory.PlantA;
        var po = await Order(c);
        await Post(
            c,
            "receiving/policy",
            new
            {
                plantId,
                requireTruck = true,
                requireDeliveryNote = true,
            }
        );
        var dock = Id(
            await Post(
                c,
                "receiving/docks",
                new
                {
                    plantId,
                    code = "D1",
                    name = "Inbound dock",
                    maintenance = false,
                }
            )
        );
        await Post(
            c,
            "receiving/checks",
            new
            {
                plantId,
                prompt = "Driver safety briefing",
                stage = "Arrival",
                required = true,
                requirePass = true,
                active = true,
            }
        );
        await Post(
            c,
            "receiving/checks",
            new
            {
                plantId,
                prompt = "Seal and unloading area safe",
                stage = "Unloading",
                required = true,
                requirePass = true,
                active = true,
            }
        );
        var registration = new
        {
            plantId,
            truckNumber = "TRUCK-01",
            driverName = "Validation driver",
            scheduledAt = DateTimeOffset.UtcNow.AddHours(1),
            purchaseOrderIds = new[] { po },
            commandId = Guid.NewGuid(),
        };
        var visit = Id(await Post(c, "receiving/visits", registration));
        Assert.Equal(visit, Id(await Post(c, "receiving/visits", registration)));
        await Bad(
            c,
            "receiving/visits",
            registration with
            {
                driverName = "Changed driver",
            },
            "different visit"
        );
        var path = $"receiving/visits/{visit}/status";
        await Bad(
            c,
            path,
            new
            {
                plantId,
                status = "Unloading",
                dockStationId = dock,
            },
            "Cannot change"
        );
        await Post(c, path, new { plantId, status = "Arrived" });
        await Bad(
            c,
            path,
            new
            {
                plantId,
                status = "Docked",
                dockStationId = dock,
            },
            "Driver safety"
        );
        var detail = await c.GetFromJsonAsync<JsonElement>(
            $"{Root}receiving/visits/{visit}?plantId={plantId}"
        );
        var answers = detail.GetProperty("checks").EnumerateArray().ToArray();
        var arrival = Id(answers.Single(x => x.GetProperty("stage").GetString() == "Arrival"));
        var unload = Id(answers.Single(x => x.GetProperty("stage").GetString() == "Unloading"));
        // Editing a configuration must not remove the already snapshotted obligation.
        await Post(
            c,
            "receiving/checks",
            new
            {
                plantId,
                prompt = "Driver safety briefing",
                stage = "Arrival",
                required = false,
                requirePass = false,
                active = false,
            }
        );
        await Bad(
            c,
            path,
            new
            {
                plantId,
                status = "Docked",
                dockStationId = dock,
            },
            "Driver safety"
        );
        await Post(
            c,
            $"receiving/checks/{arrival}/answer",
            new
            {
                plantId,
                passed = false,
                notes = "Unsafe",
            }
        );
        await Bad(
            c,
            path,
            new
            {
                plantId,
                status = "Docked",
                dockStationId = dock,
            },
            "Driver safety"
        );
        await Post(
            c,
            $"receiving/checks/{arrival}/answer",
            new
            {
                plantId,
                passed = true,
                notes = "Briefing completed",
            }
        );
        await Post(
            c,
            "receiving/docks",
            new
            {
                plantId,
                code = "D1",
                name = "Inbound dock",
                maintenance = true,
            }
        );
        await Bad(
            c,
            path,
            new
            {
                plantId,
                status = "Docked",
                dockStationId = dock,
            },
            "maintenance"
        );
        await Post(
            c,
            "receiving/docks",
            new
            {
                plantId,
                code = "D1",
                name = "Inbound dock",
                maintenance = false,
            }
        );
        await Post(
            c,
            path,
            new
            {
                plantId,
                status = "Docked",
                dockStationId = dock,
            }
        );
        await Bad(
            c,
            $"receiving/checks/{arrival}/answer",
            new { plantId, passed = false },
            "locked"
        );
        await Bad(c, path, new { plantId, status = "Unloading" }, "Seal");
        await Post(c, $"receiving/checks/{unload}/answer", new { plantId, passed = true });
        await Post(c, path, new { plantId, status = "Unloading" });
        var receipt = new
        {
            plantId,
            purchaseOrderId = po,
            quantity = 25m,
            supplierBatch = "LOT-ONE",
            batchNumber = "INT-ONE",
            commandId = Guid.NewGuid(),
            truckVisitId = (Guid?)visit,
            deliveryNote = "DN-1",
            packSize = 10m,
            packCount = 2,
            looseQuantity = 5m,
        };
        await Bad(c, "receipts", receipt with { quantity = 26m }, "pack size");
        await Bad(c, "receipts", receipt with { deliveryNote = "" }, "Delivery note");
        await Bad(c, "receipts", receipt with { truckVisitId = null }, "registered truck");
        var received = await Post(c, "receipts", receipt);
        Assert.Equal(Id(received), Id(await Post(c, "receipts", receipt)));
        await Bad(c, "receipts", receipt with { deliveryNote = "DIFFERENT" }, "different receipt");
        await Bad(
            c,
            "receipts",
            receipt with
            {
                commandId = Guid.NewGuid(),
                batchNumber = "TOO-MUCH",
                packSize = 100m,
                packCount = 2,
                quantity = 205m,
            },
            "outstanding"
        );
        var inventory = await c.GetFromJsonAsync<JsonElement>(
            Root + $"inventory?plantId={plantId}"
        );
        Assert.Equal(25m, inventory[0].GetProperty("pallet").GetProperty("quantity").GetDecimal());
        Assert.Equal(
            "Hold",
            inventory[0].GetProperty("batch").GetProperty("qualityStatus").GetString()
        );
        await Post(c, path, new { plantId, status = "Unloaded" });
        await Post(c, path, new { plantId, status = "Out" });
        Assert.Equal(Id(received), Id(await Post(c, "receipts", receipt))); // network retry after exit returns the original posting
        await Bad(
            c,
            "receipts",
            receipt with
            {
                commandId = Guid.NewGuid(),
                batchNumber = "AFTER-EXIT",
            },
            "actively unloading"
        );
        using var denied = await viewer.PostAsJsonAsync(
            Root + "receiving/visits",
            registration with
            {
                commandId = Guid.NewGuid(),
            }
        );
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await viewer.GetAsync(Root + $"receiving/visits?plantId={factory.PlantB}")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.NotFound,
            (
                await c.GetAsync(Root + $"receiving/visits/{visit}?plantId={factory.PlantAOther}")
            ).StatusCode
        );
        var ledger = await c.GetFromJsonAsync<JsonElement>(Root + $"movements?plantId={plantId}");
        Assert.Single(ledger.EnumerateArray());
    }

    [Fact]
    public async Task DockQueueCannotUnloadTwoTrucksAndForeignOrdersAreRejected()
    {
        using var c = await factory.Login("admin@example.test");
        var plantId = factory.PlantA;
        var po = await Order(c);
        var dock = Id(
            await Post(
                c,
                "receiving/docks",
                new
                {
                    plantId,
                    code = "D1",
                    name = "Single bay",
                    maintenance = false,
                }
            )
        );
        var first = Id(
            await Post(
                c,
                "receiving/visits",
                new
                {
                    plantId,
                    truckNumber = "T-1",
                    driverName = "Driver one",
                    scheduledAt = DateTimeOffset.UtcNow,
                    purchaseOrderIds = new[] { po },
                    commandId = Guid.NewGuid(),
                }
            )
        );
        var second = Id(
            await Post(
                c,
                "receiving/visits",
                new
                {
                    plantId,
                    truckNumber = "T-2",
                    driverName = "Driver two",
                    scheduledAt = DateTimeOffset.UtcNow,
                    purchaseOrderIds = new[] { po },
                    commandId = Guid.NewGuid(),
                }
            )
        );
        foreach (var v in new[] { first, second })
        {
            await Post(c, $"receiving/visits/{v}/status", new { plantId, status = "Arrived" });
            await Post(
                c,
                $"receiving/visits/{v}/status",
                new
                {
                    plantId,
                    status = "Docked",
                    dockStationId = dock,
                }
            );
        }
        await Post(c, $"receiving/visits/{first}/status", new { plantId, status = "Unloading" });
        await Bad(
            c,
            $"receiving/visits/{second}/status",
            new { plantId, status = "Unloading" },
            "another truck"
        );
        await Post(c, $"receiving/visits/{first}/status", new { plantId, status = "Unloaded" });
        await Post(c, $"receiving/visits/{second}/status", new { plantId, status = "Unloading" });
        await Bad(
            c,
            "receiving/visits",
            new
            {
                plantId = factory.PlantAOther,
                truckNumber = "FOREIGN",
                driverName = "Foreign",
                scheduledAt = DateTimeOffset.UtcNow,
                purchaseOrderIds = new[] { po },
                commandId = Guid.NewGuid(),
            },
            "belong to this plant"
        );
        await Bad(
            c,
            "receiving/visits",
            new
            {
                plantId,
                truckNumber = "DOUBLE",
                driverName = "Duplicate",
                scheduledAt = DateTimeOffset.UtcNow,
                purchaseOrderIds = new[] { po, po },
                commandId = Guid.NewGuid(),
            },
            "Duplicate"
        );
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
        var row = await db.Set<TruckVisit>().FirstAsync(x => x.Id == first);
        row.DriverName = "Persisted driver";
        await db.SaveChangesAsync();
        Assert.Equal(
            "Persisted driver",
            (await db.Set<TruckVisit>().AsNoTracking().SingleAsync(x => x.Id == first)).DriverName
        );
        // The composite FK also prevents a raw SQL/client bypass from linking a foreign-plant order.
        db.Add(
            new TruckOrder
            {
                TenantId = factory.TenantB,
                PlantId = factory.PlantB,
                TruckVisitId = first,
                PurchaseOrderId = po,
            }
        );
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(
            PostgresErrorCodes.ForeignKeyViolation,
            Assert.IsType<PostgresException>(error.GetBaseException()).SqlState
        );
    }

    [Fact]
    public async Task ConcurrentDockStartsHaveOneWinner()
    {
        using var c = await factory.Login("admin@example.test");
        var plantId = factory.PlantA;
        var po = await Order(c);
        var dock = Id(
            await Post(
                c,
                "receiving/docks",
                new
                {
                    plantId,
                    code = "RACE",
                    name = "Race bay",
                    maintenance = false,
                }
            )
        );
        var ids = new List<Guid>();
        foreach (var number in new[] { "R-1", "R-2" })
        {
            var id = Id(
                await Post(
                    c,
                    "receiving/visits",
                    new
                    {
                        plantId,
                        truckNumber = number,
                        driverName = "Validation driver",
                        scheduledAt = DateTimeOffset.UtcNow,
                        purchaseOrderIds = new[] { po },
                        commandId = Guid.NewGuid(),
                    }
                )
            );
            ids.Add(id);
            await Post(c, $"receiving/visits/{id}/status", new { plantId, status = "Arrived" });
            await Post(
                c,
                $"receiving/visits/{id}/status",
                new
                {
                    plantId,
                    status = "Docked",
                    dockStationId = dock,
                }
            );
        }
        var responses = await Task.WhenAll(
            ids.Select(id =>
                c.PostAsJsonAsync(
                    Root + $"receiving/visits/{id}/status",
                    new { plantId, status = "Unloading" }
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
            var rows = await c.GetFromJsonAsync<JsonElement>(
                Root + $"receiving/visits?plantId={plantId}"
            );
            Assert.Single(
                rows.EnumerateArray(),
                x => x.GetProperty("status").GetString() == "Unloading"
            );
        }
        finally
        {
            foreach (var response in responses)
                response.Dispose();
        }
    }

    [Fact]
    public async Task ReceivingUpgradePreservesExistingReceiptsAndLedger()
    {
        using var c = await factory.Login("admin@example.test");
        var plantId = factory.PlantA;
        var po = await Order(c);
        var receipt = await Post(
            c,
            "receipts",
            new
            {
                plantId,
                purchaseOrderId = po,
                quantity = 15m,
                supplierBatch = "EXISTING",
                batchNumber = "EXISTING",
                commandId = Guid.NewGuid(),
            }
        );
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261015195055_LabelPrintSnapshots");
        // The database is now the previous released schema, populated with real receipt/ledger rows.
        await migrator.MigrateAsync();
        db.ChangeTracker.Clear();
        var restored = await db.Set<GoodsReceipt>().SingleAsync();
        Assert.Equal(Id(receipt), restored.Id);
        Assert.Equal(15m, restored.Quantity);
        Assert.Null(restored.TruckVisitId);
        Assert.Null(restored.PackSize);
        Assert.Null(restored.PackCount);
        Assert.Equal("", restored.DeliveryNote);
        Assert.Equal(0m, restored.LooseQuantity);
        Assert.Equal(15m, (await db.Set<Pallet>().SingleAsync()).Quantity);
        Assert.Equal(15m, (await db.Set<PurchaseOrder>().SingleAsync()).ReceivedQuantity);
        Assert.Equal(15m, (await db.Set<StockMovement>().SingleAsync()).Quantity);
        var blocked = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync("UPDATE \"StockMovement\" SET \"Quantity\" = 0")
        );
        Assert.Equal(PostgresErrorCodes.CheckViolation, blocked.SqlState);
    }
}
