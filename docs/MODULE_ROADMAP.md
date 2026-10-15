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

## Current core implementation and next gates

The connected core across milestones2–7 is implemented and validated with18 PostgreSQL/API tests and2 browser tests. These do not close the full milestone scopes above. Detailed working contracts and remaining advanced requirements are in OPERATIONS_GUIDE.md.

1. Verify the complete updated Compose stack on Docker Desktop; Microsoft registry CDN denial prevents the API image build in the managed cloud. Existing foundation boot/login was confirmed by the user.
2. Next inbound expansion: multi-line PO scheduling/import, receipt inspection specifications and COA attachment, gate/dock handling and guided put-away.
3. Inventory/staging expansion: cycle counts and adjustment approvals, partial pallet movement/split, unused material returns, pre-weigh/scanner validation and substitutions.
4. Manufacturing/quality/dispatch expansion: versioned BOM/routings, equipment/shift capture, process tests/nonconformance, sales/pick-pack/load scans and shipping documents.
5. Printer/ERP/device adapters require approved connection details and environment testing. Extend local contracts before claiming external delivery.
6. Complete pagination, dependency/license review, accessibility/load/security testing, observability, recovery/UAT and production approval before external release.
