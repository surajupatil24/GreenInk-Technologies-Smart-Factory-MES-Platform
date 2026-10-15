# GREEN INK SmartFactory

An independently developed smart factory and MES platform for GREEN INK TECHNOLOGIES LLP. The application now includes a tested operational core from raw-material receipt through production, quality and shipment. It does **not** complete the full enterprise MES scope or production hardening.

The existing root Node/Express barcode project is retained unchanged. The new application lives in `apps/api` (ASP.NET Core 10) and `apps/web` (React/TypeScript/Vite), with PostgreSQL 17 persistence.

Start with [local setup](docs/LOCAL_SETUP.md), [implementation status](docs/IMPLEMENTATION_STATUS.md), and [architecture](docs/ARCHITECTURE.md). Product scope and milestones are recorded in [requirements](docs/PRODUCT_REQUIREMENTS.md) and [roadmap](docs/MODULE_ROADMAP.md).

## Local development

Install Docker Desktop with Compose v2. Copy `infrastructure/.env.example` to `infrastructure/.env`, fill in unique database and bootstrap administrator credentials, then run from the repository root:

```sh
docker compose --env-file infrastructure/.env -f infrastructure/compose.yaml up --build
```

Open http://localhost:8080. See the setup guide for native development, validation, and troubleshooting. The current suite passes18 PostgreSQL/API tests and2 real-browser workflows; see implementation status for exact evidence and the remaining cloud Docker image restriction.

Never commit `.env`, real credentials, customer data, or database backups. Do not deploy this foundation to production without hardening, independent security review, and acceptance testing.

## Operational workflows

Select a plant and open Inbound to configure raw/finished materials, suppliers/customers and bins. Follow the [operations guide](docs/OPERATIONS_GUIDE.md) for receiving, inventory, reservation/issue, production, quality, delivery/shipment, traceability, local label QR previews, local integration validation and reports. Existing installation upgrades require rebuilding **both API and web**; retain private environment configuration and database volumes. Advanced requirements remain in the backlog, and no live printer/SAP/device connection is claimed.
