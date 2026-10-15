# Decision log

| Date | Decision | Reason and consequences |
| --- | --- | --- |
| 2026-10-15 | Preserve root Node/Express source and build new runtime in `apps/` | Existing barcode source is small and incomplete as an MES; replacement is isolated and reversible. Legacy startup is outside new validation. |
| 2026-10-15 | ASP.NET Core 10 LTS, React/TypeScript/Vite and PostgreSQL 17 | Matches requested supported .NET architecture with a single transactional store. SDK/tool availability must be checked rather than assumed. |
| 2026-10-15 | Modular monolith first | Enables atomic manufacturing operations without distributed services. Add module boundaries as domain workflows arrive. |
| 2026-10-15 | No Redis, RabbitMQ, TimescaleDB, MongoDB or S3 service in foundation | No implemented workload yet justifies their resource and operating costs. Messaging/storage contracts will accompany real workflows. |
| 2026-10-15 | Local-only Compose exposure and private bootstrap configuration | No hardcoded credentials or public administrator signup. HTTP local development is not an approved production transport. |
| 2026-10-15 | Apply EF migrations at development startup | Makes the first local boot reproducible. Production migrations require separate review and controlled execution. |
| 2026-10-15 | Report verification separately from implementation | Generated files are not proof of executed database, browser, container or security validation. |
| 2026-10-15 | Persist revocable eight-hour user sessions | A signed cookie alone cannot revoke a captured cookie on logout. Database session checks and current grant reload provide immediate revocation; password change revokes all sessions transactionally. |
| 2026-10-15 | Start with fixed role policies and native accessible forms/styles | Keeps the foundation small and reviewable. Custom roles, finer domain permissions and form/design libraries can be introduced with operational workflows. |
| 2026-10-15 | Patch dependencies before accepting builds | Initial advisories were resolved with patched ASP.NET/EF packages and matching direct EF references; new API audit reports no known vulnerabilities. Legacy Node dependencies were preserved and are not part of the new runtime. |
