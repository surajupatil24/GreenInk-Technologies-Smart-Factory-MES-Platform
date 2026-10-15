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

// Exercises production endpoints against isolated PostgreSQL databases, including retries and races.
public sealed class OperationsTests : IAsyncLifetime
{
    private readonly TestFactory factory = new();

    public Task InitializeAsync() => factory.InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)factory).DisposeAsync();

    private static async Task<JsonElement> Post(HttpClient client, string path, object request)
    {
        using var response = await client.PostAsJsonAsync(path, request);
        Assert.True(
            response.IsSuccessStatusCode,
            $"POST {path}: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}"
        );
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static Guid Id(JsonElement value) => value.GetProperty("id").GetGuid();

    private const string Root = "/api/operations";

    private record Setup(Guid Material, Guid Finished, Guid Supplier, Guid Customer, Guid Purchase);

    private async Task<Setup> MasterData(HttpClient client, decimal ordered = 100)
    {
        var plantId = factory.PlantA;
        var raw = await Post(
            client,
            Root + "/materials",
            new
            {
                plantId,
                code = "RAW",
                name = "Raw material",
                uom = "kg",
                kind = "Raw",
            }
        );
        var finished = await Post(
            client,
            Root + "/materials",
            new
            {
                plantId,
                code = "FG",
                name = "Finished product",
                uom = "kg",
                kind = "Finished",
            }
        );
        var supplier = await Post(
            client,
            Root + "/partners",
            new
            {
                plantId,
                code = "SUP",
                name = "Supplier",
                kind = "Supplier",
            }
        );
        var customer = await Post(
            client,
            Root + "/partners",
            new
            {
                plantId,
                code = "CUS",
                name = "Customer",
                kind = "Customer",
            }
        );
        await Post(
            client,
            Root + "/bins",
            new
            {
                plantId,
                code = "STORE",
                name = "Main store",
                warehouse = "Main warehouse",
            }
        );
        var purchase = await Post(
            client,
            Root + "/purchase-orders",
            new
            {
                plantId,
                number = "PO1",
                supplierId = Id(supplier),
                materialId = Id(raw),
                quantity = ordered,
            }
        );
        return new(Id(raw), Id(finished), Id(supplier), Id(customer), Id(purchase));
    }

    private object Receipt(
        Guid purchase,
        decimal quantity,
        string batch,
        Guid command,
        DateTimeOffset? expiry = null
    ) =>
        new
        {
            plantId = factory.PlantA,
            purchaseOrderId = purchase,
            quantity,
            supplierBatch = "Supplier-" + batch,
            batchNumber = batch,
            expiryDate = expiry,
            commandId = command,
        };

    [Fact]
    public async Task ReceivingRetriesPreserveLedgerAndRejectInvalidOrConflictingCommands()
    {
        using var admin = await factory.Login("admin@example.test");
        var data = await MasterData(admin, 10);
        var command = Guid.NewGuid();
        var request = Receipt(data.Purchase, 6, "B1", command);
        var received = await Post(admin, Root + "/receipts", request);
        var retry = await Post(admin, Root + "/receipts", request);
        Assert.Equal(Id(received), Id(retry));
        foreach (
            var invalid in new[]
            {
                Receipt(data.Purchase, -1, "NEG", Guid.NewGuid()),
                Receipt(data.Purchase, 5, "OVER", Guid.NewGuid()),
                Receipt(data.Purchase, 5, "B1", command),
            }
        )
            Assert.Equal(
                HttpStatusCode.BadRequest,
                (await admin.PostAsJsonAsync(Root + "/receipts", invalid)).StatusCode
            );
        var palletId = received.GetProperty("palletId").GetGuid();
        var transferCommand = Guid.NewGuid();
        var transfer = new
        {
            plantId = factory.PlantA,
            palletId,
            location = "STORE",
            commandId = transferCommand,
        };
        var movement = await Post(admin, Root + "/transfers", transfer);
        Assert.Equal(Id(movement), Id(await Post(admin, Root + "/transfers", transfer)));
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (
                await admin.PostAsJsonAsync(
                    Root + "/adjustments",
                    new
                    {
                        plantId = factory.PlantA,
                        palletId,
                        quantity = -7,
                        reason = "Count correction",
                        commandId = Guid.NewGuid(),
                    }
                )
            ).StatusCode
        );
        await ReadDb(async db =>
        {
            Assert.Equal(1, await db.Set<GoodsReceipt>().CountAsync());
            Assert.Equal(6, (await db.Set<PurchaseOrder>().SingleAsync()).ReceivedQuantity);
            Assert.Equal(6, (await db.Set<Pallet>().SingleAsync()).Quantity);
            Assert.Equal("STORE", (await db.Set<Pallet>().SingleAsync()).Location);
            Assert.Equal("Hold", (await db.Set<Batch>().SingleAsync()).QualityStatus);
            Assert.Equal(6, await db.Set<StockMovement>().SumAsync(x => x.Quantity));
            Assert.Equal(2, await db.Set<StockMovement>().CountAsync());
            return true;
        });
        var available = await admin.GetFromJsonAsync<JsonElement[]>(
            $"{Root}/allocation?plantId={factory.PlantA}&materialId={data.Material}"
        );
        Assert.Empty(available!);
    }

    [Fact]
    public async Task ConcurrentReceiptsNeverOverReceiveOrCreateUnbackedStock()
    {
        using var admin = await factory.Login("admin@example.test");
        var data = await MasterData(admin, 10);
        var responses = await Task.WhenAll(
            admin.PostAsJsonAsync(
                Root + "/receipts",
                Receipt(data.Purchase, 7, "RACE1", Guid.NewGuid())
            ),
            admin.PostAsJsonAsync(
                Root + "/receipts",
                Receipt(data.Purchase, 7, "RACE2", Guid.NewGuid())
            )
        );
        Assert.Single(responses, x => x.IsSuccessStatusCode);
        Assert.All(
            responses.Where(x => !x.IsSuccessStatusCode),
            x =>
                Assert.Contains(
                    x.StatusCode,
                    new[] { HttpStatusCode.BadRequest, HttpStatusCode.Conflict }
                )
        );
        await ReadDb(async db =>
        {
            Assert.Equal(7, (await db.Set<PurchaseOrder>().SingleAsync()).ReceivedQuantity);
            Assert.Equal(1, await db.Set<GoodsReceipt>().CountAsync());
            Assert.Equal(7, await db.Set<Pallet>().SumAsync(x => x.Quantity));
            Assert.Equal(7, await db.Set<StockMovement>().SumAsync(x => x.Quantity));
            return true;
        });
    }

    [Fact]
    public async Task OperationalReadsAndWritesEnforcePlantScopeAndViewerRole()
    {
        using var admin = await factory.Login("admin@example.test");
        var data = await MasterData(admin);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(Root + "/production-orders", new { plantId = factory.PlantA, number = "INVALID", materialId = data.Finished, plannedQuantity = 1, requirements = new object?[] { null } })).StatusCode);
        Assert.Equal(0, await ReadDb(db => db.Set<ProductionOrder>().CountAsync()));
        using var viewer = await factory.Login("viewer@example.test");
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (
                await viewer.PostAsJsonAsync(
                    Root + "/receipts",
                    Receipt(data.Purchase, 1, "NO", Guid.NewGuid())
                )
            ).StatusCode
        );
        foreach (
            var resource in new[]
            {
                "materials",
                "partners",
                "bins",
                "purchase-orders",
                "receipts",
                "pallets",
                "batches",
                "inventory",
                "movements",
            }
        )
        {
            Assert.Equal(
                HttpStatusCode.Forbidden,
                (
                    await viewer.GetAsync($"{Root}/{resource}?plantId={factory.PlantAOther}")
                ).StatusCode
            );
            Assert.Equal(
                HttpStatusCode.Forbidden,
                (await admin.GetAsync($"{Root}/{resource}?plantId={factory.PlantB}")).StatusCode
            );
        }
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (
                await admin.PostAsJsonAsync(
                    Root + "/receipts",
                    new
                    {
                        plantId = factory.PlantAOther,
                        purchaseOrderId = data.Purchase,
                        quantity = 1,
                        supplierBatch = "Foreign",
                        batchNumber = "FOREIGN",
                        commandId = Guid.NewGuid(),
                    }
                )
            ).StatusCode
        );
        Assert.Equal(0, await ReadDb(db => db.Set<GoodsReceipt>().CountAsync()));
    }

    private async Task<T> ReadDb<T>(Func<FactoryDb, Task<T>> read)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await read(scope.ServiceProvider.GetRequiredService<FactoryDb>());
    }

    private Task<JsonElement> Inspect(
        HttpClient client,
        Guid batch,
        string decision = "Released"
    ) =>
        Post(
            client,
            Root + "/quality-inspections",
            new
            {
                plantId = factory.PlantA,
                batchId = batch,
                decision,
                notes = "Integration inspection",
                commandId = Guid.NewGuid(),
            }
        );

    [Fact]
    public async Task ReceivingThroughDispatchPreservesGenealogyAndQualityGates()
    {
        using var admin = await factory.Login("admin@example.test");
        var data = await MasterData(admin, 20);
        var late = await Post(
            admin,
            Root + "/receipts",
            Receipt(data.Purchase, 10, "LATE", Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(60))
        );
        var early = await Post(
            admin,
            Root + "/receipts",
            Receipt(data.Purchase, 10, "EARLY", Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(30))
        );
        var rawBatch = early.GetProperty("batchId").GetGuid();
        var rawPallet = early.GetProperty("palletId").GetGuid();
        var order = await Post(
            admin,
            Root + "/production-orders",
            new
            {
                plantId = factory.PlantA,
                number = "PROD1",
                materialId = data.Finished,
                plannedQuantity = 8,
                requirements = new[] { new { materialId = data.Material, quantity = 10 } },
            }
        );
        var orderId = Id(order);
        var reserve = new
        {
            plantId = factory.PlantA,
            productionOrderId = orderId,
            palletId = rawPallet,
            quantity = 10,
            commandId = Guid.NewGuid(),
        };
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync(Root + "/reservations", reserve)).StatusCode
        );
        await Post(
            admin,
            $"{Root}/production-orders/{orderId}/release",
            new { plantId = factory.PlantA, commandId = Guid.NewGuid() }
        );
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync(Root + "/reservations", reserve)).StatusCode
        );
        await Inspect(admin, rawBatch);
        await Inspect(admin, late.GetProperty("batchId").GetGuid());
        var allocation = await admin.GetFromJsonAsync<JsonElement[]>(
            $"{Root}/allocation?plantId={factory.PlantA}&materialId={data.Material}"
        );
        Assert.Equal(
            new[] { rawPallet, late.GetProperty("palletId").GetGuid() },
            allocation!.Select(x => x.GetProperty("pallet").GetProperty("id").GetGuid()).ToArray()
        );
        var reservation = await Post(admin, Root + "/reservations", reserve);
        Assert.Equal(Id(reservation), Id(await Post(admin, Root + "/reservations", reserve)));
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (
                await admin.PostAsJsonAsync(
                    Root + "/adjustments",
                    new
                    {
                        plantId = factory.PlantA,
                        palletId = rawPallet,
                        quantity = -1,
                        reason = "Reserved balance",
                        commandId = Guid.NewGuid(),
                    }
                )
            ).StatusCode
        );
        var complete = new
        {
            plantId = factory.PlantA,
            quantity = 7,
            scrapQuantity = 1,
            location = "STORE",
            commandId = Guid.NewGuid(),
        };
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (
                await admin.PostAsJsonAsync(
                    $"{Root}/production-orders/{orderId}/complete",
                    complete
                )
            ).StatusCode
        );
        var issue = new { plantId = factory.PlantA, commandId = Guid.NewGuid() };
        var consumption = await Post(admin, $"{Root}/reservations/{Id(reservation)}/issue", issue);
        Assert.Equal(
            Id(consumption),
            Id(await Post(admin, $"{Root}/reservations/{Id(reservation)}/issue", issue))
        );
        var confirmation = await Post(
            admin,
            $"{Root}/production-orders/{orderId}/complete",
            complete
        );
        Assert.Equal(
            Id(confirmation),
            Id(await Post(admin, $"{Root}/production-orders/{orderId}/complete", complete))
        );
        var finishedBatch = confirmation.GetProperty("batchId").GetGuid();
        var finishedPallet = confirmation.GetProperty("palletId").GetGuid();
        var delivery = await Post(
            admin,
            Root + "/delivery-orders",
            new
            {
                plantId = factory.PlantA,
                number = "DO1",
                customerId = data.Customer,
                materialId = data.Finished,
                quantity = 7,
            }
        );
        var ship = new
        {
            plantId = factory.PlantA,
            deliveryOrderId = Id(delivery),
            palletId = finishedPallet,
            quantity = 7,
            truck = "TEST-TRUCK",
            commandId = Guid.NewGuid(),
        };
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync(Root + "/shipments", ship)).StatusCode
        );
        await Inspect(admin, finishedBatch);
        var shipment = await Post(admin, Root + "/shipments", ship);
        Assert.Equal(Id(shipment), Id(await Post(admin, Root + "/shipments", ship)));
        foreach (var batchId in new[] { rawBatch, finishedBatch })
        {
            var trace = await admin.GetFromJsonAsync<JsonElement>(
                $"{Root}/traceability?plantId={factory.PlantA}&batchId={batchId}"
            );
            Assert.True(trace.GetProperty("isComplete").GetBoolean());
            Assert.False(trace.GetProperty("collectionsTruncated").GetBoolean());
            Assert.All(trace.GetProperty("perListTruncated").EnumerateObject(), flag => Assert.False(flag.Value.GetBoolean()));
            Assert.Equal(
                rawBatch,
                trace
                    .GetProperty("sourceBatches")
                    .EnumerateArray()
                    .Single()
                    .GetProperty("id")
                    .GetGuid()
            );
            Assert.Equal(
                finishedBatch,
                trace
                    .GetProperty("outputBatches")
                    .EnumerateArray()
                    .Single()
                    .GetProperty("id")
                    .GetGuid()
            );
            Assert.Equal(
                Id(shipment),
                trace.GetProperty("shipments").EnumerateArray().Single().GetProperty("id").GetGuid()
            );
            Assert.Equal(
                data.Customer,
                trace.GetProperty("customers").EnumerateArray().Single().GetProperty("id").GetGuid()
            );
            Assert.Equal(
                Id(early),
                trace.GetProperty("receipts").EnumerateArray().Single().GetProperty("id").GetGuid()
            );
        }
        await ReadDb(async db =>
        {
            Assert.Equal(1, await db.Set<Consumption>().CountAsync());
            Assert.Equal(1, await db.Set<ProductionConfirmation>().CountAsync());
            Assert.Equal(1, await db.Set<Shipment>().CountAsync());
            Assert.Equal("Completed", (await db.Set<ProductionOrder>().SingleAsync()).Status);
            Assert.Equal("Shipped", (await db.Set<DeliveryOrder>().SingleAsync()).Status);
            Assert.Equal(10, await db.Set<Pallet>().SumAsync(x => x.Quantity));
            Assert.Equal(0, await db.Set<Pallet>().SumAsync(x => x.ReservedQuantity));
            Assert.Equal(10, await db.Set<StockMovement>().SumAsync(x => x.Quantity));
            foreach (var pallet in await db.Set<Pallet>().ToListAsync())
                Assert.Equal(
                    pallet.Quantity,
                    await db.Set<StockMovement>()
                        .Where(x => x.PalletId == pallet.Id)
                        .SumAsync(x => x.Quantity)
                );
            return true;
        });
    }

    [Fact]
    public async Task LocalAdaptersPersistRetriesAndDeadLettersWithoutClaimingExternalDelivery()
    {
        using var admin = await factory.Login("admin@example.test");
        var data = await MasterData(admin, 1);
        var receipt = await Post(
            admin,
            Root + "/receipts",
            Receipt(data.Purchase, 1, "LABEL", Guid.NewGuid())
        );
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (
                await admin.PostAsJsonAsync(
                    Root + "/label-templates",
                    new
                    {
                        plantId = factory.PlantA,
                        name = "Too long",
                        content = new string('x', 8001),
                    }
                )
            ).StatusCode
        );
        var template = await Post(
            admin,
            Root + "/label-templates",
            new
            {
                plantId = factory.PlantA,
                name = "Pallet",
                content = "{{batchNumber}} {{quantity}} {{uom}}",
            }
        );
        var print = new
        {
            plantId = factory.PlantA,
            palletId = receipt.GetProperty("palletId").GetGuid(),
            labelTemplateId = Id(template),
            copies = 1,
            printer = "Local preview",
            commandId = Guid.NewGuid(),
        };
        var job = await Post(admin, Root + "/print-jobs", print);
        Assert.Equal(Id(job), Id(await Post(admin, Root + "/print-jobs", print)));
        var preview = await admin.GetFromJsonAsync<JsonElement>(
            $"{Root}/print-jobs/{Id(job)}/preview?plantId={factory.PlantA}"
        );
        Assert.False(preview.GetProperty("physicalPrint").GetBoolean());
        var rendered = preview.GetProperty("renderedContent").GetString()!.Split(' ');
        Assert.Equal("LABEL", rendered[0]);
        Assert.Equal(
            1m,
            decimal.Parse(rendered[1], System.Globalization.CultureInfo.InvariantCulture)
        );
        Assert.Equal("kg", rendered[2]);
        using var qr = await admin.GetAsync(
            $"{Root}/print-jobs/{Id(job)}/barcode.svg?plantId={factory.PlantA}"
        );
        Assert.Equal(HttpStatusCode.OK, qr.StatusCode);
        Assert.Equal("image/svg+xml", qr.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<svg", await qr.Content.ReadAsStringAsync());
        await Post(admin, Root + "/adjustments", new { plantId = factory.PlantA, palletId = receipt.GetProperty("palletId").GetGuid(), quantity = 2, reason = "Count after label generation", commandId = Guid.NewGuid() });
        var historical = await admin.GetFromJsonAsync<JsonElement>($"{Root}/print-jobs/{Id(job)}/preview?plantId={factory.PlantA}");
        Assert.Equal(preview.GetProperty("renderedContent").GetString(), historical.GetProperty("renderedContent").GetString());
        var reprint = new
        {
            plantId = factory.PlantA,
            reason = "Damaged label",
            commandId = Guid.NewGuid(),
        };
        using var viewer = await factory.Login("viewer@example.test");
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (
                await viewer.GetAsync(
                    $"{Root}/print-jobs/{Id(job)}/barcode.svg?plantId={factory.PlantAOther}"
                )
            ).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (
                await viewer.PostAsJsonAsync($"{Root}/print-jobs/{Id(job)}/reprint", reprint)
            ).StatusCode
        );
        var duplicate = await Post(admin, $"{Root}/print-jobs/{Id(job)}/reprint", reprint);
        var reprinted = await admin.GetFromJsonAsync<JsonElement>($"{Root}/print-jobs/{Id(duplicate)}/preview?plantId={factory.PlantA}");
        Assert.Equal(preview.GetProperty("renderedContent").GetString(), reprinted.GetProperty("renderedContent").GetString());
        Assert.Equal(
            Id(duplicate),
            Id(await Post(admin, $"{Root}/print-jobs/{Id(job)}/reprint", reprint))
        );
        var message = new
        {
            plantId = factory.PlantA,
            externalId = "EXT-1",
            kind = "Validation",
            payload = "[]",
        };
        var queued = await Post(admin, Root + "/integration-messages", message);
        Assert.Equal(Id(queued), Id(await Post(admin, Root + "/integration-messages", message)));
        foreach (var expected in new[] { "Failed", "Failed", "DeadLetter" })
        {
            var attempt = await Post(
                admin,
                $"{Root}/integration-messages/{Id(queued)}/process",
                new { plantId = factory.PlantA }
            );
            Assert.Equal(
                expected,
                attempt.GetProperty("message").GetProperty("status").GetString()
            );
            Assert.False(attempt.GetProperty("deliveredToExternalSystem").GetBoolean());
        }
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (
                await admin.PostAsJsonAsync(
                    $"{Root}/integration-messages/{Id(queued)}/process",
                    new { plantId = factory.PlantA }
                )
            ).StatusCode
        );
        Assert.Equal(2, await ReadDb(db => db.Set<PrintJob>().CountAsync()));
        Assert.Equal(1, await ReadDb(db => db.Set<IntegrationMessage>().CountAsync()));
    }

    [Fact]
    public async Task PersistedLedgerRejectsDatabaseUpdatesAndDeletes()
    {
        using var admin = await factory.Login("admin@example.test");
        var data = await MasterData(admin, 2);
        await Post(
            admin,
            Root + "/receipts",
            Receipt(data.Purchase, 2, "IMMUTABLE", Guid.NewGuid())
        );
        await ReadDb(async db =>
        {
            var movement = await db.Set<StockMovement>().AsNoTracking().SingleAsync();
            var update = await Assert.ThrowsAsync<PostgresException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE \"StockMovement\" SET \"Quantity\" = 900 WHERE \"Id\" = {movement.Id}"
                )
            );
            Assert.Equal(PostgresErrorCodes.CheckViolation, update.SqlState);
            var delete = await Assert.ThrowsAsync<PostgresException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM \"StockMovement\" WHERE \"Id\" = {movement.Id}"
                )
            );
            Assert.Equal(PostgresErrorCodes.CheckViolation, delete.SqlState);
            Assert.Equal(2, (await db.Set<StockMovement>().AsNoTracking().SingleAsync()).Quantity);
            return true;
        });
    }

    [Fact]
    public async Task CompositeForeignKeysRejectCrossTenantReferencesOutsideTheApi()
    {
        using var admin = await factory.Login("admin@example.test");
        var data = await MasterData(admin);
        using var platform = await factory.Login("platform@example.test");
        var foreign = await Post(
            platform,
            Root + "/materials",
            new
            {
                plantId = factory.PlantB,
                code = "FOREIGN",
                name = "Foreign raw",
                uom = "kg",
                kind = "Raw",
            }
        );
        var materialId = Id(foreign);
        await ReadDb(async db =>
        {
            var error = await Assert.ThrowsAsync<PostgresException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE \"PurchaseOrder\" SET \"MaterialId\" = {materialId} WHERE \"Id\" = {data.Purchase}"
                )
            );
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, error.SqlState);
            var mismatch = await Assert.ThrowsAsync<PostgresException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE \"Material\" SET \"TenantId\" = {factory.TenantA} WHERE \"Id\" = {materialId}"
                )
            );
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, mismatch.SqlState);
            Assert.Equal(data.Material, (await db.Set<PurchaseOrder>().SingleAsync()).MaterialId);
            return true;
        });
    }

    [Fact]
    public async Task ConcurrentIdenticalReceiptCommandsRemainRetrySafe()
    {
        using var admin = await factory.Login("admin@example.test");
        var data = await MasterData(admin, 10);
        var request = Receipt(data.Purchase, 5, "SAME", Guid.NewGuid());
        var results = await Task.WhenAll(
            admin.PostAsJsonAsync(Root + "/receipts", request),
            admin.PostAsJsonAsync(Root + "/receipts", request)
        );
        Assert.Contains(results, response => response.IsSuccessStatusCode);
        Assert.All(
            results,
            response =>
                Assert.True(
                    response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Conflict
                )
        );
        var retried = await Post(admin, Root + "/receipts", request);
        await ReadDb(async db =>
        {
            Assert.Equal(Id(retried), (await db.Set<GoodsReceipt>().SingleAsync()).Id);
            Assert.Equal(5, (await db.Set<PurchaseOrder>().SingleAsync()).ReceivedQuantity);
            Assert.Equal(5, await db.Set<Pallet>().SumAsync(x => x.Quantity));
            Assert.Equal(1, await db.Set<StockMovement>().CountAsync());
            return true;
        });
    }

    [Fact]
    public async Task FoundationDatabaseUpgradesWithoutLosingAccountsGrantsOrSessions()
    {
        var adminConnection = Environment.GetEnvironmentVariable("GREENINK_TEST_DATABASE")!;
        var name = "greenink_upgrade_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(adminConnection);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", admin))
            await create.ExecuteNonQueryAsync();
        try
        {
            var connection = new NpgsqlConnectionStringBuilder(adminConnection)
            {
                Database = name,
                Pooling = false,
            }.ConnectionString;
            var options = new DbContextOptionsBuilder<FactoryDb>().UseNpgsql(connection).Options;
            await using var db = new FactoryDb(options);
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261015181214_RevocableSessions");
            var tenant = new Tenant { Code = "UPGRADE", Name = "Existing organization" };
            var plant = new Plant
            {
                TenantId = tenant.Id,
                Code = "PLANT",
                Name = "Existing factory",
            };
            var user = new User
            {
                TenantId = tenant.Id,
                Email = "existing@example.test",
                DisplayName = "Existing admin",
                Role = "TenantAdmin",
                PasswordHash = "Preserved-hash-marker",
            };
            user.Plants.Add(new UserPlant { UserId = user.Id, PlantId = plant.Id });
            var session = new UserSession
            {
                UserId = user.Id,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(2),
            };
            db.AddRange(tenant, plant, user, session);
            await db.SaveChangesAsync();
            await migrator.MigrateAsync();
            db.ChangeTracker.Clear();
            var restored = await db.Users.Include(x => x.Plants).SingleAsync();
            Assert.Equal("Preserved-hash-marker", restored.PasswordHash);
            Assert.Equal("TenantAdmin", restored.Role);
            Assert.Equal(tenant.Id, restored.TenantId);
            Assert.Equal(plant.Id, Assert.Single(restored.Plants).PlantId);
            Assert.Equal(session.Id, (await db.Sessions.SingleAsync()).Id);
            Assert.Equal("Existing organization", (await db.Tenants.SingleAsync()).Name);
            db.Add(
                new Material
                {
                    TenantId = tenant.Id,
                    PlantId = plant.Id,
                    Code = "POST-UPGRADE",
                    Name = "New raw material",
                    Kind = "Raw",
                    Uom = "kg",
                }
            );
            await db.SaveChangesAsync();
            Assert.Equal(1, await db.Set<Material>().CountAsync());
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        }
        finally
        {
            await using var drop = new NpgsqlCommand(
                $"DROP DATABASE \"{name}\" WITH (FORCE)",
                admin
            );
            await drop.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task ReportsKeepUnitsSeparateAndWithholdUnsupportedOee()
    {
        using var admin = await factory.Login("admin@example.test");
        var data = await MasterData(admin, 3);
        await Post(admin, Root + "/receipts", Receipt(data.Purchase, 3, "KG", Guid.NewGuid()));
        var material = await Post(
            admin,
            Root + "/materials",
            new
            {
                plantId = factory.PlantA,
                code = "LIQ",
                name = "Liquid",
                uom = "L",
                kind = "Raw",
            }
        );
        var purchase = await Post(
            admin,
            Root + "/purchase-orders",
            new
            {
                plantId = factory.PlantA,
                number = "PO2",
                supplierId = data.Supplier,
                materialId = Id(material),
                quantity = 5,
            }
        );
        await Post(admin, Root + "/receipts", Receipt(Id(purchase), 5, "LITRE", Guid.NewGuid()));
        var report = await admin.GetFromJsonAsync<JsonElement>(
            $"{Root}/reports?plantId={factory.PlantA}"
        );
        Assert.Equal(JsonValueKind.Null, report.GetProperty("oee").ValueKind);
        var units = report
            .GetProperty("byUom")
            .EnumerateArray()
            .ToDictionary(x => x.GetProperty("uom").GetString()!);
        Assert.Equal(2, units.Count);
        Assert.Equal(3, units["kg"].GetProperty("inventory").GetDecimal());
        Assert.Equal(5, units["L"].GetProperty("inventory").GetDecimal());
        Assert.Equal(3, units["kg"].GetProperty("hold").GetDecimal());
        Assert.Equal(0, units["L"].GetProperty("shipped").GetDecimal());
    }

    [Fact]
    public async Task CompetingReservationsCannotAllocateTheSameStockTwice()
    {
        using var admin = await factory.Login("admin@example.test");
        var data = await MasterData(admin, 10);
        var receipt = await Post(
            admin,
            Root + "/receipts",
            Receipt(data.Purchase, 10, "STOCK", Guid.NewGuid())
        );
        await Inspect(admin, receipt.GetProperty("batchId").GetGuid());
        var orders = new List<Guid>();
        foreach (var number in new[] { "O1", "O2" })
        {
            var order = await Post(
                admin,
                Root + "/production-orders",
                new
                {
                    plantId = factory.PlantA,
                    number,
                    materialId = data.Finished,
                    plannedQuantity = 7,
                    requirements = new[] { new { materialId = data.Material, quantity = 7 } },
                }
            );
            orders.Add(Id(order));
            await Post(
                admin,
                $"{Root}/production-orders/{Id(order)}/release",
                new { plantId = factory.PlantA, commandId = Guid.NewGuid() }
            );
        }
        var requests = orders
            .Select(productionOrderId => new
            {
                plantId = factory.PlantA,
                productionOrderId,
                palletId = receipt.GetProperty("palletId").GetGuid(),
                quantity = 7,
                commandId = Guid.NewGuid(),
            })
            .ToArray();
        var responses = await Task.WhenAll(
            requests.Select(request => admin.PostAsJsonAsync(Root + "/reservations", request))
        );
        Assert.Single(responses, x => x.IsSuccessStatusCode);
        Assert.All(
            responses.Where(x => !x.IsSuccessStatusCode),
            x =>
                Assert.Contains(
                    x.StatusCode,
                    new[] { HttpStatusCode.BadRequest, HttpStatusCode.Conflict }
                )
        );
        var rejected = Array.FindIndex(responses, x => !x.IsSuccessStatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync(Root + "/reservations", requests[rejected])).StatusCode
        );
        await ReadDb(async db =>
        {
            var pallet = await db.Set<Pallet>().SingleAsync();
            Assert.Equal(10, pallet.Quantity);
            Assert.Equal(7, pallet.ReservedQuantity);
            Assert.Equal(7, await db.Set<Reservation>().SumAsync(x => x.Quantity));
            Assert.Equal(0, await db.Set<Consumption>().CountAsync());
            return true;
        });
    }
}
