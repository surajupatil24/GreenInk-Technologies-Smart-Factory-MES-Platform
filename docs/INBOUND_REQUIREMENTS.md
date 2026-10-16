# Inbound receiving requirements and implementation map

Date: 2026-10-16. Status: requirements mapped; first receiving-control slice implemented. The matrix below records the baseline gaps; the delivery notes at the end identify the exact implemented subset.

## Evidence, provenance and interpretation

The supplied `GR User Manual_version2(02042026) 1.pdf` is a 200-page operator manual, version 2 dated 2 April 2026. The entire extracted text was reviewed, including setup, mobile/web variants, warehouse activities and appendices. References below use printed page numbers, matching the PDF page index after its unnumbered cover (PDF viewer page number is printed page + 1). Pages 199 onward contain no additional extracted operational requirements. Screenshots may contain details that text extraction does not expose; no unobserved screenshot field or exact layout is assumed.

This document states original Green Ink requirements in independent language. It does not reproduce manual paragraphs, screens, icons, branding or software code. The supplied source ZIP exceeded the available 32 MiB download limit and was **not reviewed**. No source-code equivalence or compatibility is asserted. ERP movement identifiers below describe requested external accounting semantics, not an implemented SAP connector. Green Ink domain names, UI, contracts and migrations remain independently designed.

The manual describes several plant-configurable alternatives and occasionally leaves formulas or exceptional paths unspecified. Requirements labelled “Green Ink design” resolve implementation integrity concerns; they are not claims that the manual specifies that exact algorithm. Open decisions are listed below rather than silently guessed.

## Existing baseline and status convention

The baseline before this inbound expansion includes scoped materials/suppliers/customers/bins, single-material PO creation, direct quantity receipt into a held batch and receiving pallet, expiry capture, immutable stock ledger, full-pallet transfer, manager adjustment/quality decision, internal trace, local text/QR label preview with snapshots, local JSON validation and operational reporting. Saved production BOM readiness is also present. Historical tests are recorded separately in `IMPLEMENTATION_STATUS.md`; they do not verify the new requirements here.

**B** means a limited baseline exists and needs extension; **G** means a gap in that baseline; **P** means proposed behavior pending implementation and acceptance. Rows can include both B and G. No row implies complete manual coverage. This file intentionally remains a scope/acceptance specification while other agents implement; source changes and executed evidence must be reconciled before changing status.

## Section-to-feature matrix

| ID | Manual sections / pages | Independent Green Ink requirement | Baseline and gap |
| --- | --- | --- | --- |
| IN-01 | 1–2, p4 | Authenticated receiving across responsive web and scanner-friendly mobile; organization/plant context and role enforcement | B identity/scope; G guided mobile receiving |
| IN-02 | 3.1.1–2, p4–6 | Material receiving profile: batch-managed flag, pack sizes, package defaults, JIT, movement/handling indicators, bulk/sand modes, fixed destination bins, images | B material/UOM/kind only; G receiving profile |
| IN-03 | 3.1.3–5, p6–8 | PPE/hazard references, supplier questionnaire, package dimensions/tare/UOM with material-level mappings and controlled import/export | G |
| IN-04 | 3.1.6–11, p9–13 | Separate privileges for checklist creation/configuration, toller mappings, return request/picking/confirmation and stock-error closure | B broad manager policies; G fine-grained permissions |
| IN-05 | 3.1.7, 3.3.4, p9,33–34 | Identify imported orders by configured MRP/purchasing metadata | G; generic local JSON validator is not ERP master synchronization |
| IN-06 | 3.1.9, p10–11 | Vendor stock ownership and vendor-linked storage/toller receiving mappings | G |
| IN-07 | 3.2.1–3, p13–18 | Supplier contacts, truck types/capacity/unload duration, time slots, buildings, docks and eligibility suggestions; maintenance prevents use | B basic supplier; G transport/dock masters |
| IN-08 | 3.2.4, p18–30 | Versioned approved checklists, sections/items, five answer types, mandatory/comment/photo rules, catalogues, import/export and stage assignment | G |
| IN-09 | 3.2.5–7, p30–32 | Sampling tools/instructions/quantity/bin and pre-unload policy; import forwarders; material receiving notes | G; batch disposition alone is not sample execution |
| IN-10 | 3.3.1–3, p33 | Waiting alerts, optional/required delivery notes, permission to combine batches/package types/pack sizes in one receipt | G |
| IN-11 | 3.3.5–8, p34–35 | Random-weigh settings, subcontract identification/proportional component defaults, pack/pallet label policies, web put-away switch, return reject bin | G |
| IN-12 | 3.3.9–11, p36–38 | Restricted delivery item categories, tare UOM selection, editable pack/pallet defaults | G |
| IN-13 | 4.1, p38–40 | Multi-line order browse/search, open/received quantities, split delivery schedules, overdue/unscheduled indicators, spreadsheet exchange and supplier-contact action | B one material per PO; G line/schedule model and planner workflows |
| IN-14 | 4.2, p41–42 | Day/week/month receiving calendar with scheduled/unloading states and summaries for local/import/JIT/palletized/loose/bulk | G |
| IN-15 | 5.1, p43–53 | Full truck visit: multi-order registration, driver details, expected/arrival/token, surveys, dock assignment/queue/reassignment, weights, unloading, exit/token invalidation, history/exports | G |
| IN-16 | 5.2, p54–55 | Instant registration without token; shared registration group for multiple orders; category restriction at entry | G |
| IN-17 | 6.1, p56–73; 7.1, p120–132 | Packed receive, batch/nonbatch variants, actual start time, delivery note, dates/shelf life, supplier lot/internal batch, COA prompt, full/partial pack and pallet rows, staged save/labels/final receive | B direct single-batch quantity receipt; G guided draft/session/pack model |
| IN-18 | 6.2, p73–79; 7.2, p133–134 | By-quantity mode: distinct pallet identity and multiple pack sizes/segments on the same pallet | G |
| IN-19 | 6.3, p80–95; 7.3, p134–145 | Invoice-admin preparation and operator blind count, locked batch, mismatch/recount/admin resolution, registered-order-only selection | G |
| IN-20 | 6.4, p95–104; 7.4, p146–150 | Bulk invoice/explicit-quantity or truck differential receiving into one/multiple fixed bins, remaining capacity and sample gates | G |
| IN-21 | 6.5, p104–106; 7.6, p151–152 | Sand weighing: truck differential less bag/pallet tare, per-pallet allocation, multiple batch capture and final labels | G |
| IN-22 | 6.6, p106–107; 7.7, p152–154 | Pre-unload sampling when configured, QC pending/result gating and sample labels | B quality disposition; G sample records/workflow |
| IN-23 | 6.7, p107–108; 7.9, p155 | Imported PO list, bill of lading, forwarder, free demurrage days and plant arrival scheduling; then ordinary receipt | G |
| IN-24 | 6.8, p108–109; 7.8, p155 | Stock-transfer receipt with original batch/manufacture/expiry provenance prefilled | G; not an unrestricted cross-plant reference exception |
| IN-25 | 6.9, p109–116; 7.10, p155–158 | Subcontract receipt plus component actuals/multi-batch selection, proportional defaults, vendor-stock priority/fallback and linked movements | G |
| IN-26 | 6.10, p116–120 | Random-weight pending worklist, minimum pack checks, scanned labels/gross measurement and posting hold until complete | G |
| IN-27 | 7.1, 7.5, p122–124,150–151 | Mode-specific spreadsheet templates, multi-order/line/batch upload, validation and controlled label generation | G; generic JSON validation does not implement receipt import |
| IN-28 | 8, p158–168 | Pending put-away, pack/pallet scanning, proposed/manual bin, web label/batch mode, full/mass/partial mass and authorized forced placement | B full-pallet bin transfer; G receipt-scoped pending/split/mass workflow |
| IN-29 | 9–9.2, p168–175 | Reprint/RM labels, full/partial bin transfer, quality indicator, actual-stock discrepancy report/reassignment | B local snapshot/QR/reprint and pallet transfer; G physical adapters/pack transfer/error workflow |
| IN-30 | 9.3–4, p175–182 | Sampling before/after put-away, safety acknowledgments, sample booking/bin transfer, JIT sample and multi-sheet material/PO/production report/replenishment | G |
| IN-31 | 10–11, p182–186 | In-progress receipt recovery/reversal/controlled force receive; receipt/queue/error/stock-discrepancy logs, searches, exports and privileged closure | B audit/ledger/local messages; G receipt recovery/outbox and stock-error lifecycle |
| IN-32 | 12, p186–190 | COA attachment and mobile capture, material/batch/(optional order) linkage, pending/uploaded queues, review/concession/reject reasons, document/log download | G; no file-store connector presently claimed |
| IN-33 | 13, p190–196 | Receipt reversal versus vendor return; blocked-stock request, pick-to-reject location, confirm return, linked accounting and invoice reconciliation | G; stock adjustment is not a vendor-return workflow |
| IN-34 | 14, p196–198 | Mobile/web checklist execution at configured logistics/unloading stages with required completion/pass gates | G |

## Proposed data model and integrity boundaries

These are proposed entities or extensions, not a claim that tables already exist. Every reference is scoped by tenant and plant; cross-plant STO uses an explicit authorized document relationship rather than accepting foreign IDs in ordinary receiving commands.

| Aggregate | Minimum information and relationships |
| --- | --- |
| MaterialReceivingProfile | Handling mode, batch policy, shelf-life/date policy, package mappings/pack sizes, default packs/pallets, JIT/import/classification, receiving/safety notes, sampling and weighing policy, fixed bins |
| PackageType / PackageTare | Package identity/dimensions, tare value/UOM, effective configuration and allowed material mappings; separate bag and pallet tare where required |
| PurchaseOrderHeader / Line / DeliverySchedule | Supplier, type, source/external identifiers, item category, line material/UOM/ordered/open/received quantities, tolerance, planned deliveries; retain existing single-material PO compatibility through an explicit migration |
| ImportSchedule / Forwarder | Order linkage, bill of lading, forwarder, port/arrival dates, free demurrage days, plant-arrival commitment |
| Building / Dock / DockRule / Slot / TruckType | Eligibility dimensions, maintenance, sort order, queue/occupancy and expected unloading duration; suggestion distinct from reservation |
| TruckVisit / VisitOrder / InvoiceLine | Vehicle/driver/delivery purpose, registration group, token where applicable, timestamps/status/dock, multiple order links; invoice number/quantity/batch/date independent from actual blind count |
| WeightMeasurement | Truck or pack context, gross/tare/net, UOM, measurement time/actor/source, manual override reason, scale reference when available; no invented live device reading |
| ReceiptSession / ReceiptLine / PackRow / PalletAllocation | Draft physical observations, mode, actual unloading time, supplier lot/internal batch, dates, package/quantity, distinct label identities, invoice differences, all gate results and final posting link |
| SamplingInstruction / Sample / SampleTransfer | Versioned instruction/tool/safety references, required quantity, source label/location, sample bin, pending/result history and related quality inspection |
| RandomWeightCheck | Required minimum sample count, selected distinct pack labels, measured pack count/gross/tare/net, result/completion and receipt gate |
| ChecklistDefinition / Version / Rule / Response | Approved immutable execution version, five question types, required/pass criteria, attachments/comments, stage/material/truck applicability and actor/timestamps |
| CoaDocument / CoaReview | Private file metadata/hash, validated type/size, material/batch/order links, pending/uploaded/review states, decision/reason/version and authorized download audit |
| Toller / VendorStock / ComponentConsumption | Vendor-owned stock and mapped storage, received output, component BOM/actuals/batches, proportional default provenance and linked conversion/consumption events |
| PutAwayTask / Allocation | Receipt/pack/pallet scope, interim/source/destination, outstanding/placed quantities, split lineage, ordinary/mass/forced mode, authorized override reason |
| ReturnRequest / ReturnLine / ReturnPick / ReturnConfirmation | Original receipt/PO/batch, eligible blocked quantity, reject/interim locations, requested/picked/confirmed balances, reason and external posting references |
| ReceiptPosting / Outbox / StockDiscrepancy | Durable idempotency/result snapshot, original command semantics, local versus externally acknowledged status, retries/errors/recovery, discrepancy evidence and closure audit |

Stock conservation remains authoritative: receipt increases stock once; transfers move quantities without changing plant total; sample transfers move to a distinct sample bucket; production/returns consume once; reversals reference original postings and append compensating movements. A draft save, print preview or checklist completion cannot create a second receipt. Posted records cannot be deleted to repair history.

## States and gates

These are Green Ink state designs derived from the workflow, to be aligned with actual endpoint contracts before implementation. Physical completion, local posting, external acknowledgment, quality disposition and label rendering are separate dimensions.

| Workflow | Proposed transitions and safeguards |
| --- | --- |
| Truck visit | Expected → Arrived → DockAssigned/Transit → Unloading → UnloadingCompleted → Exited. Instant visits have no token. Dock queues permit assignment while occupied but do not authorize simultaneous exclusive unloading. Exit records time and invalidates token. GoodsOut/Other remain distinct purposes |
| Receipt | Draft → Unloading → Captured → AwaitingVerification/BlockedByGate → ReadyToPost → Posted. Cancellation before posting removes only draft intent; reversal after posting appends a linked correction. A conflict/retry must not duplicate stock |
| Invoice check | Uncounted → Counted → Matched or DifferenceReported → Recount/AdministratorCorrection → Matched. Operator cannot edit invoice quantity/admin batch or retrieve hidden expected count through alternate API fields |
| Random weighing | NotRequired or Pending → InProgress → Complete/Failed. Capture/labels may precede completion; posting cannot bypass Pending or Failed |
| Sampling | NotRequired or Required → Collected/PendingQC → Passed/Rejected. When pre-unload/result gate is enabled, unloading must wait for the configured outcome. Collection is not automatic release |
| Checklist | Definition Draft → Approved; editing creates Modified/new version requiring approval. Execution references approved version; required fields and required-pass criteria gate configured stage; post-GR edits follow explicit setting and are audited |
| COA | Missing → Uploaded/PendingReview → Approved/ConcessionAccepted/Rejected. Concession and rejection require reasons. Missing-document queue persists when policy allows deferred upload. COA approval and stock release must have an explicit documented linkage |
| Put-away | Pending → PartiallyPlaced → Complete; quantities cannot exceed outstanding source stock. Forced mode requires privilege/reason and never bypasses tenant scope, no-negative or duplicate-command controls |
| Vendor return | NotStarted → PickingStarted → ReadyForReturn → ReturnConfirmed. Blocked stock only; picked stock moves to reject location; confirmation consumes the eligible picked quantity once |
| External posting | LocalPending → Submitted → Acknowledged, or RetryableFailure/DeadLetter. A local JSON `Validated` state does not satisfy acknowledgment. Only an actual approved adapter may populate external document IDs |

## Calculations and validation rules

1. **Packed quantities:** full rows contribute `packSize × packsPerPallet × palletCount`; partial pack rows contribute their actual quantity/count once. Retain per-pallet/pack lineage. A partially filled pallet and a partially filled pack are different concepts. Recompute on the server with decimal arithmetic, positive integral counts and bounded totals.
2. **Mixed-size quantity mode:** one pallet may hold multiple pack-size rows; repeating the same pallet identifier appends to that pallet, while a distinct pallet creates another identity. Prevent duplicate import/scan rows from doubling stock.
3. **Defaults:** configured pack size prepopulates; when several exist, the smallest is proposed. Pack/pallet defaults apply to the first row only. No pack size means no derived quantity/count defaults. Suppress defaults when their result exceeds the permitted outstanding quantity plus configured tolerance, including pack size larger than the eligible remainder. Defaults remain editable and never replace final server validation.
4. **Dates/batches:** preserve supplier lot separately from internal batch. Date entry follows material policy: one supplied date can derive the other using shelf life; reject impossible chronology and policy breaches. Nonbatch materials do not require a fictitious vendor batch. A supplier value beyond an external batch-length constraint requires explicit lot mapping and independently generated internal identity, not silent truncation. Green Ink need not adopt an unexplained legacy batch-generation algorithm.
5. **Invoice counting:** only registered authorized order lines can be counted in invoice mode; invoice quantity/delivery note is hidden from the unloading role at the API level. Batch/date verification is visible but admin batch identity is immutable to that role. Differences persist until recount or authorized correction; changing invoice values retains original and correction audit.
6. **Bulk:** explicit unloaded quantity or truck-in minus truck-out is the proposed receipt quantity. Allocations across configured tanks must sum exactly to the accepted quantity, have compatible UOM and respect available capacity. Manual differential overrides require reason and audit (Green Ink design); do not claim an automatic scale integration without one.
7. **Sand:** `grossDelivered = truckIn − truckOut`; `netDelivered = grossDelivered − totalBagTare − totalPalletTare`. The manual describes one bag per pallet. Nominal bag print weight is not actual delivered weight. All tare values require unit compatibility. Reject negative/zero results, missing required tare, invalid weights and unallocated remainder. Define deterministic six-decimal remainder allocation so pallet sums equal net exactly (Green Ink design).
8. **Sampling:** verify label/material/batch/source location and required safety/tool acknowledgment. Sample booking transfers only the actual collected amount to the configured sample location, retaining batch/quality lineage. Pending put-away exposes interim/dock context; completed put-away exposes actual bin. Sample count/quantity cannot exceed stock or be duplicated on replay.
9. **Random weights:** distinct checked packs and measured count must meet configured minimum before the receipt gate clears. Record gross and relevant tare/UOM; the manual does not establish a universal acceptance-tolerance formula. Do not invent one as a verified rule.
10. **Toller:** propose actual consumed component batches by earliest expiry, with available vendor-owned stock first. Default quantities scale the component BOM by received output; retain operator actuals and variance. Plant-owned fallback is allowed only in the vendor-mapped storage configuration. Conversion to vendor stock (external 541 semantics), component consumption (543), and output receipt (101) need linked local movements and consistent recovery; never spend another vendor's stock. Without mapping, propose vendor stock only.
11. **Restricted categories:** enforce at scheduling, truck/order registration, invoice entry and every mobile/web/import receiving path; raw synchronized source records can remain visible. Avoid demanding a material checklist for excluded items. Client filtering alone is insufficient.
12. **Checklist gating:** logistics may require at least one response before dock assignment and full completion before receive; unloading may require all answers completed and/or all configured correct responses. Pre-unload forms must precede physical-start transition. Required comments/photos and catalogue answer membership are validated against the approved execution version.
13. **COA files:** support the requested document/image categories through an explicit allowlist; validate file signatures, limits and scoped download permissions, use private storage, and never execute active content. Mobile camera is an optional authorized capture source. Newer uploads preserve history. Download/export cannot leak another plant's documents.
14. **Put-away:** ordinary labels target the scanned pack/pallet; mass operation targets eligible material/batch stock from the same receipt rather than all lifetime stock of that batch. Partial mass placement splits balances/identity where needed. Source decrements equal destination increments, with no reservation/quality ownership loss. Proposed destinations are advisory; configured permitted alternatives are validated.
15. **Returns:** receipt reversal corrects an original posting (external 102 semantics); vendor return is a separate business disposition (122 semantics). Only eligible blocked stock can be requested/picked. Reject over-request, over-pick, foreign PO/batch, consumed stock and double confirmation. Reconcile invoice/PO open quantities via linked events; do not silently rewrite original receipt or create unexplained negative interim stock to mimic an external workaround.
16. **Transactions/security:** scoped composite references, server-side role checks, GUID command IDs on stock commands, payload-aware replay, serializable transactions/concurrency conflict handling and atomic ledger/audit/outbox writes apply to every path, including mass import, override, return and recovery. Draft edits need version checks. No forced operation is a security bypass.

## Acceptance scenario catalogue

All scenarios are **pending for this expansion**, even where a simpler historical test exists. Tests should use PostgreSQL and real browser flows; physical devices and ERP scenarios require approved test adapters/environments. Run equivalent mobile-sized and desktop paths against the same backend policy.

| Test | Scenario and required evidence |
| --- | --- |
| AC-01 | Create two tenants/plants and Viewer/Operator/manager roles; attempt direct list/detail/file/command access with foreign plant/line/token/label IDs. No leaks or mutations; Viewer writes and privileged Operator overrides denied |
| AC-02 | Multi-line PO with two scheduled deliveries; receive partial line quantities, compare outstanding totals and calendar overdue/unscheduled indicators; no received line becomes scheduled again accidentally |
| AC-03 | Register multi-order general visit, arrive, assign suggested/non-suggested/queued dock, start/complete unload, exit. Token becomes unusable after exit; maintained dock cannot start unloading; reassignment/history retained |
| AC-04 | Instant registration has no required token; group multiple same-registration orders without duplicating shared company data. Restricted line categories fail registration, invoice, import and direct receive APIs |
| AC-05 | Receive three pallets of four 25-unit packs and one pallet of two packs: total350. Add two partial packs of15: total380. Physical pack/pallet identities and posted ledger reconcile, and replay produces no extra stock |
| AC-06 | One pallet has three 50-unit packs plus one5-unit remainder, another distinct pallet has a different pack mix. Quantity mode preserves per-pallet totals and cannot create two identities for one entered pallet |
| AC-07 | Batch-managed and nonbatch materials follow their policies. Enter manufacture-only and expiry-only dates and validate derived counterpart; impossible dates rejected. Long supplier lot survives intact while internal batch is independently generated with explicit confirmation |
| AC-08 | Exercise tare-enabled/disabled and material-specific/global package mappings; compatible UOM shown, missing required tare handled explicitly, no silent unit conversion. Pack defaults first row only and smallest size selected; exceed-tolerance defaults suppressed |
| AC-09 | Warehouse admin enters invoice/batch; Operator API cannot see expected quantity or modify batch. Count mismatch blocks normal completion, recount retains evidence, admin correction is audited; matching count posts once |
| AC-10 | Bulk explicit receive split between two configured tanks; allocation sum and capacity checked. Truck differential receive records weights and any authorized override reason. Missing out-weight cannot fabricate receipt quantity |
| AC-11 | Sand truck in20000/out12000, four bags/pallets each with bag tare10 and pallet tare20: net7880, four equal pallet allocations1970. Check exact summed quantities, missing tare rejection and decimal-remainder allocation |
| AC-12 | Pre-unload sample required: start blocked until required collection/QC outcome; wrong label rejected, warning/PPE/tool acknowledgment retained. Source/sample-bin transfer balances, retry does not take another sample, put-away updates displayed source context |
| AC-13 | Random check requires four distinct packs; three cannot clear posting. Capture gross/tare/count, complete fourth and finalize once. Saved/previewed labels before check do not falsely imply posted receipt |
| AC-14 | Import order records forwarder, bill of lading, demurrage/arrival dates and appears in import/calendar workflow after scheduling; ordinary receipt retains those links. No fake ERP acknowledgment |
| AC-15 | STO preloads source batch/manufacture/expiry without allowing arbitrary foreign-plant IDs; receipt retains original genealogy and authorized transfer quantity |
| AC-16 | Toller output partial quantity scales BOM defaults; consume multiple vendor batches in expiry order. Insufficient vendor stock permits mapped fallback conversion only; wrong vendor/unmapped storage fails. Retry cannot repeat output or consumption |
| AC-17 | Upload pallet-mode and quantity-mode templates with multiple lines/batches; row errors identify exact problems. Duplicate keys, restricted categories and foreign references fail without unexplained partial posting; an explicitly documented batch/row atomicity policy governs retries |
| AC-18 | Scan pack versus pallet, then mass same-receipt group and partial quantity; total plant stock unchanged, outstanding put-away decreases exactly, split lineage retained. Forced override needs permission/reason and cannot overdraw |
| AC-19 | Full/partial bin-to-bin move retains quality state; report physical quantity discrepancy without immediately changing ledger. Authorized closure/reassignment is audited; unprivileged closure denied |
| AC-20 | JIT material eligible for sample labels/report; non-JIT excluded. Material/PO/production report and requested replenishment quantities/dates reconcile to persisted records, without creating a PO or email implicitly |
| AC-21 | Interrupt receipt before/after local posting, retry, revert or authorized force receive. Every path preserves conservation and original audit; pending external posting remains pending until real adapter acknowledgment |
| AC-22 | COA upload/camera capture, view/download, late-upload queue and Approved/ConcessionAccepted/Rejected decisions. Required reasons enforced, mandatory-policy gate effective, old revisions retained, unsafe files and foreign downloads denied |
| AC-23 | Blocked receipt stock creates unique vendor-return request; partial picks progress states and move to reject bin. Confirm only after requested picking, post once, preserve original receipt and reconcile returned/open/invoice totals. Released/unavailable/already-returned stock rejected |
| AC-24 | Approved checklist version with Text, YesNo, YesNoNA, Instruction and Catalogue items; required photos/comments/pass gates checked. Edit approved definition without changing historical responses; post-GR modification policy enforced |
| AC-25 | Two concurrent receipts/returns/put-away commands target same balance; at most eligible quantities post, losing request returns conflict with no partial audit/ledger/outbox. Same command/payload replay returns original result; changed payload rejected |
| AC-26 | Receipt/queue/error/discrepancy/COA/truck exports reconcile to source rows with date/scope filters, pagination/completeness metadata and protected document access. Restart and restore preserve pending work and posted lineage |

## Open decisions and staged delivery

1. Define exact receipt line/schedule/tolerance contract and legacy PO migration before adding multi-line data. Specify whether tolerance applies to original line quantity or outstanding balance, and require business approval rather than extrapolating one example.
2. Confirm shelf-life arithmetic/calendar basis, batch uniqueness scope, nonbatch internal trace identity and external batch constraints. The manual does not supply the legacy generator implementation.
3. Confirm whether pre-unload QC requires a pass or permits specific concessions, how sampling reduces available consumption stock, and whether random weights block local stock posting or only external posting. UI must expose the chosen distinction.
4. Define scale units/calibration, bulk density conversions/capacity, sand multi-batch allocation and override authority. No equipment protocol or universal variance threshold is established by the manual.
5. Choose file-store scanning/retention and real printer/ERP adapter contracts before asserting operational document transfer or printing. Current QR rendering/local JSON validation remain local capabilities.
6. Define source import format, duplicate keys, row/batch atomicity and outbox/recovery semantics. Spreadsheet import is an independent implementation, not an assumed compatibility contract with another vendor's workbook.
7. Confirm dock queue/override rules, return cancellation/reopen rules, simultaneous return reservations and preexisting external invoice synchronization. Keep blocked-return status separate from current simple quality Hold/Rejected values until mapped explicitly.

Recommended continuation: receiving profiles/settings and multi-line readiness → truck/dock/invoice preparation → guided packed/mixed-size draft receipt and checklist/COA gates → weight/sample/bulk/sand variants → import/STO/toller contracts → put-away/returns/recovery and physical adapter validation. Deliver and test each slice; leave unsupported modes visibly unavailable. Completing a few modes does not complete IN-01–IN-34 or the original enterprise MES scope. Update implementation status with actual migrations, tests, screenshots and remaining gaps; this document alone is not execution evidence.

## First delivered receiving-control slice

The first expansion implements subsets of IN-07, IN-08, IN-10, IN-15, IN-17 and IN-34. It does **not** complete these full manual requirements. Delivered: manager-maintained docks/maintenance, per-plant truck/delivery-note policy, scheduled multi-order truck visits with driver/token/status timestamps, searchable arrival board, queued dock assignment and exclusive active unloading, binary arrival/pre-unloading checklist snapshots with mandatory/pass gates and historical locks, packed calculation (one pack size/count plus loose quantity), delivery-note/truck provenance on actual receipts, and exited-token rejection for new stock posting. The existing ledger/quality hold and plant/tenant constraints apply to all receipts, including the quick endpoint.

This Green Ink design deliberately uses simpler binary, stage-based checks. Full five-type approved checklist designer, post-start surveys, attachments and material/truck applicability rules remain gaps. Truck visits link existing single-material POs; they do not introduce true PO header/line or split-delivery scheduling. Packed receiving posts directly into one new pallet, with no saved draft, pack-level labels or multiple pack sizes. No source ZIP analysis, ERP posting or device connection was performed.

See RECEIVING_CONTROL.md for actual contracts, operator steps, migration and verification. Follow-up priority: multiline PO/delivery scheduling and receipt sessions, invoice blind counts, material receiving profiles/pack rows, COA and sampling, weights/bulk/sand, import/STO/toller, advanced put-away and auditable returns. Open business-policy details above remain visible; existing safeguards are not relaxed to simulate parity.
