# Receiving control — first inbound expansion

This phase adds a Green Ink gate-to-receipt workspace derived from the supplied manual's workflow requirements. UI, domain and implementation are original. The supplied source ZIP was too large for the 32 MiB transfer tool and was not inspected. Full manual parity remains open in INBOUND_REQUIREMENTS.md.

## Operator sequence

1. Select a plant. Under Inbound → Master data/Receiving, create the material, supplier and existing single-material purchase orders.
2. A manager opens Receiving setup: configure dock code/name/maintenance, optional mandatory truck and delivery note, and binary Arrival/Unloading checklist rules. Same dock code or same checklist prompt/stage updates configuration. Configuration changes apply to new visits; existing checklist snapshots are retained.
3. Under Truck & dock, register a scheduled truck, driver and one or more open POs (maximum50). An internal token is generated. The arrival board searches truck, driver, token and linked order numbers, with status filters.
4. Select the visit, mark Arrived, answer Arrival checks and mark Docked. Maintenance docks cannot be selected. Several trucks can queue at a dock.
5. Answer pre-unloading checks and mark Unloading. Another active truck at the dock blocks this action. Required answers/pass checks are enforced by the server. Arrival answers lock at docking; unloading answers lock when unloading starts.
6. Under Packed receipt select the unloading truck and one of its open orders. Enter delivery note, supplier/internal batch, pack size, whole pack count, loose quantity and optional expiry. Posted quantity = size × count + loose. It must fit outstanding PO quantity. Material UOM is displayed in the PO choice.
7. Posting creates exactly one held batch, receiving pallet and immutable positive stock movement. No automatic quality release. Checklists/dock/visit changes alone never create inventory.
8. Return to the selected truck and mark Unloaded, then Out. Exit timestamp makes the token inactive for new receipts. An unchanged network retry of a previously successful receipt still returns its original result without a second movement.

Receiving retains the quick quantity form. If a manager enables required truck/delivery-note policy, use Packed receipt to supply those fields. Quick/direct API requests cannot bypass configured rules. Use Inventory for existing full-pallet put-away and Quality for disposition. Other modules and company branding remain intact.

## API

Prefix `/api/operations/receiving`; authenticated and plant scoped. GET uses `?plantId=...`; POST includes `plantId`. Mutations require `X-GreenInk-Request: 1`.

| Route | Behavior / POST body beyond plantId |
| --- | --- |
| GET/POST `/policy` | `requireTruck`, `requireDeliveryNote` booleans; manager writes |
| GET/POST `/docks` | `code`, `name`, `maintenance`; upsert by normalized code; manager writes |
| GET/POST `/checks` | `prompt`, `stage` Arrival/Unloading, `required`, `requirePass`, `active`; upsert by prompt/stage; manager writes |
| GET/POST `/visits` | `truckNumber`, `driverName`, UTC `scheduledAt`, distinct scoped open `purchaseOrderIds`1–50, GUID `commandId`; creates visit/order links/checklist snapshots atomically |
| GET `/visits/{id}` | Scoped visit, linked orders and checklist snapshots |
| POST `/visits/{id}/status` | `status`, optional `dockStationId`; Expected→Arrived→Docked→Unloading→Unloaded→Out. Same target is retry-safe; Docked retry must match dock |
| POST `/checks/{answerId}/answer` | `passed` boolean, optional `notes`≤2000; stage locks and audit enforced |

All readers with plant access can inspect records. Viewer writes are forbidden. Operational truck/checklist actions allow existing operational writer roles; settings/dock/check definition writes require PlatformAdmin/TenantAdmin/PlantManager. Foreign identifiers return403/404 or validation without exposing a foreign record. Scheduling is a timestamp within one year past/two years future, not a capacity/shift scheduler.

Existing `POST /api/operations/receipts` adds optional `truckVisitId`, `deliveryNote`, `packSize`, integer `packCount`, `looseQuantity` default0. Pack size/count must be supplied together; count≤1,000,000, size/loose at most6decimal places. If supplied, quantity must match exactly. A linked truck must be actively Unloading, not exited, and registered for the PO. Policies also protect requests that omit all new fields. Idempotency comparison includes every new receipt field.

Serializable transactions/xmin protect stock and gate transitions; conflicts return409 for refresh/retry. Concurrent starts cannot activate two trucks at one dock. Timestamp precision is normalized to PostgreSQL microseconds for stable visit retries. Composite tenant/plant FKs protect every new relationship. Creation commands reject changed payload replays. Dock/definition/policy edits are current-state saves (not historical configuration version editing); snapshots preserve each registered visit's obligations. Lists show latest200 records; UI board metrics summarize that loaded window.

## Migration / update

`20261016211846_ReceivingGate` adds six scoped tables, receipt provenance/pack columns, unique indexes, composite FKs, row versions, pack reconciliation and allowed-visit-state constraints. Existing receipts default to no truck/note/pack data. No existing balances or ledger rows are rewritten. Existing plant policies remain off until configured.

Back up the database, update Git, rebuild **API and web** with the normal Compose command. Keep `.env` and database volumes. Development startup applies migrations; production schema changes require the existing reviewed release procedure.

## Deliberately pending

Multi-line PO/delivery planning/calendar, material profiles/restricted categories, invoice preparation/blind recount, full checklist designer/material criteria/photos, COA attachments/review, sampling and sample movements, mixed packs and receipt drafts, nonbatch/date derivation, bulk/sand/random weights, imports/STO/toller, advanced put-away/returns, exports/alerts, real ERP/printer/scale adapters and production release validation. There is no source-compatibility claim or production deployment.
