using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GreenInk;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace GreenInk.Tests;

// Every test receives its own real database, so tests can run in parallel without sharing state.
public sealed class TestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string databaseName = "greenink_test_" + Guid.NewGuid().ToString("N");
    private string adminConnection = "";
    private string connection = "";
    public Guid TenantA { get; } = Guid.NewGuid();
    public Guid TenantB { get; } = Guid.NewGuid();
    public Guid PlantA { get; } = Guid.NewGuid();
    public Guid PlantAOther { get; } = Guid.NewGuid();
    public Guid PlantB { get; } = Guid.NewGuid();
    public static readonly string Password = "Aa1!" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(
            (_, config) =>
                config.AddInMemoryCollection(
                    new Dictionary<string, string?> { ["ConnectionStrings:Database"] = connection }
                )
        );
    }

    public async Task InitializeAsync()
    {
        adminConnection =
            Environment.GetEnvironmentVariable("GREENINK_TEST_DATABASE")
            ?? throw new InvalidOperationException(
                "Set GREENINK_TEST_DATABASE to a PostgreSQL connection with CREATE DATABASE permission. These integration tests never use an in-memory provider."
            );
        await using (var admin = new NpgsqlConnection(adminConnection))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand(
                $"CREATE DATABASE \"{databaseName}\"",
                admin
            );
            await create.ExecuteNonQueryAsync();
        }
        connection = new NpgsqlConnectionStringBuilder(adminConnection)
        {
            Database = databaseName,
        }.ConnectionString;
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
        await db.Database.MigrateAsync();
        db.Tenants.AddRange(
            new Tenant
            {
                Id = TenantA,
                Name = "Tenant A",
                Code = "A",
            },
            new Tenant
            {
                Id = TenantB,
                Name = "Tenant B",
                Code = "B",
            }
        );
        db.Plants.AddRange(
            new Plant
            {
                Id = PlantA,
                TenantId = TenantA,
                Name = "Assigned A",
                Code = "A1",
            },
            new Plant
            {
                Id = PlantAOther,
                TenantId = TenantA,
                Name = "Other A",
                Code = "A2",
            },
            new Plant
            {
                Id = PlantB,
                TenantId = TenantB,
                Name = "Plant B",
                Code = "B1",
            }
        );
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        foreach (
            var user in new[]
            {
                new User
                {
                    Email = "platform@example.test",
                    Role = "PlatformAdmin",
                    DisplayName = "Platform",
                },
                new User
                {
                    Email = "admin@example.test",
                    Role = "TenantAdmin",
                    DisplayName = "Tenant admin",
                    TenantId = TenantA,
                },
                new User
                {
                    Email = "viewer@example.test",
                    Role = "Viewer",
                    DisplayName = "Viewer",
                    TenantId = TenantA,
                },
            }
        )
        {
            user.PasswordHash = hasher.HashPassword(user, Password);
            if (user.Role == "Viewer")
                user.Plants.Add(new UserPlant { UserId = user.Id, PlantId = PlantA });
            db.Users.Add(user);
        }
        await db.SaveChangesAsync();
    }

    public async Task<HttpClient> Login(string email)
    {
        var client = CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false,
            }
        );
        client.DefaultRequestHeaders.Add("X-GreenInk-Request", "1");
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = Password }
        );
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return client;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        if (string.IsNullOrEmpty(adminConnection))
            return;
        NpgsqlConnection.ClearAllPools();
        await using var admin = new NpgsqlConnection(adminConnection);
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)",
            admin
        );
        await drop.ExecuteNonQueryAsync();
    }
}

public class FoundationTests : IAsyncLifetime
{
    private readonly TestFactory factory = new();

    public Task InitializeAsync() => factory.InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)factory).DisposeAsync();

    [Fact]
    public async Task CookieAuthenticationAndRequestHeaderAreEnforced()
    {
        using var anonymous = factory.CreateClient();
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonymous.GetAsync("/api/tenants")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (
                await anonymous.PostAsJsonAsync(
                    "/api/auth/login",
                    new { email = "platform@example.test", password = TestFactory.Password }
                )
            ).StatusCode
        );
        anonymous.DefaultRequestHeaders.Add("X-GreenInk-Request", "1");
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (
                await anonymous.PostAsJsonAsync(
                    "/api/auth/login",
                    new { email = "platform@example.test", password = "wrong" }
                )
            ).StatusCode
        );
        using var client = await factory.Login("viewer@example.test");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
        client.DefaultRequestHeaders.Remove("X-GreenInk-Request");
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await client.PostAsync("/api/auth/logout", null)).StatusCode
        );
        client.DefaultRequestHeaders.Add("X-GreenInk-Request", "1");
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await client.PostAsync("/api/auth/logout", null)).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/auth/me")).StatusCode
        );
    }

    [Fact]
    public async Task LogoutRevokesCapturedCookieAndPasswordChangeRevokesEverySession()
    {
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false,
            }
        );
        client.DefaultRequestHeaders.Add("X-GreenInk-Request", "1");
        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "viewer@example.test", password = TestFactory.Password }
        );
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var cookie = login
            .Headers.GetValues("Set-Cookie")
            .Single(x => x.StartsWith("greenink.session="))
            .Split(';')[0];
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await client.PostAsync("/api/auth/logout", null)).StatusCode
        );
        using var replay = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = false,
            }
        );
        replay.DefaultRequestHeaders.Add("Cookie", cookie);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await replay.GetAsync("/api/auth/me")).StatusCode
        );
        using var first = await factory.Login("viewer@example.test");
        using var second = await factory.Login("viewer@example.test");
        var newPassword = "Aa1!" + Guid.NewGuid().ToString("N");
        var changed = await first.PostAsJsonAsync(
            "/api/auth/password",
            new { currentPassword = TestFactory.Password, newPassword }
        );
        Assert.True(changed.IsSuccessStatusCode, await changed.Content.ReadAsStringAsync());
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await first.GetAsync("/api/auth/me")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await second.GetAsync("/api/auth/me")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (
                await client.PostAsJsonAsync(
                    "/api/auth/login",
                    new { email = "viewer@example.test", password = TestFactory.Password }
                )
            ).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.OK,
            (
                await client.PostAsJsonAsync(
                    "/api/auth/login",
                    new { email = "viewer@example.test", password = newPassword }
                )
            ).StatusCode
        );
    }

    [Fact]
    public async Task TenantListsAndPlantSelectionAreScoped()
    {
        using var admin = await factory.Login("admin@example.test");
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await admin.GetAsync($"/api/dashboard?tenantId={factory.TenantB}")).StatusCode
        );
        var tenants = await admin.GetFromJsonAsync<JsonElement[]>("/api/tenants");
        Assert.Single(tenants!);
        Assert.Equal(factory.TenantA, tenants![0].GetProperty("id").GetGuid());
        var plants = await admin.GetFromJsonAsync<JsonElement[]>("/api/plants");
        Assert.Equal(2, plants!.Length);
        Assert.All(
            plants,
            plant => Assert.Equal(factory.TenantA, plant.GetProperty("tenantId").GetGuid())
        );
        using var platform = await factory.Login("platform@example.test");
        var scoped = await platform.GetFromJsonAsync<JsonElement>(
            $"/api/dashboard?tenantId={factory.TenantA}"
        );
        Assert.Equal(1, scoped.GetProperty("tenantCount").GetInt32());
        Assert.Equal(2, scoped.GetProperty("plantCount").GetInt32());
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (
                await platform.GetAsync(
                    $"/api/dashboard?tenantId={factory.TenantA}&plantId={factory.PlantB}"
                )
            ).StatusCode
        );
        using var viewer = await factory.Login("viewer@example.test");
        var assigned = await viewer.GetFromJsonAsync<JsonElement[]>("/api/plants");
        Assert.Single(assigned!);
        Assert.Equal(factory.PlantA, assigned![0].GetProperty("id").GetGuid());
        Assert.Equal(
            HttpStatusCode.OK,
            (await viewer.GetAsync($"/api/dashboard?plantId={factory.PlantA}")).StatusCode
        );
        foreach (var denied in new[] { factory.PlantAOther, factory.PlantB, Guid.NewGuid() })
            Assert.Equal(
                HttpStatusCode.Forbidden,
                (await viewer.GetAsync($"/api/dashboard?plantId={denied}")).StatusCode
            );
    }

    [Fact]
    public async Task RolesCannotEscalateOrAssignForeignPlants()
    {
        using var admin = await factory.Login("admin@example.test");
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (
                await admin.PostAsJsonAsync("/api/tenants", new { name = "Forbidden", code = "NO" })
            ).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (
                await admin.PostAsJsonAsync(
                    "/api/plants",
                    new
                    {
                        name = "Forbidden",
                        code = "NO",
                        tenantId = factory.TenantB,
                    }
                )
            ).StatusCode
        );
        object User(string role, Guid? tenant, Guid[] plants) =>
            new
            {
                email = "new@example.test",
                displayName = "New user",
                password = TestFactory.Password,
                role,
                tenantId = tenant,
                plantIds = plants,
            };
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await admin.PostAsJsonAsync("/api/users", User("PlatformAdmin", null, []))).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (
                await admin.PostAsJsonAsync("/api/users", User("Viewer", factory.TenantB, []))
            ).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (
                await admin.PostAsJsonAsync(
                    "/api/users",
                    User("Viewer", factory.TenantA, [factory.PlantB])
                )
            ).StatusCode
        );
        using var viewer = await factory.Login("viewer@example.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync("/api/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync("/api/audit")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (
                await viewer.PostAsJsonAsync("/api/users", User("TenantAdmin", factory.TenantA, []))
            ).StatusCode
        );
    }

    [Fact]
    public async Task ExistingSessionsImmediatelyLoseRevokedGrants()
    {
        using var viewer = await factory.Login("viewer@example.test");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            var user = await db
                .Users.Include(x => x.Plants)
                .SingleAsync(x => x.Email == "viewer@example.test");
            db.UserPlants.RemoveRange(user.Plants);
            await db.SaveChangesAsync();
        }
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await viewer.GetAsync($"/api/dashboard?plantId={factory.PlantA}")).StatusCode
        );
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
            db.Users.Remove(await db.Users.SingleAsync(x => x.Email == "viewer@example.test"));
            await db.SaveChangesAsync();
        }
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await viewer.GetAsync("/api/auth/me")).StatusCode
        );
    }

    [Fact]
    public async Task AccessAdministrationRejectsEscalationAndRefreshesSessionRole()
    {
        using var platform = await factory.Login("platform@example.test");
        using var admin = await factory.Login("admin@example.test");
        var users = await platform.GetFromJsonAsync<JsonElement[]>("/api/users");
        Guid Id(string email) =>
            users!
                .Single(x => x.GetProperty("email").GetString() == email)
                .GetProperty("id")
                .GetGuid();
        var viewerId = Id("viewer@example.test");
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (
                await admin.PutAsJsonAsync(
                    $"/api/users/{viewerId}/access",
                    new { role = "PlatformAdmin", plantIds = Array.Empty<Guid>() }
                )
            ).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.NotFound,
            (
                await admin.PutAsJsonAsync(
                    $"/api/users/{Id("platform@example.test")}/access",
                    new { role = "Viewer", plantIds = Array.Empty<Guid>() }
                )
            ).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (
                await admin.PutAsJsonAsync(
                    $"/api/users/{viewerId}/access",
                    new { role = "Viewer", plantIds = new[] { factory.PlantB } }
                )
            ).StatusCode
        );
        var changed = await platform.PutAsJsonAsync(
            $"/api/users/{Id("admin@example.test")}/access",
            new { role = "Viewer", plantIds = new[] { factory.PlantA } }
        );
        Assert.True(changed.IsSuccessStatusCode, await changed.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/users")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await admin.GetAsync($"/api/dashboard?plantId={factory.PlantAOther}")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.OK,
            (await admin.GetAsync($"/api/dashboard?plantId={factory.PlantA}")).StatusCode
        );
    }

    [Fact]
    public async Task ValidatedWritesPersistWithAuditAndConflictsDoNotPartiallyCommit()
    {
        using var admin = await factory.Login("admin@example.test");
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (
                await admin.PostAsJsonAsync(
                    "/api/plants",
                    new
                    {
                        name = "",
                        code = "bad code",
                        tenantId = factory.TenantA,
                    }
                )
            ).StatusCode
        );
        var code = "P" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        var request = new
        {
            name = "Persisted plant",
            code,
            tenantId = factory.TenantA,
        };
        var created = await admin.PostAsJsonAsync("/api/plants", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetGuid();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
        Assert.Equal(request.name, (await db.Plants.SingleAsync(x => x.Id == id)).Name);
        var audit = await db.AuditEvents.SingleAsync(x => x.EntityId == id);
        Assert.Equal("plant.created", audit.Action);
        Assert.Equal(factory.TenantA, audit.TenantId);
        var before = await db.AuditEvents.CountAsync();
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await admin.PostAsJsonAsync("/api/plants", request)).StatusCode
        );
        Assert.Equal(1, await db.Plants.CountAsync(x => x.Code == code));
        Assert.Equal(before, await db.AuditEvents.CountAsync());
        var visibleAudit = await admin.GetFromJsonAsync<JsonElement[]>("/api/audit");
        Assert.All(
            visibleAudit!,
            item => Assert.Equal(factory.TenantA, item.GetProperty("tenantId").GetGuid())
        );
    }
}
