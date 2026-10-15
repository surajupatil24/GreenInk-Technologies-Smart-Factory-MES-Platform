# Local setup

## Docker Desktop on Windows

Use Docker Desktop's Linux containers and Compose v2. Clone/pull this repository to your Windows development PC; cloud workspace changes do not automatically modify your PC. Do not use the legacy root `npm start` to launch the new MES.

From the repository root in PowerShell:

```powershell
Copy-Item infrastructure/.env.example infrastructure/.env
notepad infrastructure/.env
docker compose --env-file infrastructure/.env -f infrastructure/compose.yaml config --quiet
docker compose --env-file infrastructure/.env -f infrastructure/compose.yaml up --build
```

Fill every empty value with your own credentials before starting. Use a long random database password and an administrator password with at least 14 characters, uppercase and lowercase letters, and a digit. Prefer alphanumeric database passwords for this local connection-string configuration; semicolons require correct connection-string quoting. Bootstrap email and display name identify your initial administrator. Never paste the populated file into a chat or commit it.

Open http://localhost:8080 and sign in with your configured bootstrap account. PostgreSQL and the API are internal to the Compose network. The development API applies migrations at startup. Initial startup may take time while database readiness and image downloads complete. The database volume persists across container restarts. Changing bootstrap variables is not a password-reset workflow for an existing user.

```powershell
docker compose --env-file infrastructure/.env -f infrastructure/compose.yaml ps
docker compose --env-file infrastructure/.env -f infrastructure/compose.yaml logs --tail 100 api
docker compose --env-file infrastructure/.env -f infrastructure/compose.yaml down
```

Do not add `--volumes` when stopping unless intentionally deleting local data. Logs may contain operational identifiers; review before sharing. This is local development HTTP configuration. External deployment requires TLS, cookie/proxy review, secret management, migration governance and security hardening.

## Native development

Install .NET SDK 10, Node.js 22, and PostgreSQL 17. Create an empty development database and set its connection string using private environment variables. From PowerShell:

```powershell
$env:ConnectionStrings__Database='Host=localhost;Port=5432;Database=greenink;Username=YOUR_USER;Password=YOUR_PASSWORD'
$env:Bootstrap__Email='YOUR_EMAIL'
$env:Bootstrap__Password='YOUR_UNIQUE_PASSWORD'
$env:Bootstrap__DisplayName='YOUR_NAME'
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ASPNETCORE_URLS='http://localhost:5080'
dotnet run --project apps/api/GreenInk.Api.csproj
```

In a second terminal, run:

```powershell
cd apps/web
npm ci
npm run dev
```

Use Vite's displayed local address. Its `/api` proxy targets the API development port; inspect `apps/web/vite.config.ts` if you change that port. Do not commit credentials to launch settings or source files.

## Validation commands

From the repository root:

```powershell
dotnet restore tests/GreenInk.Api.Tests.csproj
dotnet build tests/GreenInk.Api.Tests.csproj --configuration Release
$env:GREENINK_TEST_DATABASE='Host=localhost;Port=5432;Database=postgres;Username=YOUR_TEST_DB_ADMIN;Password=YOUR_TEST_PASSWORD'
dotnet test tests/GreenInk.Api.Tests.csproj --configuration Release
npm --prefix apps/web ci
npm --prefix apps/web run build
```

The integration-test database account must have CREATE DATABASE permission. Each test creates and drops its own uniquely named database; use a dedicated local test instance. The tests fail with a clear error if GREENINK_TEST_DATABASE is missing.

With the native API running, execute the real-browser workflow:

```powershell
$env:GREENINK_E2E_EMAIL=$env:Bootstrap__Email
$env:GREENINK_E2E_PASSWORD=$env:Bootstrap__Password
cd apps/web
npx playwright install chromium
npm run test:e2e
```

The E2E test writes uniquely identified validation organizations, plants and users to the configured development database. Use a disposable development database; these records remain for review. Never run it against production. `GREENINK_BROWSER_PATH` can select an existing Chromium executable when browser downloads are unavailable.

Then verify Compose startup from a fresh database, login/logout, session expiration, role restrictions, tenant and plant isolation, persistence after restart and audit records. Builds alone do not substitute for PostgreSQL integration and browser E2E tests. Record executed results in `IMPLEMENTATION_STATUS.md`.

If a port is occupied, stop the conflicting local process or change the published web port. If API startup fails, inspect database connectivity, migration logs and bootstrap validation. If cloud tooling or network access prevents restore/build, run these exact checks locally and retain the output for review.

## Managed cloud build notes

The cloud run validated the native API with PostgreSQL and built the web container. The API image pull was blocked at `westcentralus.data.mcr.microsoft.com` by network policy, so complete Compose startup remains unverified. No proxy bypass was attempted. On this managed workspace, .NET required writable `DOTNET_CLI_HOME`, `NUGET_PACKAGES`, and `XDG_RUNTIME_DIR` directories under `/tmp`.

Dockerfiles support an optional BuildKit `proxy_ca` secret for package restore/install when a managed proxy requires its CA. Supply a combined trusted CA bundle using `docker build --secret id=proxy_ca,src=/path/to/combined-ca.pem ...`. The certificate is mounted only during the network step and is not copied into the image. Standard Windows Docker builds do not require this secret.

## Operational update

Pull the latest main branch and rebuild **both API and web** for the operational lifecycle and its migrations:

```powershell
cd "D:\GitHub\GreenInk-Technologies-Smart-Factory-MES-Platform"
git pull --ff-only origin main
docker compose --env-file infrastructure/.env -f infrastructure/compose.yaml up -d --build
```

Keep the existing private `.env` and PostgreSQL volume. Do not use `down --volumes`. The Development API applies `OperationsLifecycle` and `LabelPrintSnapshots` migrations to the existing database. A baseline-to-latest upgrade regression verifies tenant, plant, account, password hash, grant and session records survive. Back up the local database before upgrades, using BACKUP_RESTORE.md. API Data Protection keys now use an `api_keys` named volume; existing ephemeral cookies can require one new login when first moving to the volume. This development key store is not encryption-at-rest governance.

After login, select an authorized plant, open Inbound and configure Materials (Raw/Finished), Supplier/Customer partners and bins. See OPERATIONS_GUIDE.md for the connected receive→release→reserve→issue→produce→release→ship workflow. Lists are bounded to 200 records; trace explicitly flags incomplete results. No demo records are automatically seeded. Labels use local text/QR previews; integration validation does not send messages to an ERP.

Browser validation can use `GREENINK_E2E_PORT` and `API_PROXY_TARGET` to select a disposable Vite port and API address. The operations E2E suite creates uniquely identified test organization/plant/master/transaction data in the development database.
