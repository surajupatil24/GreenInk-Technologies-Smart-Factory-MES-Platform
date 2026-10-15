# Database design

## Foundation and source of truth

PostgreSQL is the transactional store. EF Core entities and migrations in `apps/api` are the executable schema definition. The initial implementation covers identity, sessions, tenancy, plant access and audit foundations; inspect its migration for exact names and constraints. The entities below describe the planned MES schema and must not be mistaken for already delivered tables.

Tenants own plants. Users are authorized through tenant and plant membership and role policy; an identifier's existence does not grant access. Authentication uses an eight-hour protected cookie linked to a persisted UserSession row. Every request checks session expiry and reloads user grants. Logout deletes that session; password changes atomically revoke all user sessions. Audit events preserve actor, scope, action, subject and timestamp without secrets. Initial uniqueness rules and foreign keys must be supplemented by integration tests against PostgreSQL.

## Planned domain relationships

| Aggregate | Relationships and integrity |
| --- | --- |
| Warehouse / StorageBin | Belong to a plant and tenant; bin codes unique within warehouse |
| Material / UOM / Supplier / Customer | Tenant-owned masters, normalized references, explicit decimal precision |
| PurchaseOrder / Line | Supplier, plant, material, ordered/received quantities and lifecycle |
| GoodsReceipt / Line / Batch / Pallet | Source order line, supplier batch, dates, quantity/UOM, inspection disposition and unique pallet identity |
| InventoryBalance / StockMovement | Unique tenant/plant/location/material/batch/pallet/status stock key; immutable movements and auditable reversing entries |
| ProductionOrder / BOM / Reservation / Consumption / Confirmation | Versioned BOM and quantities, allocation and issue links, execution and output genealogy |
| QualityInspection | Plan/specification version, sample/test evidence, disposition and approver |
| FinishedGoodsLot / DeliveryOrder / Shipment | Production provenance through pallet pick/load to customer dispatch |
| LabelTemplate / PrintJob | Template version, scoped target, printer adapter, job status and authorized reprint history |
| IntegrationMessage | External ID and tenant uniqueness, payload reference, correlation, retry state and deduplication |

## Transaction and indexing rules

Every inventory-changing command must atomically write the movement ledger, balances, source document state and any outbox event. Use an idempotency key unique within tenant and operation; compare repeated request semantics before replaying a result. Use row locks or optimistic concurrency with bounded retry for contested balances. Reject insufficient stock unless a documented authorized policy allows it. Persist reversals rather than editing posted movements. Test rollback and concurrent posting with real PostgreSQL.

Use composite scope-aware foreign keys or equivalent database constraints to prevent cross-tenant and cross-plant references. Index tenant/plant prefixes with document status/date and natural lookup keys; unique constraints enforce business identity rather than relying on application prechecks. Use UTC instants, fixed-precision decimals and explicit UOM conversions; avoid floating-point stock quantities. Define retention and partitioning only after real audit/telemetry volume is measured.

## Migration and recovery

Review generated SQL before release. Never silently drop tenant data or edit an already applied shared migration. Validate both empty-database creation and upgrades with representative data; back up before release and test recovery. See [BACKUP_RESTORE.md](BACKUP_RESTORE.md) for a local logical recovery procedure. Production RPO/RTO, encryption, retention, offsite storage and scheduled restore drills remain deployment decisions.

## Operational core schema

`OperationsLifecycle` adds plant-scoped material, partner, bin, PO, receipt, batch, pallet, movement, order/BOM requirement, reservation, consumption, production confirmation, downtime, inspection, delivery, shipment, label template/job and integration message entities. Composite `(TenantId, PlantId, Id)` principal keys and composite foreign keys prevent cross-scope references in PostgreSQL, independently of route checks. Every row has an xmin-backed optimistic version. Decimal quantities use precision20,scale6; database checks enforce positive ordered/posted quantities and pallet `Quantity >= ReservedQuantity >= 0`. Scoped natural/command keys enforce duplicate prevention.

Stock movements are append-only in DbContext and through a PostgreSQL trigger rejecting UPDATE/DELETE. Corrections append adjustment movements. Transfers retain quantity with zero-delta location history. Receipts, issue, production and shipment write balances, state, ledger and audit atomically under serializable isolation. Read-only genealogy and reports use repeatable-read snapshots.

`LabelPrintSnapshots` adds rendered content and template revision to print jobs. Reprints preserve the original content rather than re-rendering current stock quantities. Legacy jobs with no snapshot are explicitly identified. See OPERATIONS_GUIDE.md for command-specific idempotency and the remaining broader schema requirements.
