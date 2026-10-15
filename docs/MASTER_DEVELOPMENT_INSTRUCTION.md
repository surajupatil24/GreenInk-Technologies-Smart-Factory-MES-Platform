# MASTER DEVELOPMENT INSTRUCTION
# GREEN INK TECHNOLOGIES — SMART FACTORY & MES PLATFORM

You are the **Principal Software Architect, AI Development Orchestrator, Senior Full-Stack Engineer, Database Architect, Manufacturing Domain Expert, QA Lead, Security Engineer, and DevOps Engineer** for GREEN INK TECHNOLOGIES LLP.

Your mission is to independently design, develop, test, document, and prepare for deployment a complete, enterprise-grade, multi-tenant Smart Factory and Manufacturing Execution System (MES).

**Project name:** GreenInk-Technologies-Smart-Factory-MES-Platform

**Product name:** GREEN INK SmartFactory

**Company:** GREEN INK TECHNOLOGIES LLP

**Primary objective:** Deliver a real, fully functional manufacturing application that supports the complete industrial material lifecycle, from inbound material receipt through inventory, staging, production, quality, finished goods, and dispatch.

This is a commercial software product, not a prototype, static dashboard, or collection of mock screens.

## 1. IMPORTANT DEVELOPMENT PRINCIPLES

1. Develop a completely original Green Ink application.
2. Do not access, copy, translate, or reuse proprietary Vegam source code, database scripts, screens, icons, branding, or proprietary business rules.
3. Use standard manufacturing processes, independently written requirements, and recognized industry concepts.
4. Do not introduce dependencies on Vegam software.
5. Use modern, maintainable, secure, and commercially suitable technology.
6. Build functional workflows, not just visual pages.
7. Do not falsely report that unimplemented features are complete.
8. Do not use fake API responses for production features.
9. Use realistic seed data only for explicitly identified development/demo environments.
10. Never hardcode passwords, API keys, or secrets.
11. Do not access folders or repositories outside this Green Ink project.
12. Do not publish, deploy externally, purchase services, or perform destructive operations without approval.

## 2. OPERATING ENVIRONMENT

Development environment:

- Windows 10/11
- 16 GB RAM
- Docker Desktop installed
- Git-connected repository
- Codex cloud environment available

The cloud environment and local Windows PC may have different installed tools and network access.

Inspect the actual environment before executing commands. Do not assume that Docker, databases, or Windows-specific paths are accessible from the Codex cloud environment.

Keep development resource usage appropriate for a 16 GB RAM workstation.

## 3. TECHNOLOGY ARCHITECTURE

Preferred technology stack:

**Frontend**
- React
- TypeScript
- Vite
- Tailwind CSS
- shadcn/ui
- TanStack Query
- React Router
- React Hook Form
- Zod

**Backend**
- ASP.NET Core on a supported LTS version
- C#
- Entity Framework Core
- REST APIs
- OpenAPI
- FluentValidation or equivalent

**Database**
- PostgreSQL for core MES transactional data
- TimescaleDB extension when high-volume machine telemetry is introduced
- Redis only when caching requirements justify it
- MongoDB only if a proven document workload justifies an additional database
- RabbitMQ for asynchronous integration and operational events
- S3-compatible object storage for files and reports

Avoid unnecessary complexity. Use a modular monolith initially with explicit domain boundaries.

**Authentication and Security**
- Secure login
- Role-based permissions
- Multi-tenant access isolation
- Plant-specific authorization
- Enterprise SSO/OIDC readiness
- Auditable privileged operations
- Secure session handling

**Testing**
- xUnit
- Integration tests
- Playwright end-to-end tests
- API contract tests
- Security checks
- Performance testing

**Infrastructure**
- Docker Compose for local development
- GitHub Actions for CI
- Environment-specific configuration
- Health checks
- Structured logging
- OpenTelemetry-ready observability

## 4. COMPLETE MES FUNCTIONAL SCOPE

Build the following modules as independently maintainable but integrated parts of one application.

### MODULE A — ADMINISTRATION & MASTER DATA

Implement:

- Tenant/company management
- Plant and site configuration
- Warehouse configuration
- Storage locations and bins
- Production areas and production lines
- Work centers and equipment
- User management
- Role and permission management
- Material master
- Units of measure
- Material categories
- Batch management rules
- Packaging types
- Supplier master
- Customer master
- Printer configuration
- Label template configuration
- Quality inspection configurations
- Shift calendars
- Downtime reason master
- Hazard and safety information
- Audit history

### MODULE B — INBOUND & GOODS RECEIPT

Implement:

- Purchase order creation and import
- Purchase order scheduling
- Supplier delivery registration
- Truck preregistration
- Gate entry and check-in
- Dock allocation
- Goods receipt transactions
- Material quantity validation
- Supplier batch capture
- Manufacturing and expiry date capture
- Quality inspection initiation
- COA attachment
- Material acceptance and rejection
- Raw material label generation
- Pallet creation
- Put-away instructions
- Goods receipt posting
- Receiving history and reports

### MODULE C — INVENTORY & WAREHOUSE

Implement:

- Real-time inventory overview
- Material stock by plant, warehouse, bin, batch and pallet
- Stock ledger
- Inventory reservations
- Material movements
- Bin-to-bin transfers
- Warehouse-to-production transfers
- Quality hold stock
- Blocked stock
- Released stock
- Cycle counting
- Stock adjustments with approval
- FEFO/FIFO allocation
- Pallet tracking
- Barcode-based inventory lookup
- Inventory reconciliation
- Inventory transaction history

Every inventory-changing operation must use appropriate database transactions and concurrency controls.

### MODULE D — MATERIAL STAGING

Implement:

- Production/process order import
- Production material requirements
- BOM-based material planning
- Material reservation
- Material picking
- Pre-weighing
- Barcode validation
- Batch allocation
- Material staging
- Kanban replenishment
- Repacking
- Material substitution with approval
- Shortage identification
- Production area inventory
- Material issue to production
- Unused material returns
- Staging history

### MODULE E — MANUFACTURING EXECUTION

Implement:

- Production order scheduling
- Production order release
- Resource and equipment assignment
- Batch manufacturing
- Operator work instructions
- Raw material consumption
- Production milestone confirmation
- Production quantity capture
- Process parameter capture
- Equipment status
- Shift reporting
- Downtime recording
- Scrap and yield reporting
- Production loss/gain
- Production completion
- Packaging execution
- Finished goods pallet creation
- Finished goods label printing
- Production order closure
- Production history

### MODULE F — QUALITY MANAGEMENT

Implement:

- Inspection plans
- Quality sampling
- Raw material inspection
- In-process inspection
- Finished goods inspection
- Quality specifications
- Test results
- Material release/rejection
- Nonconformance tracking
- Quality hold
- COA management
- Quality approval workflows
- Quality audit history

### MODULE G — FINISHED GOODS & DISPATCH

Implement:

- Finished goods inventory
- Sales order management
- Delivery order management
- Delivery planning
- Picking and packing
- Shipment preparation
- Pallet validation
- Dispatch label printing
- Truck assignment
- Loading verification
- Shipment confirmation
- ERP goods issue integration
- Dispatch completion
- Dispatch history
- Shipping documentation

### MODULE H — LABEL PRINTING & TRACEABILITY

This is a major product differentiator.

Implement:

- Label template management
- QR code generation
- Barcode generation
- Dynamic label variables
- Material label printing
- Pallet label printing
- Production label printing
- Finished goods label printing
- Shipment label printing
- Printer assignment
- Print job queue
- Print history
- Reprint authorization
- Barcode validation
- Unique pallet identification
- Batch genealogy
- Material-to-production traceability
- Production-to-customer traceability
- Forward and backward traceability

Design a printer adapter abstraction supporting future integrations with ZPL, BarTender, and other printing systems through authorized interfaces.

### MODULE I — ERP / SAP INTEGRATION

Implement an extensible integration framework supporting:

- SAP and other ERP systems
- REST/OData interfaces
- IDoc-compatible integration through approved connectors
- Material master synchronization
- Purchase order synchronization
- Production order synchronization
- Delivery order synchronization
- Inventory transaction posting
- Production confirmation
- Goods receipt posting
- Goods issue posting
- Integration status monitoring
- Error logs
- Retry mechanisms
- Idempotency and duplicate prevention
- Message correlation
- Dead-letter handling

Start with local test adapters and documented contracts. Do not claim live SAP integration without an actual SAP test environment and credentials.

### MODULE J — SMART FACTORY & ANALYTICS

Implement:

- Factory command center
- Plant performance dashboard
- Production progress
- Material shortages
- Warehouse utilization
- Order status monitoring
- Equipment utilization
- OEE reporting
- Downtime analysis
- Production yield
- Quality trends
- Dispatch performance
- Traceability reports
- Operator activity reports
- Historical reporting

Design interfaces for future PLC, SCADA, MQTT and industrial IoT connectivity.

Advanced AI capabilities may be added after reliable operational data is available.

## 5. UI/UX REQUIREMENTS

Create an original, premium-quality industrial software interface.

Design characteristics:

- Modern enterprise UI
- Fast navigation
- Responsive layouts
- Accessible forms
- Clear operational statuses
- Consistent design system
- Professional typography
- Light and dark themes
- Keyboard-friendly workflows
- Barcode scanner-friendly inputs
- Tablet-friendly operator interfaces
- Clear validation and error messages
- Loading and empty states
- Plant and tenant context selectors
- Role-based navigation

Main navigation:

1. Command Center
2. Inbound
3. Inventory
4. Material Preparation
5. Production
6. Quality
7. Finished Goods
8. Dispatch
9. Traceability
10. Label Studio
11. Integration Hub
12. Reports & Analytics
13. Administration

Do not reproduce the visual design of any proprietary third-party application.

## 6. DATABASE AND TRANSACTION REQUIREMENTS

Design a normalized relational schema with explicit relationships and constraints.

Include entities such as:

- Tenant
- Plant
- Warehouse
- StorageBin
- User
- Role
- Permission
- Material
- Supplier
- Customer
- PurchaseOrder
- PurchaseOrderLine
- GoodsReceipt
- GoodsReceiptLine
- Batch
- Pallet
- InventoryBalance
- StockMovement
- QualityInspection
- ProductionOrder
- BillOfMaterials
- MaterialReservation
- MaterialConsumption
- ProductionConfirmation
- FinishedGoodsLot
- DeliveryOrder
- Shipment
- LabelTemplate
- PrintJob
- IntegrationMessage
- AuditEvent

Important requirements:

- Tenant and plant isolation
- Strong referential integrity
- Idempotent transaction handling
- Immutable stock movement ledger
- Auditable correction transactions
- Optimistic concurrency where appropriate
- No negative inventory unless explicitly authorized by business policy
- Database migrations
- Indexing strategy
- Backup and restore documentation
- Transaction rollback tests
- Traceability relationships

## 7. AI DEVELOPMENT WORKFLOW

Operate as a disciplined software engineering team.

For each feature:

1. Review the existing repository.
2. Read the architecture and development rules.
3. Identify dependencies.
4. Define acceptance criteria.
5. Write a short implementation plan.
6. Implement backend logic.
7. Implement database migrations.
8. Implement frontend.
9. Add validation and permissions.
10. Write automated tests.
11. Execute builds and tests.
12. Fix identified failures.
13. Update documentation.
14. Report completion evidence.

Use task-based development and maintain an implementation backlog.

If subagents or parallel execution are available, use them only for isolated, nonconflicting tasks. Otherwise, execute tasks sequentially.

Do not run competing agents against the same files simultaneously.

Do not skip quality gates merely to increase development speed.

## 8. INITIAL REPOSITORY STRUCTURE

Create or adapt the repository to a structure similar to:

GreenInkSmartFactory/
- apps/
  - web/
  - api/
- modules/
  - administration/
  - inbound/
  - inventory/
  - staging/
  - manufacturing/
  - quality/
  - dispatch/
  - labeling/
  - integrations/
- database/
- infrastructure/
- tests/
- docs/
- ai-agents/

Adjust this structure where required by .NET solution conventions.

Create:

- README.md
- AGENTS.md
- CLAUDE.md
- docs/ARCHITECTURE.md
- docs/PRODUCT_REQUIREMENTS.md
- docs/DATABASE_DESIGN.md
- docs/MODULE_ROADMAP.md
- docs/IMPLEMENTATION_STATUS.md
- docs/DECISION_LOG.md
- docs/LOCAL_SETUP.md

## 9. IMPLEMENTATION STRATEGY

Do not attempt to generate the entire MES in a single change.

Work through the following milestones:

**Milestone 1 — Platform Foundation**
- Backend API
- Frontend application
- Database connection
- Authentication
- Role-based authorization
- Tenant and plant administration
- Application shell
- Command center
- Docker development environment
- Automated tests

**Milestone 2 — Inbound**
- Purchase orders
- Goods receipt
- Quality hold
- Material batches
- Pallet creation
- Label printing
- Put-away

**Milestone 3 — Inventory**
- Stock ledger
- Stock balances
- Warehouse movements
- Barcode scanning
- Inventory reconciliation

**Milestone 4 — Staging**
- Production material requirements
- Reservation
- Picking
- Pre-weighing
- Material issue

**Milestone 5 — Manufacturing**
- Production orders
- Material consumption
- Production confirmation
- Packaging
- Finished goods

**Milestone 6 — Dispatch**
- Delivery orders
- Picking
- Loading
- Shipment confirmation

**Milestone 7 — Enterprise Integrations**
- ERP connectors
- Messaging
- IoT adapters
- Operational monitoring

**Milestone 8 — Hardening**
- End-to-end testing
- Performance testing
- Security review
- Backup/restore testing
- UAT readiness

Each milestone must be demonstrable and tested before it is considered complete.

## 10. AUTOMATION AND CONTINUITY

Minimize unnecessary manual intervention.

For tasks within the approved development scope:

- Implement changes autonomously.
- Run tests automatically.
- Correct ordinary compilation and test failures.
- Maintain task progress in repository documentation.
- Prepare changes for review.
- Resume from documented project state in future sessions.

Do not assume the agent can continue indefinitely after a session ends. Persist implementation plans, decisions, progress, and next tasks in Git-tracked documentation.

Never automatically merge, publish, or deploy to production without approval.

## 11. DEFINITION OF DONE

A feature is complete only when:

- Backend logic is implemented.
- Database persistence is implemented where needed.
- Frontend workflow is functional.
- Authentication and authorization are enforced.
- Validation and error handling are present.
- Relevant automated tests pass.
- No critical security issue is known.
- Documentation is updated.
- The feature can be demonstrated.
- Remaining limitations are explicitly reported.

Do not mark placeholder pages as completed modules.

## 12. START WORK NOW

Begin immediately with the following actions:

1. Inspect the current Green Ink Git repository.
2. Identify existing files and development environment capabilities.
3. Create the master product requirements and architecture documentation.
4. Establish the initial implementation backlog.
5. Implement Milestone 1 — Platform Foundation.
6. Create the PostgreSQL development configuration.
7. Implement the backend and frontend application shells.
8. Implement initial tenant, plant, user and role management.
9. Implement secure authentication.
10. Create the initial Green Ink SmartFactory command center.
11. Add automated tests.
12. Run all available validation commands.
13. Correct implementation errors.
14. Commit or prepare the changes according to repository permissions and workflow.

If the environment lacks Docker, a database, or another required dependency, complete all feasible work, document the blocker, and provide exact local validation commands.

**Do not stop after creating only documentation or a development plan. Begin actual implementation in this task.**

At the end, provide:

- Features implemented
- Files created or modified
- Database migrations
- Tests executed and their results
- Commands to run the application locally
- Known limitations
- Next development milestone

**FINAL OBJECTIVE:** Build GREEN INK SmartFactory into an independently developed, reliable, commercially viable, enterprise-grade MES platform covering the complete manufacturing lifecycle.