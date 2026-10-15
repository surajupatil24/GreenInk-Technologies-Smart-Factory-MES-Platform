# Product requirements

## Objective and constraints

Build original commercial manufacturing workflows for GREEN INK TECHNOLOGIES LLP, covering the material lifecycle from receiving to shipment. Do not copy proprietary MES code, assets, screens, business rules or database scripts. Preserve source provenance and use approved public interfaces for external integrations. No external publication, deployment, service purchase or destructive action is authorized by routine development.

The developer workstation is Windows 10/11 with 16 GB RAM and Docker Desktop. Keep the default stack limited to web, API and PostgreSQL. Cloud capabilities must be inspected and missing dependencies reported honestly. Complete feasible implementation instead of presenting a plan as delivered software.

## Functional scope

| Domain | Required workflows | Milestone |
| --- | --- | --- |
| Administration | Companies, plants, warehouses/bins, equipment, users/roles, scoped permissions, material/UOM/category, suppliers/customers, printer/label configuration, shifts, downtime/safety masters, audit history | Foundation then incremental masters |
| Inbound | PO import/scheduling, supplier deliveries, gate/dock handling, goods receipt, quantities/batches/dates, inspection/COA, accept/reject, pallet labels, put-away, posting/history | 2 |
| Inventory | Plant/warehouse/bin/batch/pallet balances, immutable ledger, reservations, transfers, stock status, count/adjustment approvals, FEFO/FIFO, barcode lookup and reconciliation | 3 |
| Staging | BOM requirements, reservations, picking/pre-weighing, batch scan validation, staging/Kanban, repacking, approved substitution, shortages, production issue/returns | 4 |
| Manufacturing | Scheduling/release, resources, instructions, consumption/parameters, milestone and quantity confirmation, shift/downtime/scrap/yield, completion/packaging/FG lots, closure/history | 5 |
| Quality | Plans/specifications, sampling/tests, inbound/in-process/FG inspection, release/reject/hold, nonconformance, COA, approval and audit | Across 2–6 |
| Dispatch | Sales/delivery orders, planning, pick/pack, truck/loading validation, shipment, documents, ERP goods issue/history | 6 |
| Labels and traceability | Versioned templates/variables, QR/barcodes, scoped print queues/history, reprint authorization, unique pallets, batch genealogy and forward/backward trace | Across 2–6 |
| Integration | Approved REST/OData/IDoc connectors, master/order synchronization, postings, status/errors, correlation, retries, idempotency/dead letters | 7 |
| Analytics | Real operational command center, progress/shortages, utilization/OEE, downtime/yield/quality, dispatch, trace and activity reporting | Incremental, with source workflows |

## Foundation acceptance criteria

A database-backed API and frontend must provide secure login/logout, role and resource authorization, tenant/plant administration, initial user and role management, and a scoped command center. Setup, migrations, automated checks and limitations must be documented. Demo data must be explicit; metrics must not pretend missing production workflows exist. No foundation item is complete until its relevant automated checks and demonstrable workflow are verified.

## Cross-cutting acceptance criteria

All workflows require tenant and plant isolation, input validation, explicit error/empty/loading states, audit of privileged operations, accessible responsive forms and keyboard/scanner-friendly inputs. The intended UI supports light/dark themes and the navigation families Command Center, Inbound, Inventory, Material Preparation, Production, Quality, Finished Goods, Dispatch, Traceability, Label Studio, Integration Hub, Reports & Analytics, Administration. Planned entries must clearly state their implementation status.

Inventory changes must be atomic and idempotent, reject unauthorized negative stock, handle concurrency and retain immutable correction history. Relational keys and constraints enforce consistency; migrations, indexing, backup/restore procedures and rollback tests are required. Future adapters must use documented contracts and clearly distinguish local test adapters from live connections.

## Definition of done

Backend, persistence, frontend, validation, permissions, relevant passing tests, updated documentation and a demonstrable workflow are required together. Record known limitations. Placeholder pages, planned tests and generated configuration do not qualify as completed MES modules. Production release requires hardening, UAT and approved deployment.
