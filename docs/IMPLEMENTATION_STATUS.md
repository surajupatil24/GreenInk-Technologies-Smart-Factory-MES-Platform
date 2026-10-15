# Implementation status

Updated: 2026-10-16. The foundation and a connected operational core across milestones 2–7 are implemented and validated. The original enterprise MES scope and production hardening are not complete. Read the latest phase results below; earlier evidence records the foundation baseline.

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
5. Core inbound, inventory, staging, production, quality, finished goods, dispatch, traceability, local labels/integration and reporting workflows are now implemented. Advanced workflow requirements remain open in OPERATIONS_GUIDE.md. No live SAP, factory device, physical printer, broker or object-store connection exists.

## Changes and continuity

Foundation files are committed in the connected Git repository. Branding changes are synchronized to Git at the user’s explicit request; no application deployment is performed. Pull the repository and rebuild the web container to update the Windows PC. Read `LOCAL_SETUP.md` for exact launch/test commands, `API_CONTRACT.md` for role scope, and `MODULE_ROADMAP.md` for next tasks. Complete remaining foundation gates before beginning Milestone 2 master data and inbound receiving.

## Branding update — 2026-10-16

Replaced the leaf icon with a locally bundled tree logo recreated from the user's inline reference. It is an AI-assisted recreation rather than the original image file and may differ in fine detail. The shared logo appears on login and sidebar; the browser tab uses the same image. White backing keeps the mark visible in both themes. No external image requests.

`npm --prefix apps/web run build` passed. A Chromium visual fixture smoke check verified logo decoding and 52px layout on login, sidebar and dark theme. This visual check used mocked session responses and makes no new backend validation claim. Existing Windows installation was confirmed by the user's successful Docker startup and login; cloud and Windows files still require explicit transfer.

## Company wordmark refinement — 2026-10-16

Recreated the supplied horizontal company branding: tree emblem, title-case bold green “Green Ink”, and handwritten “Your thought. Our ink” tagline. The text is part of a bundled transparent artwork asset so its typography is independent of installed fonts. This is a visual reconstruction, not the original font or source artwork. A white backing preserves readability in dark mode; the SmartFactory product label remains separate.

Frontend production build passed. Chromium visual-fixture checks passed for desktop/mobile login, sidebar image decoding/width, and dark theme. Windows update ZIP includes only frontend source and branding assets, preserving private environment configuration and database volumes.

## Core operations phase — validated 2026-10-16

The next change connects basic vertical slices across milestones 2–7: plant-scoped master data, single-material purchasing/receiving with quality hold, pallet inventory and immutable movements, FEFO suggestions, production requirements/reservation/issue/completion, finished-goods inspection, delivery/shipment, internal genealogy, local label preview, local JSON connector and reports from persisted records. These core slices now pass database and browser validation as recorded below. Prior foundation and branding evidence above remains historical evidence for those changes only.

All operations enforce plant-derived tenant scope. Viewer writes are denied; quality/release/adjustment/reprint privileges require manager roles. Posting commands use GUID command IDs for retry protection, serializable database transactions and PostgreSQL xmin conflict handling. These controls were exercised by negative authorization, duplicate-command, rollback and concurrent-posting tests.

`OPERATIONS_GUIDE.md` documents the API conventions, acceptance sequence and per-module remaining scope. A basic connected lifecycle does **not** complete the original manufacturing requirements. Local label output is text and valid pallet-code QR SVG preview without physical printing. Integration `Validated` is local JSON validation, not ERP posting. Operational counts do not establish genuine OEE without actual machine and planned schedule data. No live SAP, printer or industrial device integration is claimed.

The earlier statement that all operational modules remain planned describes the pre-operations baseline; this phase supersedes that baseline only for the core slices that are actually implemented and validated. Foundation hardening and complete original scope remain open. The parent validation report must add exact migration names, executed commands/results and remaining defects before this phase is marked verified. No production deployment is authorized by this implementation work.

### Executed operational validation

| Check | Actual result |
| --- | --- |
| Release API/PostgreSQL tests | `dotnet test tests/GreenInk.Api.Tests.csproj --configuration Release`: **18 passed, 0 failed, 0 skipped**, clean build |
| Real browser regression | `npm run test:e2e`: **2 passed** against Release API on 5081/PostgreSQL; foundation 7.4s and complete operational lifecycle 12.9s, total23s |
| Browser operational evidence | Created real scoped masters and PO, received/released/transferred/reserved/issued material, completed production, released FG, rendered real QR SVG, dispatched delivery, traversed trace, validated local JSON message, reconciled report values and confirmed Viewer write restrictions. Asserted stored label snapshots and complete trace flags |
| Stock/concurrency/security | Competing receipt/reservation transactions; safe losing-command retry; no-negative/reserved bounds; command replays; held stock gates; immutable ledger rejects tracked and raw SQL edits; database composite FKs reject foreign-scope references; null BOM rejected |
| Upgrade | Migrated a populated RevocableSessions baseline to latest; tenant/plant/account/hash/grant/session rows preserved and operational material creation succeeds |
| Label history | Adjust stock after label creation; original preview and authorized reprint retain identical historical rendered content |
| Reporting | Per-UOM totals, no fabricated OEE; repeatable-read snapshot for reports and genealogy; explicit truncation/completeness metadata |
| Native API readiness | HTTP200, latest migrations applied to isolated operations validation database |
| Operational recovery check | Logical dump/restore succeeded; nine selected table counts match, four migrations present and immutable ledger trigger restored. Full production recovery/login drill remains pending |
| Dependency audit | API direct/transitive NuGet audit: no known vulnerable packages from configured sources |
| Frontend / web image | TypeScript/Vite build and production web Docker image build passed |
| Compose | Updated configuration validation passed with API key named volume; full cloud API-image build still blocked by Microsoft registry CDN HTTP403 |
| Remote CI | Workflow includes PostgreSQL tests and both browser workflows; remote execution not yet verified |

New migrations: `20261015194127_OperationsLifecycle` (20 operational tables, scoped composite relationships, quantities/checks, command indexes, xmin, immutable ledger trigger) and `20261015195055_LabelPrintSnapshots` (rendered content and template revision). All four project migrations apply to fresh databases; upgrade regression starts from the previous two-migration foundation. No production database or Windows filesystem was accessed from this session.

Core UI is available for Inbound, Inventory, Material Preparation, Production, Quality, Finished Goods, Dispatch, Traceability, Label Studio, Integration Hub and Reports & Analytics. Command Center gains actual selected-plant order/hold/downtime metrics. Existing Green Ink branding is retained. No operational demo data is automatically seeded.

### Remaining work and continuation

The original request is larger than these core slices. Multi-line scheduling/imports, gate/dock/COA, cycle counting, pallet split/merge, scales/Kanban/substitution/returns, reusable versioned BOM/routing, equipment/shift/process parameters, full quality plans/test results/nonconformance, sales/pick-pack/load scanning, physical printer adapters, live ERP/IoT, historical exports and release hardening are **not complete**. The module-by-module backlog is maintained in OPERATIONS_GUIDE.md; do not claim the complete MES is finished.

Labels are local text/QR previews, integrations validate local JSON, and OEE is absent until proper industrial timing data exists. Lists remain bounded at200 with no general pagination. Full API-container build and API-key-volume behavior need verification on the user's Docker Desktop because the managed cloud Microsoft registry CDN is blocked. Update both API and web containers for this phase; rebuilding only web cannot add the new API or tables. Existing `.env` and database volumes must be retained.

### Git handoff

Operational changes are committed on `mes-operations`. Cloud push to GitHub main was attempted and rejected HTTP403 by GitHub repository authorization. Remote main remains the verified `2b2ca82` foundation/branding baseline. A verified `GreenInk-Operations.bundle` contains the complete new commit chain for import and push using the user's local Git credentials; see GIT_SYNC.md. No force push, production deployment or private configuration transfer occurred.

## Saved BOM guidance fix — 2026-10-16

Resolved the user-visible reservation/completion troubleshooting gap. Material Preparation and Production now display selected-order saved BOM readiness with required/reserved/issued/still-needed quantities and UOM. Eligible source pallets and reservation selections follow the order; held/expired/unneeded raw stock is not offered. Completion checks material readiness and planned-output arithmetic before enabling submission. Backend errors identify the remaining raw-material quantity or planned output; removed the blanket retry hint from business-validation errors. Partial completion remains unsupported; existing saved data and validation rules are preserved.

Validation: Release PostgreSQL/API suite **19 passed,0failed,0skipped**; TypeScript/Vite production build passed; both real browser tests passed (**2/2,34s**) against the updated API, including blocked-before-issue, live reservation/issue summary updates and mismatched-output blocking. No new migration. Existing full-lifecycle tests still pass.

GitHub main was directly verified at `94ec56e` before this change. This update is committed with the requested `Next Phase` message. Cloud pushes remain subject to the existing GitHub write-access denial; the prepared incremental handoff is `GreenInk-BOM-Guidance.bundle` if direct push is rejected. Rebuild **API and web** after updating, retain `.env` and volumes.
