# Implementation status

Updated: 2026-10-15. Milestone 1 core workflows are implemented and validated natively. The milestone remains open for complete Compose startup verification and remaining foundation review. This is an initial foundation, not a completed MES or production release.

## Implemented

- ASP.NET Core 10 API with EF Core/PostgreSQL, REST endpoints, Development OpenAPI, health endpoints and validation/problem responses.
- Password hashing, login throttling, eight-hour HTTP-only SameSite cookies, server-side session revocation, request-header CSRF protection, password change with transactional global logout and immediate permission reload.
- Tenant/plant creation and scoped lists; user creation and role/plant access editing; five built-in role policies and audited privileged changes. Platform administrator bootstrap requires private environment configuration.
- React/TypeScript/Vite/TanStack Query app with responsive light/dark shell, login/logout/password change, administration forms, role/access editor and audit view.
- Command center uses persisted organization/plant/user counts with authorized organization and plant context. Operational manufacturing KPIs are not invented.
- PostgreSQL migrations `20261015181109_Foundation` and `20261015181214_RevocableSessions`, indexes/foreign keys and transactional audit writes.
- Local Docker Compose, nginx, Dockerfiles, GitHub Actions API/frontend/PostgreSQL/browser jobs, requirements, architecture, API contract, backlog and Windows setup/recovery instructions.

Legacy root Node files are preserved. Their dependencies are not installed or included in the new API/web runtime. The uploaded master instruction is retained as `MASTER_DEVELOPMENT_INSTRUCTION.md`.

## Executed evidence

| Check | Result |
| --- | --- |
| `dotnet test tests/GreenInk.Api.Tests.csproj --configuration Release` | 7 passed, 0 failed, 0 skipped; clean build, real PostgreSQL 17; one isolated database per test |
| API security/persistence tests | Cookie/header enforcement; tenant/plant substitution; escalation and foreign assignments; live grant/account revocation; role editing; captured-cookie logout replay; password change revokes all sessions; persisted writes and unique-conflict audit rollback |
| Migrations | Both migrations applied repeatedly to fresh test databases and to the native application database |
| `npm --prefix apps/web run build` | TypeScript and Vite production build passed |
| `npm --prefix apps/web run test:e2e` | 1 passed against running API/PostgreSQL using system Chromium; real create organization/plant/user, access edit, audit, tenant/plant filter counts, restricted Viewer navigation, logout, password change and new-password login; desktop/mobile screenshots inspected |
| Native `/health/ready` | HTTP 200 with real PostgreSQL connectivity |
| API transitive dependency audit | No known vulnerable packages from configured NuGet sources after patch updates |
| Web dependency audit | 0 vulnerabilities reported in npm install/build validation |
| Docker PostgreSQL 17 | Container pulled, started and used for integration/native/browser validation |
| Web Docker image | Built successfully with optional proxy CA secret and production asset compilation |
| `docker compose ... config --quiet` | Passed with generated private validation variables |
| API Docker image / complete Compose startup | Blocked: Microsoft image registry redirected to `westcentralus.data.mcr.microsoft.com`, denied HTTP 403 by managed network policy. No proxy bypass attempted. Full Compose boot remains unverified |
| Logical backup/restore | pg_dump/pg_restore into a separate temporary database succeeded; tenant/plant/user/audit/migration counts matched (5/4/4/18/2 at snapshot). Recovered-application login and production recovery drill remain pending |
| `git diff --check` | Passed |
| Remote GitHub Actions | Configuration prepared; not remotely executed |

Cloud-only .NET SDK and writable tool caches were installed under `/tmp`. Browser CDN download was blocked, so Playwright used installed `/usr/bin/chromium`. Validation credentials are generated at runtime and are not tracked. E2E records are explicitly validation data in the development database, not production seed data.

## Known limitations and next work

1. Validate complete Compose build/start/restart on Docker Desktop or after the managed environment allows the Microsoft image CDN; verify API key persistence and session behavior across container recreation.
2. Built-in roles are implemented; custom permission/role definitions, OIDC/SSO, MFA, administrator password recovery, account suspension and tenant/plant edit/delete workflows remain backlog. PlantManager/Operator/Viewer currently differ only in future operational intent; all have explicitly scoped foundation reads.
3. Lists need pagination; grant updates need explicit optimistic concurrency handling. Add finer audit before/after details, session cleanup/retention, Data Protection key persistence/governance, proxy-aware authentication throttling, security headers and further accessibility/expiry/concurrency tests before release.
4. Production deployment requires TLS, restricted database identities, separately controlled migrations, secret management, logging/telemetry review, load testing and UAT. Development startup alone auto-applies migrations.
5. Inbound, inventory, staging, manufacturing, quality, finished goods, dispatch, traceability, printing, ERP and IoT workflows remain planned. Navigation entries explicitly display planned status. No live SAP, device, printer, messaging or object-store connection exists.

## Changes and continuity

Changes are prepared as uncommitted working-tree files for review; nothing is merged, pushed or externally deployed. Cloud files do not automatically update the Windows PC. Read `LOCAL_SETUP.md` for exact launch/test commands, `API_CONTRACT.md` for role scope, and `MODULE_ROADMAP.md` for next tasks. Complete remaining foundation gates before beginning Milestone 2 master data and inbound receiving.
