# GREEN INK SmartFactory

An independently developed smart factory and MES platform for GREEN INK TECHNOLOGIES LLP. This change establishes the first platform foundation; it does **not** deliver the full MES lifecycle or production readiness.

The existing root Node/Express barcode project is retained unchanged. The new application lives in `apps/api` (ASP.NET Core 10) and `apps/web` (React/TypeScript/Vite), with PostgreSQL 17 persistence.

Start with [local setup](docs/LOCAL_SETUP.md), [implementation status](docs/IMPLEMENTATION_STATUS.md), and [architecture](docs/ARCHITECTURE.md). Product scope and milestones are recorded in [requirements](docs/PRODUCT_REQUIREMENTS.md) and [roadmap](docs/MODULE_ROADMAP.md).

## Local development

Install Docker Desktop with Compose v2. Copy `infrastructure/.env.example` to `infrastructure/.env`, fill in unique database and bootstrap administrator credentials, then run from the repository root:

```sh
docker compose --env-file infrastructure/.env -f infrastructure/compose.yaml up --build
```

Open http://localhost:8080. See the setup guide for native development, validation, and troubleshooting. Docker startup, PostgreSQL integration, and browser workflows require the validation recorded in the status document before being treated as verified.

Never commit `.env`, real credentials, customer data, or database backups. Do not deploy this foundation to production without hardening, independent security review, and acceptance testing.
