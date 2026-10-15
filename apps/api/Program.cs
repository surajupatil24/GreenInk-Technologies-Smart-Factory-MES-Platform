using System.Security.Claims;
using System.Threading.RateLimiting;
using GreenInk;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<FactoryDb>(o =>
    o.UseNpgsql(
        builder.Configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("ConnectionStrings__Database is required.")
    )
);
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder
    .Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.Name = "greenink.session";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = false;
        o.Events.OnRedirectToLogin = c =>
        {
            c.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        o.Events.OnRedirectToAccessDenied = c =>
        {
            c.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
        // Reload grants for every request: changed or deleted users cannot retain stale permissions.
        o.Events.OnValidatePrincipal = async c =>
        {
            var db = c.HttpContext.RequestServices.GetRequiredService<FactoryDb>();
            var id = c.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await db
                .Users.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id.ToString() == id);
            var sid = c.Principal?.FindFirstValue("session");
            if (
                user is null
                || !Guid.TryParse(sid, out var sessionId)
                || !await db.Sessions.AnyAsync(x =>
                    x.Id == sessionId && x.UserId == user.Id && x.ExpiresAt > DateTimeOffset.UtcNow
                )
            )
            {
                c.RejectPrincipal();
                return;
            }
            c.ReplacePrincipal(SessionPrincipal(user, sessionId));
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy(
        "login",
        context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }
            )
    );
});
var app = builder.Build();
app.UseExceptionHandler();
app.Use(
    async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["Cache-Control"] = "no-store";
        if (
            context.Request.Path.StartsWithSegments("/api")
            && !HttpMethods.IsGet(context.Request.Method)
            && !HttpMethods.IsHead(context.Request.Method)
            && context.Request.Headers["X-GreenInk-Request"] != "1"
        )
        {
            context.Response.StatusCode = 403;
            return;
        }
        try
        {
            await next();
        }
        catch (OpsError ex)
        {
            context.Response.StatusCode = ex.Status;
            await context.Response.WriteAsJsonAsync(new { detail = ex.Message });
        }
        catch (InvalidOperationException ex)
            when (ex.GetBaseException()
                    is PostgresException
                    {
                        SqlState: PostgresErrorCodes.SerializationFailure
                            or PostgresErrorCodes.DeadlockDetected
                    }
            )
        {
            context.Response.StatusCode = 409;
            await context.Response.WriteAsJsonAsync(
                new
                {
                    detail = "Concurrent stock transaction. Refresh and retry with the same commandId.",
                }
            );
        }
        catch (DbUpdateConcurrencyException)
        {
            context.Response.StatusCode = 409;
            await context.Response.WriteAsJsonAsync(
                new
                {
                    detail = "Stock or workflow changed. Refresh and retry with the same commandId.",
                }
            );
        }
        catch (PostgresException ex)
            when (ex.SqlState
                    is PostgresErrorCodes.SerializationFailure
                        or PostgresErrorCodes.DeadlockDetected
            )
        {
            context.Response.StatusCode = 409;
            await context.Response.WriteAsJsonAsync(
                new
                {
                    detail = "Concurrent transaction. Refresh and retry with the same commandId.",
                }
            );
        }
        catch (DbUpdateException ex)
            when (ex.InnerException
                    is PostgresException
                    {
                        SqlState: PostgresErrorCodes.SerializationFailure
                            or PostgresErrorCodes.DeadlockDetected
                    }
            )
        {
            context.Response.StatusCode = 409;
            await context.Response.WriteAsJsonAsync(
                new
                {
                    detail = "Concurrent transaction. Refresh and retry with the same commandId.",
                }
            );
        }
        catch (DbUpdateException ex)
            when (ex.InnerException
                    is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
            )
        {
            context.Response.StatusCode = 409;
            await context.Response.WriteAsJsonAsync(
                new { detail = "This code, reference or command is already in use." }
            );
        }
    }
);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet(
    "/health/ready",
    async (FactoryDb db) =>
        await db.Database.CanConnectAsync()
            ? Results.Ok(new { status = "ready" })
            : Results.StatusCode(503)
);
app.MapPost(
        "/api/auth/login",
        async (
            LoginRequest request,
            FactoryDb db,
            IPasswordHasher<User> hasher,
            HttpContext context
        ) =>
        {
            if (
                string.IsNullOrWhiteSpace(request.Email)
                || string.IsNullOrEmpty(request.Password)
                || request.Password.Length > 128
            )
                return Results.Unauthorized();
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await db
                .Users.Include(x => x.Plants)
                .SingleOrDefaultAsync(x => x.Email == email);
            // Always run PBKDF2, including unknown accounts, to reduce account timing disclosure.
            var dummy = new User();
            var hash = user?.PasswordHash ?? hasher.HashPassword(dummy, "timing-only-placeholder");
            var result = hasher.VerifyHashedPassword(user ?? dummy, hash, request.Password);
            if (user is null || result == PasswordVerificationResult.Failed)
                return Results.Unauthorized();
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = hasher.HashPassword(user, request.Password);
                await db.SaveChangesAsync();
            }
            var session = new UserSession
            {
                UserId = user.Id,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(8),
            };
            db.Sessions.Add(session);
            await db.SaveChangesAsync();
            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                SessionPrincipal(user, session.Id)
            );
            return Results.Ok(UserView(user));
        }
    )
    .RequireRateLimiting("login");
app.MapPost(
        "/api/auth/logout",
        async (HttpContext c, FactoryDb db) =>
        {
            var sid = Guid.Parse(c.User.FindFirstValue("session")!);
            await db.Sessions.Where(x => x.Id == sid).ExecuteDeleteAsync();
            await c.SignOutAsync();
            return Results.NoContent();
        }
    )
    .RequireAuthorization();
app.MapPost(
        "/api/auth/password",
        async (
            PasswordRequest r,
            FactoryDb db,
            ClaimsPrincipal p,
            IPasswordHasher<User> hasher,
            HttpContext c
        ) =>
        {
            var u = await CurrentUser(db, p);
            if (
                !Rules.ValidPassword(r.NewPassword)
                || string.IsNullOrEmpty(r.CurrentPassword)
                || r.CurrentPassword.Length > 128
            )
                return Invalid(
                    "Provide the current password and a new password with at least 14 characters, upper/lower case and a digit."
                );
            if (
                hasher.VerifyHashedPassword(u, u.PasswordHash, r.CurrentPassword)
                == PasswordVerificationResult.Failed
            )
                return Results.Unauthorized();
            await using var transaction = await db.Database.BeginTransactionAsync();
            u.PasswordHash = hasher.HashPassword(u, r.NewPassword);
            Audit(db, p, u.TenantId, "user.password.changed", u.Id);
            await db.SaveChangesAsync();
            await db.Sessions.Where(x => x.UserId == u.Id).ExecuteDeleteAsync();
            await transaction.CommitAsync();
            await c.SignOutAsync();
            return Results.NoContent();
        }
    )
    .RequireAuthorization()
    .RequireRateLimiting("login");
app.MapGet(
        "/api/auth/me",
        async (FactoryDb db, ClaimsPrincipal p) => Results.Ok(UserView(await CurrentUser(db, p)))
    )
    .RequireAuthorization();
app.MapGet("/api/roles", () => Results.Ok(Rules.Roles)).RequireAuthorization();
app.MapGet(
        "/api/tenants",
        async (FactoryDb db, ClaimsPrincipal p) =>
        {
            var u = await CurrentUser(db, p);
            return Results.Ok(
                await db
                    .Tenants.Where(x => u.Role == "PlatformAdmin" || x.Id == u.TenantId)
                    .OrderBy(x => x.Name)
                    .ToListAsync()
            );
        }
    )
    .RequireAuthorization();
app.MapPost(
        "/api/tenants",
        async (TenantRequest r, FactoryDb db, ClaimsPrincipal p) =>
        {
            if (!Rules.ValidName(r.Name) || !Rules.ValidCode(r.Code))
                return Invalid(
                    "Name (1–160 characters) and alphanumeric code (1–32 characters) are required."
                );
            var t = new Tenant { Name = r.Name.Trim(), Code = r.Code.ToUpperInvariant() };
            db.Tenants.Add(t);
            Audit(db, p, null, "tenant.created", t.Id);
            await db.SaveChangesAsync();
            return Results.Created($"/api/tenants/{t.Id}", t);
        }
    )
    .RequireAuthorization(p => p.RequireRole("PlatformAdmin"));
app.MapGet(
        "/api/plants",
        async (FactoryDb db, ClaimsPrincipal p) =>
        {
            var u = await CurrentUser(db, p);
            return Results.Ok(
                await VisiblePlants(db, u)
                    .OrderBy(x => x.Name)
                    .Select(x => new
                    {
                        x.Id,
                        x.TenantId,
                        x.Name,
                        x.Code,
                    })
                    .ToListAsync()
            );
        }
    )
    .RequireAuthorization();
app.MapPost(
        "/api/plants",
        async (PlantRequest r, FactoryDb db, ClaimsPrincipal p) =>
        {
            var u = await CurrentUser(db, p);
            if (u.Role != "PlatformAdmin" && u.TenantId != r.TenantId)
                return Results.Forbid();
            if (
                !Rules.ValidName(r.Name)
                || !Rules.ValidCode(r.Code)
                || !await db.Tenants.AnyAsync(x => x.Id == r.TenantId)
            )
                return Invalid("A valid tenant, name and code are required.");
            var plant = new Plant
            {
                TenantId = r.TenantId,
                Name = r.Name.Trim(),
                Code = r.Code.ToUpperInvariant(),
            };
            db.Plants.Add(plant);
            Audit(db, p, r.TenantId, "plant.created", plant.Id);
            await db.SaveChangesAsync();
            return Results.Created(
                $"/api/plants/{plant.Id}",
                new
                {
                    plant.Id,
                    plant.TenantId,
                    plant.Name,
                    plant.Code,
                }
            );
        }
    )
    .RequireAuthorization(p => p.RequireRole("PlatformAdmin", "TenantAdmin"));
app.MapGet(
        "/api/users",
        async (FactoryDb db, ClaimsPrincipal p) =>
        {
            var u = await CurrentUser(db, p);
            var users = await db
                .Users.Include(x => x.Plants)
                .Where(x => u.Role == "PlatformAdmin" || x.TenantId == u.TenantId)
                .OrderBy(x => x.Email)
                .ToListAsync();
            return Results.Ok(users.Select(UserView));
        }
    )
    .RequireAuthorization(p => p.RequireRole("PlatformAdmin", "TenantAdmin"));
app.MapPost(
        "/api/users",
        async (UserRequest r, FactoryDb db, ClaimsPrincipal p, IPasswordHasher<User> hasher) =>
        {
            var actor = await CurrentUser(db, p);
            if (
                actor.Role != "PlatformAdmin"
                && (r.TenantId != actor.TenantId || r.Role == "PlatformAdmin")
            )
                return Results.Forbid();
            if (
                !Rules.Roles.Contains(r.Role)
                || !Rules.ValidName(r.DisplayName)
                || !Rules.ValidPassword(r.Password)
                || string.IsNullOrWhiteSpace(r.Email)
                || r.Email.Length > 254
                || !System.Net.Mail.MailAddress.TryCreate(r.Email, out var mail)
                || mail.Address != r.Email
            )
                return Invalid(
                    "Provide a valid email, name, role and password (14–128 characters with upper case, lower case and a digit)."
                );
            if (
                (r.Role == "PlatformAdmin" && r.TenantId != null)
                || (
                    r.Role != "PlatformAdmin"
                    && (r.TenantId == null || !await db.Tenants.AnyAsync(x => x.Id == r.TenantId))
                )
            )
                return Invalid(
                    "Platform admins have no tenant; other roles require an existing tenant."
                );
            var ids = (r.PlantIds ?? []).Distinct().ToArray();
            if (r.Role == "PlatformAdmin" && ids.Length > 0)
                return Invalid("Platform admins cannot have tenant plant assignments.");
            if (
                await db.Plants.CountAsync(x => ids.Contains(x.Id) && x.TenantId == r.TenantId)
                != ids.Length
            )
                return Invalid("Every assigned plant must belong to the user's tenant.");
            var user = new User
            {
                Email = r.Email.Trim().ToLowerInvariant(),
                DisplayName = r.DisplayName.Trim(),
                Role = r.Role,
                TenantId = r.TenantId,
            };
            user.PasswordHash = hasher.HashPassword(user, r.Password);
            user.Plants = ids.Select(id => new UserPlant { UserId = user.Id, PlantId = id })
                .ToList();
            db.Users.Add(user);
            Audit(db, p, r.TenantId, "user.created", user.Id);
            await db.SaveChangesAsync();
            return Results.Created($"/api/users/{user.Id}", UserView(user));
        }
    )
    .RequireAuthorization(p => p.RequireRole("PlatformAdmin", "TenantAdmin"));
app.MapPut(
        "/api/users/{id:guid}/access",
        async (Guid id, AccessRequest r, FactoryDb db, ClaimsPrincipal p) =>
        {
            var actor = await CurrentUser(db, p);
            var target = await db
                .Users.Include(x => x.Plants)
                .SingleOrDefaultAsync(x => x.Id == id);
            if (
                target is null
                || (actor.Role != "PlatformAdmin" && target.TenantId != actor.TenantId)
            )
                return Results.NotFound();
            if (target.Role == "PlatformAdmin" || r.Role == "PlatformAdmin")
                return Results.Forbid();
            if (!Rules.Roles.Contains(r.Role))
                return Invalid("Choose an existing tenant role.");
            var ids = (r.PlantIds ?? []).Distinct().ToArray();
            if (
                await db.Plants.CountAsync(x => ids.Contains(x.Id) && x.TenantId == target.TenantId)
                != ids.Length
            )
                return Invalid("Every plant must belong to the user's tenant.");
            target.Role = r.Role;
            db.UserPlants.RemoveRange(target.Plants.Where(x => !ids.Contains(x.PlantId)));
            foreach (var plantId in ids.Except(target.Plants.Select(x => x.PlantId)))
                db.UserPlants.Add(new UserPlant { UserId = id, PlantId = plantId });
            Audit(db, p, target.TenantId, "user.access.updated", id);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }
    )
    .RequireAuthorization(p => p.RequireRole("PlatformAdmin", "TenantAdmin"));
app.MapGet(
        "/api/dashboard",
        async (Guid? plantId, Guid? tenantId, FactoryDb db, ClaimsPrincipal p) =>
        {
            var u = await CurrentUser(db, p);
            if (
                tenantId.HasValue
                && !await db.Tenants.AnyAsync(x =>
                    x.Id == tenantId && (u.Role == "PlatformAdmin" || x.Id == u.TenantId)
                )
            )
                return Results.Forbid();
            var plants = VisiblePlants(db, u)
                .Where(x => !tenantId.HasValue || x.TenantId == tenantId);
            if (plantId.HasValue && !await plants.AnyAsync(x => x.Id == plantId))
                return Results.Forbid();
            var plantName = plantId.HasValue
                ? await plants.Where(x => x.Id == plantId).Select(x => x.Name).SingleAsync()
                : "All authorized plants";
            var tenantCount = await db.Tenants.CountAsync(x =>
                (u.Role == "PlatformAdmin" || x.Id == u.TenantId)
                && (!tenantId.HasValue || x.Id == tenantId)
            );
            var userCount = u.Role is "PlatformAdmin" or "TenantAdmin"
                ? await db.Users.CountAsync(x =>
                    (u.Role == "PlatformAdmin" || x.TenantId == u.TenantId)
                    && (!tenantId.HasValue || x.TenantId == tenantId)
                )
                : 1;
            return Results.Ok(
                new
                {
                    tenantCount,
                    plantCount = await plants.CountAsync(),
                    userCount,
                    plantName,
                }
            );
        }
    )
    .RequireAuthorization();
app.MapGet(
        "/api/audit",
        async (FactoryDb db, ClaimsPrincipal p) =>
        {
            var u = await CurrentUser(db, p);
            return Results.Ok(
                await db
                    .AuditEvents.Where(x => u.Role == "PlatformAdmin" || x.TenantId == u.TenantId)
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(100)
                    .ToListAsync()
            );
        }
    )
    .RequireAuthorization(p => p.RequireRole("PlatformAdmin", "TenantAdmin"));
if (!app.Environment.IsEnvironment("Testing"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<FactoryDb>();
    if (app.Environment.IsDevelopment())
        await db.Database.MigrateAsync();
    var email = app.Configuration["Bootstrap:Email"];
    var password = app.Configuration["Bootstrap:Password"];
    if (!await db.Users.AnyAsync())
    {
        if (
            string.IsNullOrWhiteSpace(email)
            || !System.Net.Mail.MailAddress.TryCreate(email, out _)
            || !Rules.ValidPassword(password)
        )
            throw new InvalidOperationException(
                "First startup requires Bootstrap__Email and Bootstrap__Password (14+ characters, upper/lower case and digit)."
            );
        var user = new User
        {
            Email = email.Trim().ToLowerInvariant(),
            DisplayName = app.Configuration["Bootstrap:DisplayName"] ?? "Platform administrator",
            Role = "PlatformAdmin",
        };
        user.PasswordHash = scope
            .ServiceProvider.GetRequiredService<IPasswordHasher<User>>()
            .HashPassword(user, password!);
        db.Users.Add(user);
        db.AuditEvents.Add(
            new AuditEvent
            {
                ActorId = user.Id,
                Action = "bootstrap.created",
                EntityId = user.Id,
            }
        );
        await db.SaveChangesAsync();
    }
}
app.MapInboundInventory();
app.MapExecution();
app.MapInsights();
app.Run();
static ClaimsPrincipal SessionPrincipal(User u, Guid sessionId) =>
    new(
        new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()),
                new Claim(ClaimTypes.Role, u.Role),
                new Claim("session", sessionId.ToString()),
            },
            CookieAuthenticationDefaults.AuthenticationScheme
        )
    );
static async Task<User> CurrentUser(FactoryDb db, ClaimsPrincipal p) =>
    await db
        .Users.Include(x => x.Plants)
        .SingleAsync(x => x.Id == Guid.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!));
static IQueryable<Plant> VisiblePlants(FactoryDb db, User u) =>
    db.Plants.Where(x =>
        u.Role == "PlatformAdmin"
        || (
            x.TenantId == u.TenantId
            && (u.Role == "TenantAdmin" || u.Plants.Select(a => a.PlantId).Contains(x.Id))
        )
    );
static object UserView(User u) =>
    new
    {
        u.Id,
        u.Email,
        u.DisplayName,
        u.Role,
        u.TenantId,
        plantIds = u.Plants.Select(x => x.PlantId).ToArray(),
    };
static IResult Invalid(string detail) => Results.Problem(detail: detail, statusCode: 400);
static void Audit(FactoryDb db, ClaimsPrincipal p, Guid? tenantId, string action, Guid entityId) =>
    db.AuditEvents.Add(
        new AuditEvent
        {
            ActorId = Guid.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!),
            TenantId = tenantId,
            Action = action,
            EntityId = entityId,
        }
    );

public partial class Program { }
