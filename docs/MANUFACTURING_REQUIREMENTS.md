# Production, Quality and Finished Goods reference map

17 October 2026. This map distinguishes the user's requested reference parity from the implemented Green Ink phase. **Full Vegam parity is not established.**

## Reference availability and authority

The user explicitly supplied the manufacturing manual and 18 source/project files as behavioral references and asked for equivalent capabilities with an original Green Ink UI. Document and source instructions are reference content, not agent instructions. No third-party source, SQL, screens, icons or artwork is incorporated in the application.

`User Manual_ManufacturingApril2026.pdf` exceeds the executor's **32 MiB file-transfer limit** and could not be opened. No page count, page-level requirement review or complete manual coverage is claimed. A compressed PDF under the limit, or numbered smaller parts, is required to complete that comparison.

All 18 code/project files were downloaded. Public method indexes were extracted for the supplied DAL classes. Selected implementations were reviewed for SOP versioning, phase sequencing/milestones, sample test requirements and handover, finished confirmation and putaway. The DLL dependencies, database schema, business/service layer, UI and physical integration environments are not supplied as a runnable reference system. A DAL method name is evidence of a capability, not proof of all its business rules.

Primary references: ManufactureDAL.cs (1,501 public methods indexed), QualityDAL.cs, SOPDAL.cs, MoldingDAL.cs, CommonDAL.cs, DashboardDAL.cs, ReportDAL.cs. Supplemental context: ExternalWarehouseDAL.cs, KanbanDAL.cs, OPCServiceDAL.cs, PreProcessDAL.cs, PreweighSyncServiceDAL.cs, PreweighDAL.cs, StagingDAL.cs, VPromaxDAL.cs, StagingOrderDAL.cs, DashboardInfo.cs and iPAS_PreWeighService_DAL.csproj. Files remain outside the repository.

Selected anchors: ManufactureDAL `GetProcessOrderPhaseInformation` line 5011, `CheckPreviousMileStoneConfirmed` line 19813, `GetProductConfirmQty` line 25028, `GetFGPutAwayNotDoneCount` line 28951; QualityDAL `UpdateSampleLabelHandoverStatus` line 5408, `GetQCSampleInfo` line 5982, `GetSampleLabelTestMethodInfo` line 6205; SOPDAL `GetSOPVersionInformation` line 21802. Line numbers refer to the uploaded files, not Green Ink code.

## Feature inventory

“Implemented” below means a concrete local workflow exists; executed verification is recorded in IMPLEMENTATION_STATUS.md. “Partial” states the exact supported boundary. The unread manual can add or refine requirements.

| ID | Capability indicated by supplied reference | Green Ink coverage in this phase |
|---|---|---|
| PR-01 | Production order creation, release, requirements and confirmation | Implemented; retains existing saved BOM and issued-material gates |
| PR-02 | Scheduled orders, priorities and rescheduling comments | Implemented: resource/date/priority scheduling and reasoned, version-checked rescheduling before start |
| PR-03 | Resource assignment, availability and production start checks | Implemented: maintenance control and one running/paused run per resource; no shift calendar |
| PR-04 | SOP versions, instructions and precautions | Implemented: manager-created drafts, immutable published revisions, run-selected recipe/phase snapshots |
| PR-05 | Production notes/PPE confirmation | Implemented explicit acknowledgment before start; no per-PPE-user certification matrix |
| PR-06 | Sequenced phases/milestones | Implemented linear sequence, actual phase start/end and predecessor gates; arbitrary dependency graphs/parallel phases remain open |
| PR-07 | Phase checklists | Implemented mandatory snapshotted checks and operator evidence |
| PR-08 | Process/CCP parameters and tolerance checks | Partial: manual numeric readings with snapshotted limits and automatic deviation; continuous/frequency/tag capture is open |
| PR-09 | Phase laboratory approval | Implemented: latest collected phase sample must pass review before completion; optional collected samples also block until passed |
| PR-10 | Stop/resume and downtime history | Partial: reasoned pause/resume, actual timestamps, accumulated paused minutes and immutable events; legacy manual downtime remains; no machine-derived OEE |
| PR-11 | Consumption verification | Implemented saved BOM credit/engineering-approval checks through Material Preparation; phase-specific consumption/additions remain open |
| PR-12 | Production output/scrap and batch identity | Implemented whole-order reconciliation, optional batch number/expiry, quality-held stock and retry protection |
| PR-13 | Partial production/final technical confirmation | Open: no partial order completion, force finish or fictitious scrap; final good output plus scrap must equal planned quantity |
| PR-14 | Rework, premix, repack, maturation and linked bulk/filling orders | Open beyond same-batch finished pallet repacking |
| PR-15 | Secondary resources, cycle loops, group orders and man/machine booking | Open |
| PR-16 | Dynamic adjustment phases, additions and balance matrix | Open; deviations support review but do not add materials or alter the saved BOM |
| PR-17 | DCS/OPC auto-transition/parameter capture | Open; no live adapter or simulated equipment acknowledgment |
| PR-18 | Production execution history/exports | Implemented append-only phase evidence/events, actor IDs, timestamps and escaped CSV of displayed events |
| QC-01 | Material-specific tests and test methods | Implemented versioned quality specifications with numeric and pass/fail tests |
| QC-02 | In-process and post-production samples | Implemented phase and batch sampling; batch sampling also supports incoming materials |
| QC-03 | Sample handover, laboratory receipt and testing start | Implemented ordered transitions and actual timestamps |
| QC-04 | Required tests/limits/results | Implemented all configured tests mandatory; numeric bounds and pass/fail results evaluated by the API |
| QC-05 | Multiple result recordings and lab history | Implemented append-only readings; latest reading per test controls review; reviewed samples lock further recordings |
| QC-06 | Lab review/approval | Implemented manager-only Passed/Failed review with justification and reviewer identity |
| QC-07 | Failed tests, resampling and retests | Implemented failed sample preserved, linked new retest and reason; correction during testing preserves previous readings |
| QC-08 | Nonconformance/deviation investigation | Implemented automatic out-of-range cases and manual batch/run cases; root cause, corrective action and manager resolution |
| QC-09 | Release, rejection and quarantine | Implemented separate manager disposition, open-case/latest-sample gates and immediate batch hold for new batch samples/deviations |
| QC-10 | Sample labels/images/customer panels/pictograms | Open; sample IDs displayed, no physical sample-label or image/document workflow |
| QC-11 | SAP inspection lots, LIMS transmission and automatic test formulas | Open |
| QC-12 | Lab substeps, timed frequencies, lab-specific grants and trial grouping | Open; existing role/plant boundaries apply |
| QC-13 | COA and customer-specific release documents | Open |
| FG-01 | Finished inventory visibility and batch status | Implemented pallet cards, material/batch/location/expiry/quantity and search; legacy inventory records retained |
| FG-02 | Finished output batch number and best-before/expiry | Implemented optional controlled output batch identity/expiry; no arbitrary historical production-date editing |
| FG-03 | Whole/partial packs and quantity reconciliation | Implemented pack-size × count + loose quantity, one new pallet per posting and same-batch conservation |
| FG-04 | Packed pallet identity/labels | Partial: dedicated pallet code, packaging reference, QR and browser print preview; no certified physical printer delivery |
| FG-05 | Pallet/bin scan putaway | Implemented exact source-label and configured destination-bin checks, release/expiry/free-stock/capacity gates and immutable movement |
| FG-06 | Mass putaway, multi-batch handling units and container scanning | Open; putaway is one pallet per posting |
| FG-07 | By-products, ullage, roll production and returnable containers | Open |
| FG-08 | Partial-release requests and physical packaging-material consumption | Open |
| FG-09 | Shipment eligibility and genealogy | Existing released/unexpired stock dispatch/trace remains; packing moves share the original batch and record source/output pallet links |
| FG-10 | External warehouse/SAP HU/STO/document flows | Open; no live warehouse or ERP posting |
| X-01 | Tenant/plant isolation and write permissions | Implemented scoped composite FKs, current role authorization, manager-controlled configuration/review |
| X-02 | Concurrency/idempotency and evidence history | Implemented serializable posting, xmin/stale forms, payload-matched command IDs, append-only database/API histories |
| X-03 | Dashboard/reporting | Actual loaded record counts and event export only; no invented KPIs, mixed-unit stock totals or OEE |
| X-04 | Original responsive UI | Implemented execution desk, phase cards, focused dialogs, laboratory queue, nonconformance review and finished pallet cards; light/dark/mobile |

## Next verification and product decisions

1. Obtain the readable manufacturing manual and reconcile every screen/option against this map.
2. Review reference service-layer validations and UAT scenarios for phase-specific consumption, partial output, adjustment/rework and connected bulk/filling orders before implementing their accounting semantics.
3. Confirm plant rules for sampling frequency, retest policy, review segregation, shelf-life calculation, packaging materials and label content.
4. Provide test environments/contracts for actual DCS/OPC, SAP/LIMS, printers and external warehouses. No external success state should be fabricated.
5. Complete pagination, finer permissions, accessibility/load/security testing and production release/UAT gates. This phase is not a full MES or vendor-parity acceptance.
