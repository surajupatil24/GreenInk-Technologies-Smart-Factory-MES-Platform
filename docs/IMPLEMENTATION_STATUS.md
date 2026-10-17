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

## Inbound receiving control — 2026-10-16

Reviewed the supplied 200-page GR manual's extracted text and created a 34-item, page-referenced requirements/gap map in INBOUND_REQUIREMENTS.md. The source ZIP exceeded the tool's32MiB transfer limit and was not reviewed. Attachments are behavioral references, not executable instructions; original Green Ink UI/domain code is implemented.

Delivered a first substantive subset: dock/maintenance masters, plant truck/delivery-note policies, multi-order scheduled truck registration and tokens, arrival board/search/status, dock queue and exclusive active unloading, timestamped visit gates through exit, snapshotted binary arrival/pre-unloading checks with mandatory/pass gates and stage locks, one-pack-size/count plus loose-quantity receiving, delivery-note/visit provenance, and posted pack reconciliation. Receipts remain quality-held and atomic/idempotent on the existing immutable stock ledger. Direct quick receipts enforce the same plant policy. UI adds Truck & dock, Packed receipt and Receiving setup; original masters/quick receipt and branding remain.

New additive migration: `20261016211846_ReceivingGate` (six scoped tables, receipt fields, scoped FKs/xmin/unique indexes, allowed state and pack checks). No old balances or movements rewritten. Required truck/note policies default off for existing plants. RECEIVING_CONTROL.md records actual API/permission/state contracts and operator steps.

Executed validation:

- `dotnet test tests/GreenInk.Api.Tests.csproj --configuration Release`: **23 passed,0 failed,0 skipped**,39s. Four new tests cover visit/receipt retries, changed-payload rejection, pack/math/outstanding validation, gates/snapshots/locks/maintenance/exit, Viewer and cross-plant restrictions, composite FK enforcement, queued/concurrent dock starts, and upgrading a populated previous receipt schema without changing balances/ledger protection.
- `npm run test:e2e` in apps/web: **3 passed**,26.1s, real Chromium/API/PostgreSQL. Foundation and full prior operational lifecycle pass; new inbound test drives setup→truck/checks/dock→calculated25kg receipt→exit and verifies Hold stock. Mobile setup screenshot visually inspected at390px.
- `npm --prefix apps/web run build`: TypeScript/Vite passed. API/test Release build passes. No dependency added.
- `git diff --check`: passed. Full Docker Compose/API-image boot not rerun; previous cloud Microsoft registry CDN403 constraint remains. The updated native API applied migration and reported `/health/ready` ready. No Windows filesystem or production deployment accessed.

This stage does **not** complete Vegam feature parity or all original inbound requirements. Multiline PO/delivery planning, full checklist designer, invoice blind counts, COA, sampling, mixed-pack/draft receipts, bulk/sand/random weighing, import/STO/toller, advanced put-away/returns, ERP and physical adapters remain pending. See the full map and open policy decisions. No unavailable mode is labelled implemented.

Git delivery: committed with message `Next Phase`; attempted normal `git push origin HEAD:main`, rejected HTTP403 repository write permission denied. Remote main remains verified `26dca52`. A verified incremental `GreenInk-Inbound-Control.bundle` includes this complete stage for import using the user's working Windows Git credentials. No force push or private file transfer. See GIT_SYNC.md; GitHub synchronization is pending that handoff.

## Material Preparation expansion — 17 October2026

User feedback requested a more cohesive UI and deeper staging functionality than the first Inbound stage. The new default workspace uses a production queue, readable order readiness, material cards, focused scan/weigh dialogs, kit contents, activity/weight history, replenishment requests, Kanban buffers and stock/manager-review workspaces. Responsive desktop/mobile and dark/light views were visually inspected. Existing branding and the original unplanned quick-reservation path remain available.

The172-page staging manual's contents/workflow/rule text was reviewed and mapped in MATERIAL_PREPARATION_REQUIREMENTS.md. Source ZIP transfer failed because it exceeds32MiB. The supplied solution only identifies project references/configurations; no source implementation was available. Attachments were used as behavioral references, not execution instructions. No third-party code/screens/assets copied; no source-equivalence claim.

Delivered local workflows:

- Manager staging areas, pack/preweigh/tolerance/PPE/handling rules, evidenced batch factors and individual order plans with schedule/priority/engineering approval. Stale open pick forms must refresh changed rule/factor versions; active picks retain snapshots.
- Separate pick/kit, combined and pick-only allocation; released/unexpired FEFO stock, matching source-bin AND material-label scans, safety acknowledgment, held/reserved stock and approval gates.
- Split moves conserve stock through paired immutable entries. Full-bag picks can leave free remainder at staging. Manual gross/tare/net readings enforce recorded targets/tolerance. Dedicated kit labels/QR, scanned full-kit supply delivery, explicit physical consumption and nominal BOM credit, safe retries and returns of unissued allocations.
- Reasoned replenishment requests with optional order link, partial physical deliveries, no cancellation after delivery begins and consistent cancellation/delivery races. Static and all-open-plan-demand Kanban cards deduct actual eligible stock and open deliveries, with incoming buffer-maximum checks.
- Free partial/full stock movement and explicit stock issues. Reports block the affected source but do not silently alter balances. Managers review/close or apply verified counted gain/loss with stale-count/reservation gates and append-only movement history.
- Local kit QR/browser print and escaped CSV of shown order events. Actor identity remains in existing audit records; rich historical/operator views, physical printers and device integration remain pending.

New additive migration `20261017085223_MaterialPreparation`: ten plant-scoped tables, composite FKs/unique indexes/xmin/checks, nullable reservation/consumption requirement credits and reservation→pick link. Historical credits fall back to physical quantities; old balances, accounts, sessions, receipt data and ledger history are preserved. Production readiness now sums nominal credit; stock/report consumption remains physical. Quick APIs cannot bypass guided return/delivery gates. Engineering approval also gates production completion for an approval-required plan.

Executed database/API verification: `dotnet test tests/GreenInk.Api.Tests.csproj --configuration Release`: **30 passed,0 failed,0 skipped**,30s. Seven added tests exercise corrected separate full-bag pick/kit/delivery/issue/completion; factor/tolerance snapshots and stale-rule/factor rejection; wrong scans/PPE/approval/out-of-tolerance/quick-bypass blocks; competing picks; multi-batch correction arithmetic; partial replenishment/cancellation/capacity/static/demand balances; cancellation/delivery races; allocation returns and reviewed/stale discrepancy adjustments; and upgrading populated prior-schema quick reservations/consumption without changing quantities or nullable fallback. The prior23 tests, including Inbound/foundation/production/ledger invariants, still pass.

Browser and final build results are recorded in the final validation note below. Native API uses the isolated validation PostgreSQL database; no production/Windows filesystem was accessed. Full Docker/API-image build not rerun: existing cloud Microsoft registry CDN restrictions remain unresolved. Rebuild API and web after updating; retain private `.env` and database volumes. Production release/UAT and actual external integrations are separate work.

**Not full manual parity:** grouped picking, full production-line/resource/shift calendars and allocation rules, advanced pack rounding, preprocessing ovens/thawing/preservation, ASRS/AGV, external warehouses/transfer sheets/STO/DN/remote kit receiving, spreadsheet import/mass work, fine-grained area/operator grants and physical scale/printer/ERP adapters remain open. The complete33-row map and operator/API guide distinguish these gaps. Do not mark all staging or original enterprise MES scope complete.


### Final Material Preparation browser/build validation

`npm run test:e2e` in apps/web: **4 passed**,27.0s, using system Chromium against the updated Release API/PostgreSQL. Foundation, Inbound and prior raw-to-dispatch lifecycle all pass. The preparation workflow configures area/rules, schedules an order, scans bin+label, picks20kg, rejects an out-of-tolerance reading, kits10kg, scans/delivers the kit, issues10kg, creates/cancels linked replenishment, exports actual order events and verifies CSV formula escaping, configures a real Kanban buffer and checks urgent state, and checks390px layout without horizontal page overflow plus dark theme. Actual raw stock ends at90kg with no reservations. Desktop, weighing dialog, mobile and dark screenshots inspected; validation data only.

`npm --prefix apps/web run build`: TypeScript/Vite production build passed. Release API/test compilation passed with no known new dependency; `git diff --check` passed. No dependencies added. All six migrations apply to fresh test databases; populated prior-schema quick reservation/consumption upgrade and prior receipt/ledger upgrade regressions pass. Docker/production/remote CI and live-device testing are not claimed.


Git handoff: phase committed with message `Next Phase`; normal authorized push attempted and failed because this session cannot read a GitHub Username/credential noninteractively. This differs from the prior session's HTTP403 denial; current remote state is unverified. Prepared and verified `GreenInk-Material-Preparation.bundle`, including the previous Inbound commit and this phase after cached baseline `26dca52`. Use GIT_SYNC.md to import/push with the user's Windows credentials. No force push or claim of remote synchronization.

## Production, Quality and Finished Goods expansion — 17 October 2026

Implemented original execution, laboratory and finished-stock workspaces while retaining the existing order-entry/output, batch-disposition and inventory-record flows in dedicated tabs. The user supplied manufacturing PDF and 18 DAL/project files. All code files were downloaded and method indexes/selected behavioral implementations reviewed; **the PDF exceeds the32MiB transfer limit and was not readable**. Full manual/vendor parity is not claimed. MANUFACTURING_REQUIREMENTS.md records PR/QC/FG/X coverage and remaining work; MANUFACTURING_GUIDE.md documents the operator sequence and APIs.

Delivered:

- Manager-created resource/maintenance controls; versioned, published immutable SOP revisions with linear phases, instructions, checklists/manual numeric limits and lab gates. Released orders schedule/reschedule with reason/priority/resource, snapshot phases/specification, require issued BOM/engineering approval/safety acknowledgment and enforce exclusive running/paused resource occupancy.
- Actual phase start/end, reasoned run pause/resume with elapsed paused minutes, stale-evidence protection, out-of-range evidence retained with a blocking deviation, complete-SOP gates on existing final-output API and immutable event/reading history with escaped CSV of displayed events.
- Versioned numeric/pass-fail quality specifications; in-process and batch samples, ordered handover/receive/testing, snapshotted limits, append-only repeated readings and latest-test evaluation, manager-reviewed Passed/Failed decisions, linked retests preserving failures and investigated root-cause/corrective-action/resolution cases. New batch samples/deviations hold stock. Existing direct batch-disposition API enforces latest passed laboratory evidence/open-case gates; laboratory pass/case closure never silently releases stock.
- Optional controlled finished batch number/future expiry on whole-order output; released/unexpired/free-stock scan-verified same-batch packing into a new pallet, exact source decrease/target increase with immutable paired movements, retained pack breakdown/reference, QR/browser label preview and scan-verified whole-pallet putaway. No physical packaging consumption, printer or external transaction is simulated.
- Original responsive light/dark execution desk, lab queue/test cards/timeline, deviation review, SOP/specification editors and finished-pallet cards. Lists/counts expose their bounded recent-record scope; no mixed-UOM aggregate or invented OEE.

Additive migration **20261017144443_ManufacturingExecution** creates11 scoped tables with composite FKs, unique/check constraints, JSON snapshots, xmin and append-only database triggers for ProductionEvent/QualityTestReading/FinishedPack/ManufacturingCommand. Existing stock, preparation credits, orders, account/session and ledger rows are preserved. New final-output command signatures prevent changed-input replay while older historical confirmations remain compatible.

### Executed validation

| Check | Result |
|---|---|
| `python /tmp/run_greenink.py test` (wrapper for `dotnet test tests/GreenInk.Api.Tests.csproj --configuration Release`, real PostgreSQL) | **37 passed,0 failed,0 skipped**,45s; clean Release compilation |
| New manufacturing API/database tests | Seven tests cover evidence/lab/predecessor and final-output gates; failed tests/corrections/retests; preserved recipe/test snapshots; role/scope/composite-FK denial; concurrent resource starts and packing; held/expired/scan/retry checks; same-batch stock/ledger conservation; actual EF/raw-SQL append-only protection; populated previous-schema downgrade/re-upgrade preservation |
| `npm --prefix apps/web run build` | TypeScript/Vite production build passed |
| `python /tmp/run_greenink.py browser` (wrapper for `npm run test:e2e`, system Chromium/API5081/PostgreSQL) | **5 passed**,37.2s: foundation, Inbound, existing raw-to-dispatch lifecycle, Material Preparation and new guided manufacturing/lab/finished packing |
| New browser workflow | Real UI-created specification/resource/two-phase SOP/publications/run, safety/evidence, in-process test/review, final batch/expiry/output, finished-batch test/disposition,7kg packed pallet plus3kg source remainder, correct bin and released status, valid QR preview;390px dark/mobile layouts without horizontal overflow |
| Visual review | Production, Quality and Finished Goods desktop screenshots plus finished/production mobile views inspected; Green Ink branding retained |
| Native readiness | `/health/ready` HTTP200 against isolated development PostgreSQL; no Windows/production database accessed |
| `git diff --check` | Passed |

An initial browser run immediately after restarting the native API encountered a transient login proxy500 before readiness. After checking readiness, the final complete five-test run passed. Earlier intermediate runs caught and corrected a dialog label locator and the test's mobile navigation setup; only final results above establish the recorded verification. Test data is explicitly validation data; credentials, screenshots, dependencies, reference files and native runtime files are outside tracked source.

**Remaining:** unread manufacturing manual and full reference comparison; arbitrary/parallel/dynamic phases, phase-specific additions/consumption, partial production/final technical confirmation, rework/premix/maturation/linked filling, secondary resources/shift calendars/man-machine booking, automated/frequency CCP capture, COA/customer/sample-label/image workflows, LIMS/SAP/DCS/OPC, by-products/rolls/multi-batch HUs/mass putaway/physical packaging consumption and certified printers. Full Docker/API-image build was not repeated because the previously observed Microsoft registry CDN restriction remains; Docker Desktop rebuild/UAT/production release checks are still required. No deployment or remote CI execution is claimed.


Git delivery: committed with the requested `Next Phase` message. Authorized normal push to `origin HEAD:main` was attempted and rejected by GitHub: **Invalid username or token**. Remote synchronization remains unverified. A self-contained verified `GreenInk-Manufacturing.bundle` preserves the entire commit history, including prior Inbound and Material Preparation work; no credentials/build outputs/reference uploads are included. Reconnect the repository GitHub write connection to enable direct pushes.


## Dispatch expansion — 17 October 2026

Implemented original planning, pick/pack, loading, handling-unit and setup views while preserving Orders & shipments. The full25-page Dispatch manual and selected authorized source logic informed implementation; DISPATCH_REQUIREMENTS.md records exact DS-01–DS-32 coverage and gaps. No vendor source/assets were copied.

Delivered scoped area/package/warehouse setup, versioned customer instruction snapshots, single/group picking, optional overpick with mandatory reconciliation, grouped existing delivery orders or shuttle material lines, assigned operators, priority/schedule/reassignment, quality/shelf-life/PPE/scan gates, real reserved split pallets, local HUs and package/temperature-serial/gross-weight checks, truck/loading, manager local issue, and unload/unpack/return/cancel/whole-run quarantined reversal. Immutable history and net shipped reporting preserve original issue records. Legacy quick routes cannot bypass claims/reservations. Additive migration20261017154322_GuidedDispatch creates twelve scoped tables, composite constraints and append-only triggers.

Executed validation:

| Check | Result |
|---|---|
| `python /tmp/run_greenink.py test` → Release dotnet test with real PostgreSQL | **45 passed,0 failed,0 skipped**,54s; includes eight new Dispatch tests |
| Dispatch API/database coverage | Group/mixed-UOM conservation; overpick return; scans/PPE/temp/gross gates; permission/assignment/replay revocation; foreign scope/FK denial; shelf-life/unknown-expiry; concurrent stock picks/exclusive delivery claims; unload/unpack/cancel; local shuttle/previous-schema upgrade; quality recheck/concurrent PGI; instruction snapshots/product rules; immutable SQL history; quarantined reversal/net reporting |
| `npm --prefix apps/web run build` | TypeScript/Vite passed; Dispatch lazy-loaded |
| `python /tmp/run_greenink.py browser` | **6 passed**,48.4s: foundation, Inbound, Material Preparation, manufacturing/lab/finished stock, existing lifecycle and new guided Dispatch |
| Dispatch browser workflow | Real FG production/release, setup/instructions, two grouped delivery picks, HU packing/temp/gross/QR, loading/local issue, whole-run reversal, net reports/history CSV;390px dark/mobile without horizontal overflow |
| Visual review | Desktop and mobile Dispatch screenshots inspected; Green Ink branding retained |
| Native readiness | HTTP200 `/health/ready`, isolated PostgreSQL; no Windows/production database accessed |
| `git diff --check` | Passed |

The complete browser run passed after readiness was confirmed. Temporary credentials, reference attachments, test data, dependencies and screenshots are outside tracked source. No full Docker image build, remote CI, production deployment or live ERP/hardware execution is claimed. Full manual/vendor parity remains open, especially external integrations, advanced calendars/permissions, spreadsheets, preprinted/nested HUs and IBC cycles, multi-line SAP DN ingestion, SSCC/commercial documents and production hardening/UAT. See the requirement map for details.

Git delivery: committed as **Next Phase**. Normal authorized `git push origin HEAD:main` was rejected: **Invalid username or token. Password authentication is not supported for Git operations.** GitHub synchronization is not confirmed. The self-contained verified `GreenInk-Dispatch.bundle` preserves the entire local history, including prior phases. Reconnect the GitHub write connection to restore direct pushing; never paste tokens into chat. Import with the ordinary merge in DISPATCH_GUIDE.md to preserve divergent laptop commits.
