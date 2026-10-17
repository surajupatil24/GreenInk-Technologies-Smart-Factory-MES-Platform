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

## Material Preparation coverage update — 17 October2026

The new order-focused local staging workspace now connects scheduled/approved plans, scan-verified stock, separate/combined/pick-only preparation, manual tolerance/correction-factor weighing, kit delivery/issue/return, replenishment, static/open-plan Kanban and reviewed discrepancies. This advances the staging core, not the full172-page reference scope. Use MATERIAL_PREPARATION_REQUIREMENTS.md (MP-01–MP-33) as the current feature inventory and MATERIAL_PREPARATION_GUIDE.md for verified contracts. Grouping/resource calendars, thermal preprocessing, ASRS/AGV, external warehouse/STO/DN, spreadsheets, physical adapters and production hardening remain open. Earlier roadmap statuses describe earlier baselines.


## Manufacturing, Quality and Finished Goods coverage update — 17 October 2026

The original execution/lab/finished-stock workspaces now connect versioned SOPs, schedule/reschedule and exclusive resources, linear phases/checklists/manual numeric limits, pause/resume timing, sample handover/testing/retest/review, deviation investigation, gated batch disposition, output batch/expiry, same-batch packing and scan putaway. See MANUFACTURING_REQUIREMENTS.md (PR/QC/FG/X map) and MANUFACTURING_GUIDE.md. Full reference parity remains unverified: the manufacturing PDF exceeds the32MiB transfer limit. Partial completion, dynamic rework/addition/parallel workflows, secondary resources/calendars, advanced packaging/by-products/HUs, COA/LIMS/ERP/DCS/physical printers and production hardening remain open. Existing baseline roadmap entries are historical, not claims that these new slices remain entirely unimplemented.


## Dispatch coverage update — 17 October 2026

Connected local configuration/instructions, assigned customer/shuttle plans, scan-verified single/group picks, HU packing/weights/serial references, loading, local issue and auditable unload/unpack/return/cancel/reversal are implemented. DISPATCH_REQUIREMENTS.md maps DS-01–DS-32 to the supplied manual; DISPATCH_GUIDE.md defines operator/accounting boundaries. Full parity is not complete: SAP/ERP, destination receipt, live RFID/telemetry/scales/printers, GS1 SSCC, spreadsheet masters, preprinted/nested HUs/IBC cycles, multi-line DN ingestion, picker groups/fine area grants, route optimization, commercial documents, pagination, production hardening and plant UAT remain open.
