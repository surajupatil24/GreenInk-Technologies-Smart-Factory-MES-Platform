# Architecture

## Foundation boundary

GREEN INK SmartFactory begins as a modular monolith: one ASP.NET Core 10 API, one React/TypeScript/Vite web client, and PostgreSQL 17. The legacy root Express barcode application remains outside the new runtime. Module boundaries will be introduced with business capabilities; empty directories do not represent implemented modules.

The browser calls same-origin `/api` endpoints. In Docker, nginx serves the web application and forwards `/api/` to the internal API. Only the web port is exposed on localhost. Native development uses Vite's development proxy. EF Core migrations describe relational schema changes; the development API applies migrations at startup. Production will require a separately reviewed migration release procedure and a database principal with reduced privileges.

## Security and scope

The API owns authentication, session lifecycle, role permissions, tenant membership, and plant access checks. Browser navigation can reflect permissions but cannot enforce them. Every identifier must be authorized before reading or writing data. Plant ownership must match the active tenant. Lists and command-center metrics must use the same scope as individual operations. Sessions expire after eight hours and logout deletes the server-side session; cookies require HTTP-only protection and secure transport outside local development. Passwords must use framework password hashing rather than custom cryptography. Never trust a role or tenant supplied by the browser without checking server-side membership.

Bootstrap configuration creates the first administrator without a public signup endpoint or default password. Environment variables are suitable for local development; production secrets require an approved secret provider. Privileged mutations must create audit records without storing passwords or session tokens.

## Domain boundaries and later dependencies

Administration owns organizational structures, membership, permissions and master data. Inbound owns receiving; inventory owns balances and immutable movements; staging owns reservations and issues; manufacturing owns execution; quality owns disposition; dispatch owns outbound execution. Labeling and integration use explicit adapters. Cross-module workflows must preserve tenant and plant scope and commit atomic business operations.

Transactional outbox, RabbitMQ, S3 storage, TimescaleDB and industrial adapters are future capabilities, introduced only when an implemented workflow needs them. Redis and MongoDB are not required by the current foundation. No live SAP, printer, PLC, MQTT, or SCADA connection is implemented.

## Quality gates

Foundation checks include API build/tests and frontend typecheck/build. PostgreSQL migration, cookie/session behavior, cross-tenant access, cross-plant access, browser workflows, Compose startup, and recovery need direct validation. Later milestones add transaction rollback, idempotency, concurrency, accessibility, E2E, security and load checks. A successful unit build alone does not demonstrate production readiness.

## Guided Material Preparation

Staging adds plant-scoped area/rule/plan/pick/measurement/request/issue/Kanban entities in the same transactional database. A dedicated allocation is a real pallet of its original batch, with a linked reservation. Splits append equal negative/positive movements; kit delivery appends a zero-quantity location event. Issue consumes physical quantity while recording a nullable nominal requirement credit for tolerance/correction-factor accounting. Historical records default to physical quantity. Production readiness uses credit; inventory/reporting retains physical balances. Rule/factor versions protect open forms, and each posted pick retains its configuration snapshot.

StagingCommand stores a scoped unique command ID, payload hash, action/result and optional plan link. Serializable transactions/xmin include the command, balances, reservations, measurements and ledger changes. Discrepancy reports are separate from manager-reviewed adjustments. No external queue/device response is invented. MATERIAL_PREPARATION_GUIDE.md describes the actual API/state boundaries; MATERIAL_PREPARATION_REQUIREMENTS.md records the supplied manual's full remaining scope and UAT decisions.


## Guided manufacturing and quality

Plant-scoped ProductionResource/Recipe/Run/Phase/Event entities implement linear execution with immutable published SOP content and per-run phase snapshots. A run snapshots its quality specification; QualitySample snapshots its own numeric/pass-fail test limits. QualityTestReading and ProductionEvent are append-only through tracked guards and database triggers. Review/closure and batch disposition remain separate decisions. The existing complete and quality-inspection APIs enforce the same guided gates so legacy/quick forms cannot bypass them.

ManufacturingCommand records a scoped unique payload hash/action/result in the same serializable transaction as mutations. New final-output commands use the same signature store while historical confirmations remain replay-compatible. FinishedPack creates a same-batch pallet with equal negative/positive stock ledger entries. Scan putaway posts a zero-quantity location movement. No hardware response, external event bus, laboratory result or ERP posting is invented. MANUFACTURING_GUIDE.md defines the bounded local contracts; MANUFACTURING_REQUIREMENTS.md distinguishes all unfinished reference requirements.
