# Production, Quality and Finished Goods operator guide

This phase adds connected execution, laboratory and finished-pallet workflows. Existing Inbound and guided Material Preparation remain compatible. Use a selected operating plant throughout.

## First-time setup

1. Inbound → Master data: create raw/finished materials, suppliers and warehouse bins. Receive and release raw stock as appropriate.
2. Production → Orders & output: create an order with the actual whole-order raw requirements, then release it as a manager. Use Material Preparation to prepare/deliver/issue all requirements and obtain any required engineering approval.
3. Quality → Specifications: create numeric or pass/fail tests for the finished material, save the draft and publish it. Numeric tests require both limits. A published specification causes batch release to require passed laboratory evidence.
4. Production → Resources: add the actual production resources. Maintenance prevents new starts and resumes.
5. Production → SOP library: create a revision with its finished material, precautions and ordered phases. Each phase may require checklist checks, numeric process parameters and a passed laboratory sample. Publish the draft. Reusing the SOP code creates the next immutable revision; it does not rewrite active runs.

Configuration and publication require PlatformAdmin, TenantAdmin or PlantManager. Operators may execute/record/sample/pack; laboratory review, batch disposition, nonconformance closure and scheduling require manager roles. Viewers have read-only access. The API enforces these restrictions independently of UI buttons.

## Execute an order

Execution → Schedule execution links one released order, a published SOP for its product, a resource, schedule and priority. Only one execution run per order is allowed. Scheduling is a target time, not a calendar-slot reservation. Before start, managers can reschedule with a reason and refreshed row version.

Select the run. Confirm production notes, precautions and PPE, then start. All saved BOM quantities must already be issued; pending engineering approval and resource maintenance/occupancy block start. Running and paused orders occupy the resource until final output completes the run.

Start each phase in sequence. Record its actual checklist and process readings in **Record phase evidence**. Incomplete evidence blocks completion. Out-of-range readings are retained and open a quality deviation; recording a corrected value does not erase the earlier event or close the deviation. Refresh stale forms before saving.

If a phase requires a sample, collect it and use the Quality laboratory workflow below. The latest phase sample must be Passed. Any optional sample that was collected must also pass before that phase can complete. Review and close open run deviations before completion.

Pause with an actual reason when needed; resume records elapsed paused minutes. Evidence and phase transitions are blocked while paused. This is manual execution timing, not machine-derived availability/OEE. Legacy manual downtime remains in Orders & output; recording the same downtime there would be separate data.

Once all phases are complete, Orders & output → Complete production records **actual good output plus actual scrap equal to the order’s planned output**. Partial completion is not supported. Never enter fictional scrap to force this validation. Choose a configured finished bin; optionally supply a unique batch number and future expiry. Completion creates a quality-held batch/pallet, completes the run and frees its resource. Existing orders with no guided run retain the earlier whole-order flow.

## Laboratory workflow

Collect an in-process sample from its running Production phase. The run’s published specification is fixed when scheduled. To inspect incoming or finished stock, Quality → Laboratory → Collect batch sample uses the material’s latest published specification. A new batch sample puts on-hand batch stock on Hold; rejected stock cannot be resampled into a release.

Each sample progresses through **Collected → HandedOver → Received → Testing**. Record every required numeric or pass/fail test. Additional recordings remain visible; the latest reading for each test controls review. Out-of-spec readings open a nonconformance and keep batch stock held.

A manager reviews Testing samples with a justified Passed or Failed decision. Passed requires every required result in spec and no open target nonconformances. Reviewed samples lock their readings. For a failed sample, **Collect retest sample** creates a linked new sample with a reason while retaining the failed history. A correction while still Testing is a new reading, followed by manager-reviewed investigation; it does not overwrite old results.

Nonconformances → Review and close requires root cause, corrective action and resolution. Closing a case does **not** pass a sample or release a batch. Laboratory Passed also does **not** silently release stock. Batch decisions is the separate manager disposition; a published specification or collected batch sample requires the latest sample to have passed, and any open batch case blocks release. A later batch deviation immediately returns stock to Hold, blocking consumption/packing/dispatch through existing quality checks.

## Finished stock and packing

Finished Goods → Stock displays on-hand finished pallets individually, including product, batch, quality, bin, free/reserved quantity and expiry. No mixed-unit aggregate is presented.

**Pack pallet** requires released, unexpired finished stock. Scan/type the exact source pallet label; enter pack size, whole count, loose quantity smaller than one whole pack, and packaging reference. One posting creates one new pallet of the **same batch**. Source stock decreases by exactly the new pallet’s quantity; paired immutable movements reconcile to zero overall change. The free source balance cannot be exceeded. Packing does not consume packaging-material inventory or close part of an unfinished production order.

**Put away** requires the exact finished pallet code and a configured destination-bin code. The whole nonempty, unreserved released pallet moves; any configured buffer capacity applies. The transaction adds an immutable location movement. Use an actual keyboard barcode scanner or type codes; no device connection is simulated.

Packing history → Preview packed label displays product, original batch, packed quantity, pack breakdown, packaging reference, date and valid QR encoding the new pallet code. Quantity is the packing snapshot, not a live stock claim after later shipments. Browser printing is a local preview, not a physical printer acknowledgment. Professional printer layouts, print queues and sample labels remain further work.

## API and data rules

All endpoints require the existing session cookie and plant authorization. POST also requires `X-GreenInk-Request: 1`. Supply the same GUID `commandId` for a retry of unchanged input; changed input with the same identifier is rejected. Every manufacturing mutation is a serializable transaction and returns `{resultId}`. Keep refreshed `version` values for resource changes, reschedules and phase evidence.

Base: `/api/operations/manufacturing`.

| Route | Behavior |
|---|---|
| GET resources / recipes / runs / specifications / samples / quality-cases / packs | `{items,hasMore}`, at most 200 recent scoped records |
| POST resources | Create or version-checked update of code/name/maintenance |
| POST recipes; POST recipes/{id}/publish | Add immutable revision content; publish draft |
| POST runs; POST runs/{id}/reschedule | Schedule released order; reschedule only before start |
| GET runs/{id} | Scoped order/resource/SOP, full saved phases and bounded samples/cases/history |
| POST runs/{id}/state | Start/Pause/Resume with safety/reason gates |
| POST phases/{id}/start / evidence / complete | Ordered execution, snapshotted checks/parameter limits and laboratory gates |
| POST specifications; POST specifications/{id}/publish | Numeric or pass/fail test revisions and publication |
| POST samples; GET samples/{id} | Collect phase/batch/retest; snapshot tests and show complete readings up to the enforced 500-reading limit |
| POST samples/{id}/state / readings / review | Handover/Receive/StartTesting, append results, manager review |
| POST quality-cases; POST quality-cases/{id}/close | Quarantine/block target; manager-reviewed investigation |
| POST packs; GET packs/{id}/label | Same-batch split packing and local QR label preview |
| POST putaway | Scan-verified whole finished pallet movement |

Existing `/production-orders/{id}/complete` adds optional `batchNumber` and `expiryDate`, enforces saved guided phases when a run exists and retains backward compatibility for older orders. New completion commands also preserve a payload signature. Existing `/quality-inspections` enforces laboratory/nonconformance release gates even when called directly.

Additive migration `20261017144443_ManufacturingExecution` creates 11 scoped tables, JSON snapshots, composite FKs/check constraints, xmin/unique indexes and append-only triggers for ProductionEvent, QualityTestReading, FinishedPack and ManufacturingCommand. It does not rewrite existing stock, material-preparation credits, accounts or sessions. Production confirmation, receipt and movement history remain intact.

Lists and UI counts are explicitly bounded; metrics describe loaded recent records. Execution export includes only displayed events with CSV formula escaping. No files/source from the reference application are stored in this repository. No production data is seeded automatically.

## Updating Windows

After receiving the committed changes, retain `infrastructure/.env` and database volumes, and rebuild both API and web:

```powershell
git pull --ff-only
docker compose --env-file infrastructure/.env -f infrastructure/compose.yaml up -d --build
```

Do not run `down -v`. New APIs/tables require the API rebuild; rebuilding only web is insufficient. Complete plant UAT and production release checks separately. See MANUFACTURING_REQUIREMENTS.md for the unread-manual blocker and all remaining capability gaps.
