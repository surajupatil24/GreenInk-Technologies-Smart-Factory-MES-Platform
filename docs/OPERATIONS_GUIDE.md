# Operations guide — core vertical slices

This phase extends the foundation with a basic connected material lifecycle across milestones 2–7. It does not complete the original MES scope. The core lifecycle is implemented and under final regression validation; consult `IMPLEMENTATION_STATUS.md` for executed evidence. Use a development database and explicitly identified test records for the acceptance steps below.

## Access and posting contract

All operations use `/api/operations` and require the existing authenticated session. Supply `plantId` in list query strings and in command JSON. The server derives the tenant from the authorized plant; identifiers from another plant cannot be used as references. PlatformAdmin can access all plants; TenantAdmin accesses its tenant; other roles need explicit plant assignment. Viewer cannot write. Quality decisions, production release, stock adjustments and authorized reprints require PlatformAdmin, TenantAdmin or PlantManager. Operator permissions do not include those manager actions.

POST requests also require `Content-Type: application/json` and `X-GreenInk-Request: 1`. Posting commands that declare `commandId` require a nonempty GUID; retain it with the original payload when retrying an uncertain result. Reusing a key with different command data is invalid. Keys are checked within the implemented command's scope; they are not a global ERP deduplication guarantee. Master-data creation uses unique codes rather than the posting-command convention.

Quantities use decimals with at most six fractional places and documented positive bounds. Codes contain 1–32 letters, digits, hyphens or underscores and are normalized to uppercase. Names are trimmed, 1–160 characters. Use the returned IDs rather than treating business codes as IDs. Rows expose PostgreSQL `xmin`-backed versions for concurrent modification detection. Stock-changing workflows use serializable transactions; conflicts return HTTP 409. Refresh affected data and retry an unchanged uncertain posting with its original command ID. Validation errors return 400, missing authentication 401, forbidden scope/role 403, and some missing resource lookups 404. Do not retry validation errors blindly.

Operational list endpoints are bounded views, generally up to 200 rows, rather than complete paginated exports. Audit and reports likewise must not be treated as an archival reporting system. Time values use UTC timestamps. Do not infer UOM conversions: quantities must use the selected material's unit.

## Inbound and inventory API

Paths below are relative to `/api/operations`. All POST bodies include `plantId`; all listed GETs require `?plantId=GUID`.

| Method/path | Additional inputs / behavior |
| --- | --- |
| GET/POST `/materials` | Create: `code`, `name`, `uom` (up to 16 chars), `kind` = `Raw` or `Finished` |
| GET/POST `/partners` | Create: `code`, `name`, `kind` = `Supplier` or `Customer` |
| GET/POST `/bins` | Create: `code`, `name`, `warehouse`; warehouse is a descriptive field in this slice |
| GET/POST `/purchase-orders` | Create: `number`, `supplierId`, `materialId`, `quantity`; one raw-material line per order |
| GET/POST `/receipts` | Post: `purchaseOrderId`, `quantity`, `supplierBatch`, `batchNumber`, optional `expiryDate`, `commandId`. Creates held raw batch, receiving pallet and receipt movement atomically; updates outstanding PO quantity |
| GET `/batches`, `/pallets`, `/movements` | Plant-scoped source records and append-only stock history |
| GET `/inventory` | Optional `barcode` matches the exact pallet code; returns joined pallet/batch/material |
| GET `/allocation` | Additional `materialId`; suggestions include released, unexpired stock with free quantity, ordered by earliest expiry then receipt age; undated batches last |
| POST `/transfers` | `palletId`, `location` (configured destination bin code), `commandId`; moves an entire nonempty pallet, appending a zero-quantity location movement |
| POST `/adjustments` | Manager only: `palletId`, signed `quantity` delta, `reason`, `commandId`; rejects balances below zero or reserved quantity |

An allocation suggestion is not a reservation and can become stale. Receiving rejects excess quantity against the outstanding PO, duplicate batch numbers and expiry in the past. A receipt creates a quality hold; it does not imply inspection acceptance. Corrections append movements rather than editing the posted ledger. Bin/warehouse strings in this slice do not implement a full warehouse/location hierarchy.

## Execution, quality and supporting APIs

All POST bodies below also include `plantId`. GET lists require `plantId` as above. `id` is the resource GUID in the URL.

| Method/path | Inputs / behavior |
| --- | --- |
| GET/POST `/production-orders` | Create: `number`, finished `materialId`, `plannedQuantity`, `requirements` array of `{materialId, quantity}`; 1–200 unique raw-material requirements |
| GET `/bom` | Additional `productionOrderId`; order-specific requirements, not a reusable versioned BOM master |
| POST `/production-orders/{id}/release` | Manager: `commandId`; draft → released; repeated final state is safe |
| GET/POST `/reservations` | Create: `productionOrderId`, `palletId`, `quantity`, `commandId`; released, unexpired raw stock and remaining requirements only |
| POST `/reservations/{id}/issue` | `commandId`; consumes reserved stock and links consumption to the production order |
| POST `/reservations/{id}/cancel` | `commandId`; frees an unissued reservation; repeated cancellation is safe |
| POST `/production-orders/{id}/complete` | `quantity`, `scrapQuantity`, configured bin `location`, `commandId`; every requirement must have been consumed, good plus scrap must equal planned quantity; creates held finished output |
| GET `/consumptions`, `/confirmations`, `/downtimes` | Recorded execution evidence |
| POST `/downtime` | `productionOrderId`, `minutes` (1–525600), `reason`; manual event, without inferred machine utilization |
| GET `/finished-goods` | Finished pallets and batch/quality/order references |
| GET/POST `/delivery-orders` | Create: `number`, `customerId`, finished `materialId`, `quantity` |
| GET/POST `/shipments` | Post: `deliveryOrderId`, `palletId`, `quantity`, `truck`, `commandId`; full delivery quantity from one released available finished pallet |
| GET/POST `/quality-inspections` | Manager create: `batchId`, `decision`, `notes` (up to 2000 chars), `commandId`; Hold → Released/Rejected, Released → Hold; requires on-hand stock |
| GET `/traceability` | Additional `batchId`; connected internal input/output/customer graph, max 200 nodes and 20 expansions; inspect graph-depth flag; each result array is limited to 200; inspect `perListTruncated`, `collectionsTruncated` and `isComplete` before treating the result as complete |
| GET `/reports` | Actual persisted summaries grouped by UOM; yield = good/(good+scrap); OEE is null with an explanation |
| GET/POST `/label-templates` | Manager create: `name`, `content` (1–4000 chars); supported tokens `{{palletCode}}`, `{{batchNumber}}`, `{{materialName}}`, `{{quantity}}`, `{{uom}}`, `{{location}}` |
| GET/POST `/print-jobs` | Create: `palletId`, `labelTemplateId`, `copies` (1–100), `printer` exactly `Local preview`, `commandId` |
| GET `/print-jobs/{id}/preview` | `plantId`; JSON with plain-text `renderedContent`, `physicalPrint=false`; new jobs preserve immutable rendered text and template revision; legacy jobs explicitly report unavailable snapshots |
| GET `/print-jobs/{id}/barcode.svg` | `plantId`; authenticated plant-scoped `image/svg+xml` QR preview encoding the pallet code (QRCoder, ECC Q); no physical output |
| POST `/print-jobs/{id}/reprint` | Manager: `reason`, `commandId`; creates linked local preview job |
| GET/POST `/integration-messages` | Manager create: `externalId`, `kind`, `payload` (JSON encoded as a string, max 32 KiB); matching external ID/content replays, different content rejected |
| POST `/integration-messages/{id}/process` | Manager: no extra fields beyond `plantId`; Pending/Failed → Validated for JSON objects or Failed/DeadLetter after at most three attempts; `deliveredToExternalSystem=false` |

Release/cancel use state-based retry safety; integration creation uses scoped external ID; integration processing is a bounded attempt, not a retry-safe ERP post. Downtime and master/template creation do not accept command IDs. Do not assume every POST has a uniform idempotency contract. Integration syntax is checked on submission; syntactically valid non-object JSON can enter the local validation failure path. Label text/QR previews and connector status do not represent external delivery. QR generation is local; no printer driver is configured.

## Workflow acceptance sequence

1. As an administrator, create a tenant, plant and plant-scoped Operator, Viewer and PlantManager. Verify each account can see only assigned plants. Record the initial stock totals.
2. In that plant, create a supplier, customer, raw material, finished material and receiving/storage/production bins. Create a raw-material PO for a known quantity.
3. Receive part of the PO with a future expiry and unique supplier batch, batch number and command ID. Verify PO partial receipt, held batch, receiving pallet and exactly one receipt movement. Replay the same payload and ID; stock must not increase. Alter its quantity while retaining the ID; expect rejection. Attempt over-receipt and a foreign-plant supplier/order ID; expect rejection with no partial writes.
4. As PlantManager, inspect/release the raw batch. Verify an Operator cannot make the same privileged quality decision. Held/rejected/expired stock must be excluded from usable allocation. Transfer the pallet to a configured storage bin and verify quantity is unchanged and location history remains visible.
5. Create a finished-material production order and raw-material requirements. Release it as a manager, reserve available released stock and issue the reservation. Verify reservation/free balances, consumption and immutable movement links. Attempt over-reservation, repeated issue and issue from held stock; verify no double depletion. Use a second simultaneous posting to exercise conflict/rollback behavior.
6. Complete production against consumed inputs, capturing good output and scrap where supported. Verify finished batch/pallet provenance, quality hold, order state and no duplicate output on replay. Inspect/release finished goods as manager.
7. Create a delivery for the configured customer, ship released available finished goods with a truck reference, and verify delivery state, shipment record and stock depletion. Held, expired, insufficient and foreign-plant stock must not ship. Replay must not deplete twice.
8. Look up raw and finished batch/pallet trace. Follow receipt → consumed input → production output → shipment references in both directions. This is the persisted genealogy available in the slice, not a claim of complete external supply-chain traceability.
9. Create a local label template/job, inspect rendered text, the valid pallet-code QR SVG and manager-authorized reprint history. Confirm the UI states that this is a local preview. No physical printer should receive a job.
10. Submit a local JSON integration message, process it, inspect status/error/attempt/correlation fields, retry invalid input and verify duplicate external-ID handling. A `Validated` message means only local JSON validation; it does not post to SAP or another ERP.
11. Inspect persisted operational summaries and available reports. Reconcile them to the records created above. No genuine OEE is calculated without machine runtime, planned schedule and performance-rate evidence.
12. Repeat read/write requests as Viewer and with substituted tenant/plant/object IDs. Verify forbidden writes and no cross-scope data. Review audit records and restart the application to confirm persistence. These are acceptance instructions until backed by executed tests and a recorded result.

## Scope limits and continuation backlog

| Module | Implemented core slice (see validation status) | Original advanced requirements still open |
| --- | --- | --- |
| Inbound | Single-material PO, receipt, batch/expiry, hold and pallet | Multi-line/import/scheduled POs, truck preregistration, gate/dock allocation, COA attachments, manufacturing-date capture, guided put-away, richer rejection/receiving reports |
| Inventory | Pallet stock, immutable movements, transfer, adjustment, barcode lookup, FEFO suggestions | Full normalized warehouse hierarchy, cycle count approval/reconciliation, status-separated balances, configurable FIFO/FEFO policy, partial pallet split/merge, advanced correction approvals |
| Material preparation | Order requirements, released-stock reservation and issue | Scales/pre-weighing, scanner-enforced picking, Kanban, repacking, approved substitutions, shortage planning, unused returns and full production-area inventory |
| Manufacturing | Order/release, BOM-like requirements, issue/consumption, completion/output and scrap | Versioned reusable BOM/routings, finite scheduling/resource assignment, operator instructions, process parameters, shift/equipment states, milestones, packaging execution, loss/gain approvals and complete closure policies |
| Quality | Manager batch decision and inspection history | Versioned plans/specifications, sampling/test results, in-process control, nonconformance/CAPA, COA, multi-step approvals and quality audit workflows |
| Dispatch | Single-material delivery and pallet shipment | Sales orders, multi-line deliveries, route/planning, pick-pack tasks, load scans, shipping documents, truck/dock workflows and ERP goods issue |
| Labels/trace | Local text and QR preview, job history/reprint authorization, internal genealogy | Additional barcode symbologies, physical ZPL/BarTender adapters, printer assignment/queue delivery/recovery, versioned label approval and broader trace exports |
| Integrations | Local JSON message validation, status and correlation | Actual approved SAP/ERP REST/OData/IDoc adapters, synchronization/posting, outbox/broker delivery, durable dead letters, credentials, contract tests against external systems |
| Analytics/smart factory | Summaries derived from persisted application records | Genuine OEE, machine/equipment utilization, historical trend/report export, PLC/SCADA/MQTT telemetry, scheduling baselines and validated AI use cases |
| Platform/hardening | Existing scoped identities, sessions, audit and local runtime | SSO/MFA, fine-grained custom roles, full pagination, observability/retention, load/security/accessibility tests, production recovery/UAT and approved deployment |

Milestones 2–7 therefore have connected core slices; they are not fully completed milestones. Complete missing acceptance evidence and resolve defects before expanding each workflow. Maintain the original requirements as the scope reference rather than reducing them to this table's implemented subset.

## Saved BOM and completion readiness

Select a released order in Material Preparation or Complete production to view its **saved** BOM, not the default values in the new-order form. The read-only endpoint `GET /api/operations/production-orders/{id}/requirements?plantId=...` returns planned output and, per raw material/UOM, required, active reserved, issued, still-to-reserve, still-to-issue and released unexpired available quantities. It uses a consistent database snapshot and the same plant authorization as posting. Aggregations cover all posted rows rather than the bounded list views.

The source-pallet picker shows only released, unexpired raw materials still required by that order. Active reservations offered for issue/cancel follow the selected order. Reservation is not issue: when Still to reserve is zero but Still to issue is positive, issue the existing reservations.

Completion closes the entire order. The button stays disabled until all saved BOM quantities have been issued and good output plus **actual** scrap equals planned output. Partial confirmations are not implemented. Do not invent scrap to reconcile a short production run. For example, planned25 does not accept good15 + scrap0; good24 + actual scrap1 accounts for25, but still requires all saved raw-material quantities issued. The BOM raw quantities are independent of the finished output quantity. No schema migration or alteration of existing order/stock records is required by this visibility improvement.

## Inbound expansion: receiving control

Inbound now adds Truck & dock, Packed receipt and Receiving setup. Follow RECEIVING_CONTROL.md for the actual gate-to-receipt sequence and API contracts. INBOUND_REQUIREMENTS.md maps the entire supplied manual and distinguishes this first implemented subset from remaining enterprise workflows. Settings apply to all receipt API paths, not only the new screens. Rebuild API and web; keep private environment and database volumes.


## Material Preparation workspace

Material Preparation now opens an order-focused workspace with planning, scan-verified picking, manual gross/tare/net kitting, tolerance/factor snapshots, kit delivery/issue, allocation returns, replenishment, static/open-plan-demand Kanban and manager-reviewed stock discrepancies. Quick reservations remain a secondary path for unplanned orders. Read MATERIAL_PREPARATION_GUIDE.md for operator steps and contracts; MATERIAL_PREPARATION_REQUIREMENTS.md maps the supplied172-page manual and remaining features. Physical consumption and nominal BOM credit are distinct for guided allocations. Rebuild both API and web; preserve the existing private environment and database volumes.
