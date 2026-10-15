# Implementation backlog

Each milestone is a demonstrable vertical slice, not a single generated code dump. Review `IMPLEMENTATION_STATUS.md` for current evidence.

| Priority | Milestone | Deliverables and exit evidence |
| --- | --- | --- |
| P0 | 1 — Platform foundation | API/web/database, secure sessions and RBAC, tenant/plant and user administration, scoped command center, Compose and CI; build/test, PostgreSQL migration and browser demonstration |
| P1 | 2 — Inbound | Master data, PO and receiving, inspection hold/batches/pallets, label adapter and put-away; receipt rollback and duplicate-post prevention |
| P1 | 3 — Inventory | Immutable ledger, balances, transfers/scanning, reservations/reconciliation; contention, no-negative and tenant isolation tests |
| P2 | 4 — Staging | BOM requirements, allocation/picking/pre-weighing, issue/returns and substitutions; scanner and reservation concurrency tests |
| P2 | 5 — Manufacturing | Orders, execution/consumption/confirmation, quality gates, packaging/FG genealogy; material-to-output trace demonstration |
| P2 | 6 — Dispatch | Deliveries, picking/loading, shipment/documentation; end-to-end forward/backward trace demonstration |
| P3 | 7 — Integrations | Contract-tested local ERP adapters, outbox/messaging, retry/dead letters, IoT interfaces and monitoring; no live SAP claim without credentials and a test environment |
| P0 before release | 8 — Hardening | E2E/accessibility, security/load review, backup restore drill, operational telemetry and UAT; approved production release checklist |

## Next foundation gates

1. Native Release API/PostgreSQL tests and frontend build/browser workflow pass; maintain them in CI. Remote Actions execution remains unverified.
2. Complete API image pull and fresh Compose startup/restart on Docker Desktop; managed cloud image CDN access is blocked.
3. Review key persistence, session retention, proxy throttling, list pagination and grant concurrency before closing the foundation.
4. Logical backup restore/schema/count check passed; verify recovered application login and operational recovery procedures.
5. Then implement Milestone 2: material/UOM/supplier/warehouse/bin masters, purchase orders and the first transactional goods-receipt workflow with validation, inspection hold, batches and pallets. Add label adapters and put-away through tested vertical slices.
