# Green Ink Dispatch guide

This phase adds an original shipment workspace to the existing Orders & shipments tab. It supports customer dispatch and local shuttle transfer-out. SAP, live RFID, scales, temperature telemetry and physical printing are not connected. See DISPATCH_REQUIREMENTS.md for exact manual coverage and gaps.

## Run a customer shipment

1. Select the operating plant. In Setup, create an area with different packing/loading bins, Single or Group picking and Product or Collective packing. Create a package with dimensions, tare/max gross and optional required temperature serial. Policies/packages are immutable by code; use a new code for a revised configuration.
2. Save customer instructions, minimum shelf-life days and optional product packing rule. Existing plans retain their instruction and policy snapshots. Create open delivery orders in Orders & shipments; current delivery orders contain one material each.
3. In Planning, group the delivery orders, assign an eligible operator, and enter date, priority and route reference. One active plan claims each delivery order. Managers may reassign/reschedule with a reason.
4. Start picking. Select released, unexpired, free finished stock. Confirm instructions/PPE, scan the exact source pallet and bin and enter quantity. Group areas can submit several lines together. Picks create separate reserved pallets at the packing bin; they do not consume inventory. Unknown expiry cannot satisfy a positive customer shelf-life requirement.
5. Reconcile every line, return any excess, then complete picking and start packing. Create a local HU using a package; capture its temperature serial when required. Scan each allocation label to pack it. One allocation belongs to one HU: use separate picks when dividing quantity. Product rules require separate allocation confirmations. Close each nonempty HU with its exact code and gross weight. Gross must fit package limits and cover known kg contents plus tare; non-kg content is not automatically converted.
6. Complete packing, enter truck/carrier and confirm shipping instructions. Scan each closed HU to load it. Finish loading only after every allocation is loaded.
7. A manager confirms local goods issue. Quality, expiry, quantities, reservation and locations are rechecked in the transaction. Customer delivery orders become shipped and local shipment/issue/movement history is recorded. The LOCAL-PGI reference is not an SAP posting.

## Corrections and returns

Managers can unload before issue, unpack, return picked stock to its original source bin with scanned label/bin and reason, or cancel after all stock is returned. Original movements remain visible. After issue, a whole-run reversal requires confirmation that stock physically returned and a reason. It restores the emptied picked pallets to the packing bin on quality hold, reopens customer delivery orders, preserves original shipment rows and appends reversal records. Net shipped reports exclude reversed shipments. Release returned batches through Quality only after review. Partial post-issue returns and external ERP reversal are not implemented.

For shuttle runs, configure a destination warehouse reference and material quantities instead of customer deliveries. Follow the same picking/packing/loading controls. Local issue records transfer-out; it does not acknowledge external receipt or create a customer shipment.

## Access, history and technical boundaries

Operators can execute only their assigned runs. Managers configure, assign and approve issue/returns/reversal; read-only users cannot mutate records. Every endpoint enforces tenant/plant scope. Replayed commands require the same payload and current authorization. Serializable transactions, xmin, command IDs, constraints and append-only database triggers protect balances and history. Existing quick shipment and whole-pallet transfer routes cannot bypass active Dispatch claims/reservations.

The API base is `/api/operations/dispatch`: configuration lists, operators, available-deliveries, runs, run detail/stock/state/assignment/picks/units/truck/loading-complete/goods-issue/reverse, pick return and HU pack/state/label routes. Use the UI for required request shapes and current versions. Lists are bounded (typically200 rows; run picks500); displayed counts/CSV refer to loaded records. Keep large-history pagination/export as a production backlog item. Local HU labels are QR previews, not GS1 SSCC certificates.

Migration `20261017154322_GuidedDispatch` adds twelve scoped tables and immutable-history triggers without rewriting earlier operations. Both API and web require rebuilding. The complete upgrade and prior workflows were checked against isolated PostgreSQL; Docker Desktop/UAT remain local release checks.

## Install on the Windows laptop

When direct cloud Git authentication is blocked, download the self-contained GreenInk-Dispatch.bundle. It includes all previous local phases, not database contents. In PowerShell run each command separately and stop on errors:

```powershell
cd "D:\GitHub\GreenInk-Technologies-Smart-Factory-MES-Platform"
git status --short
git fetch "$HOME\Downloads\GreenInk-Dispatch.bundle" HEAD
git merge --no-edit FETCH_HEAD
git push origin HEAD:main
```

Use an ordinary merge because the laptop's `bebd501` Inbound video commit may diverge from this phase; a fast-forward-only merge can fail. Preserve that commit and any local work. If Git reports conflicts, resolve and review them before pushing; never force/reset. Back up the database following BACKUP_RESTORE.md, retain the private `.env` and database volumes, then run:

```powershell
docker compose --env-file infrastructure/.env -f infrastructure/compose.yaml up -d --build
docker compose --env-file infrastructure/.env -f infrastructure/compose.yaml ps
```

Open http://localhost:8080 and refresh. Application source merges do not copy cloud test data or replace local accounts. Production readiness requires plant UAT, backup/restore validation and integration commissioning.
