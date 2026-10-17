# Material Preparation workspace

## Use the workspace

1. Receive raw material in Inbound, release its batch in Quality, and configure warehouse/staging/supply bins. Create and release the production order with its saved BOM in Production.
2. Material Preparation→Setup: a manager creates a staging area, selecting different staging/supply bins and Separate/Together/PickOnly workflow. Used areas keep their bins/workflow; create a different area to change these safely.
3. Save material pack/preweigh/tolerance/PPE/handling rules. Where correction factors are required, save an evidenced factor per raw batch. Existing picks keep their rule/factor snapshots. Missing factors block that batch. Open pick forms submit the versions of the rules/factors they displayed; a manager change requires refreshing and reviewing the instructions before confirmation.
4. Orders & kits: select the order, Schedule preparation, choose area/time/priority and any engineering approval/reference. Pending approval blocks picking. Open quick reservations must be cancelled before adopting a guided plan.
5. Prepare material: select a FEFO-suggested released batch, scan its source bin and material label, set the nominal BOM allocation and physical quantity, and acknowledge safety instructions. For Separate, bring enough stock to staging (a full bag can exceed demand); Together/PickOnly enter gross/tare and confirm a tolerance-valid allocation directly to supply.
6. Separate workflow: Kit contents→Weigh & kit. Enter actual gross/tare; target/range and net weight remain visible. Confirm creates a dedicated labelled kit portion; loose remainder stays free at staging. Full-bag quantity can be entered manually where measurement is not otherwise required. No live scale reading is claimed.
7. When all required materials are allocated/kitted, Deliver kit using its kit code and configured supply-bin scan. Together/PickOnly allocations already arrive at supply. QR kit identity supports local browser printing; no physical printer driver is connected.
8. Issue to production consumes the reserved physical stock once. Its recorded nominal credit satisfies the saved BOM. Production completion retains the actual good+scrap and material gates. Return allocation reverses only an unissued allocation into a selected warehouse bin; it does not reverse already consumed material.

Orders & kits has material readiness, kit contents and activity/weight views. Search and state filters help select work. The desktop queue becomes a horizontal order picker on small screens; action dialogs support keyboard cancel/close, labelled inputs and visible feedback. Dark/light themes retain the company branding.

## Stock and supply

- Replenishment: request RMShortage/Immediate/Kanban material with target bin, quantity, reason and optional plan. Deliver physical quantities from an eligible source; partial delivery is supported. Only a request with no delivered stock can be cancelled. The server prevents a cancelled request from being fulfilled by an already-open form.
- Kanban buffers: managers configure static thresholds or outstanding demand of open plans mapped to an area's staging/supply bin. Low/reorder/max levels and open deliveries determine the proposed request. Demand mode uses uncorrected nominal BOM units; correction-factor materials use static buffers. Shift/selected-order windows and trolley replenishment are not implemented.
- Stock & issues: inspect raw label/bin/physical/reserved/quality data. Transfer only free quantity using source-bin confirmation; a new label and balanced ledger pair preserve the split. Report a missing/batch/bin issue or an explicit loss/gain count. Open issue labels leave picking suggestions until reviewed.
- Managers can close an issue without changing stock, or apply a verified loss/gain count. Applying a count is rejected if system stock changed since the report or the count would undercut reservations. Historical ledger entries are retained.

Local Kanban maximums gate inbound preparation/replenishment/transfer movement. There is no silent above-max override. Counters summarize the loaded window; no mixed-UOM inventory sum or fabricated machine status is shown.

## API contracts

Authenticated prefix `/api/operations/staging`. GET includes `?plantId=...`; POST includes `plantId`, mutations require `X-GreenInk-Request: 1`. Command mutations include GUID `commandId`, retained for an unchanged retry. Viewer is read-only. Manager writes include areas/rules/factors/plans/Kanban and discrepancy resolution. Other scoped writers may execute preparation/supply actions. Existing broad roles remain; production-line/operator assignment is not a separate grant model.

| Routes | Main behavior |
| --- | --- |
| GET/POST `/areas` | Code/name/stagingBinId/supplyBinId/workflow; upsert unused area by code |
| GET/POST `/rules` | materialId/packSize/preweighRequired/tolerancePercent/correctionRequired/safetyNotes/handlingNotes |
| GET/POST `/factors` | batchId/factor/evidence; manager-entered, not SAP lookup |
| GET `/orders` | Latest200 order/workflow summaries with truncation flag |
| POST `/plans` | productionOrderId/stagingAreaId/scheduledAt/priority/approvalRequired/approved/approvalNotes; current-state schedule save |
| GET `/plans/{id}` | Scoped BOM credits, rules, picks/reservations, prepared labels, FEFO sources, measurements and history |
| POST `/plans/{id}/pick` | palletId/palletCode/sourceBin/quantity/requirementQuantity/ruleVersion/factorVersion; optional gross/tare/scaleReference; safetyConfirmed/commandId |
| POST `/picks/{id}/weigh` | gross/tare/scaleReference/commandId; Separate only |
| POST `/plans/{id}/putaway` | kitCode/destinationBin/commandId; complete kitted BOM to configured supply |
| POST `/picks/{id}/return` | destinationBin/reason/commandId; unissued allocation only |
| GET `/plans/{id}/label.svg` | Authorized QR identity; local browser print is separate from physical printing |
| POST `/transfers` | palletId/sourceBin/destinationBin/quantity/reason/commandId; free stock, split conservation |
| GET/POST `/requests` | materialId/destinationBinId/optional stagingPlanId/quantity/kind/reason/commandId |
| POST `/requests/{id}/fulfil` | palletId/sourceBin/quantity/commandId; physical partial delivery |
| POST `/requests/{id}/cancel` | reason/commandId; before first delivery |
| GET/POST `/stock-issues` | palletId/kind/notes/optional countedQuantity/commandId |
| POST `/stock-issues/{id}/resolve` | applyCount/resolution/commandId; manager review |
| GET/POST `/kanban` | Dashboard; binId/materialId/mode/lowStock/reorderStock/maxStock policy |

Existing `POST /api/operations/reservations/{id}/issue` issues guided reservations only after verified supply delivery; it records physical `Quantity` and nullable `RequirementQuantity`. Old records use physical quantity as their default requirement credit. Guided quick cancellation/full transfer is blocked; use the preparation return/delivery workflow. Production readiness uses nominal credit; physical reporting/traceability retains actual quantities. See MATERIAL_PREPARATION_REQUIREMENTS.md for the explicit correction/tolerance design and outstanding plant UAT decisions.

## Update and limits

Migration `20261017085223_MaterialPreparation` adds ten scoped operational tables plus nullable reservation/consumption credits and reservation→pick linkage. Composite foreign keys, indexes/xmin and quantity checks apply. Old receipts, balances, ledger rows, accounts, branding and unplanned quick workflows remain. Back up the database, update Git, rebuild both API and web; retain `.env` and database volumes. No production deployment is performed here.

The uploaded source ZIP could not be read (>32MiB tool limit). A smaller source-only archive is needed for implementation-logic comparison. Full scope remains open: group picking, resource/calendar/shift configuration, sophisticated rounding, preprocessing ovens/thawing/preservation, ASRS/AGV, external warehouses/transfer sheets/STO/DN/kit receipt, spreadsheet templates/imports, fine-grained production-area grants, physical device/printer/ERP adapters and release hardening. No inactive UI tab pretends those features exist.
