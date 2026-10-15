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

| 2026-10-16 | Implement an integrated operational core while preserving the full backlog | Delivers a testable receive-to-ship lifecycle across modules without mislabeling advanced enterprise requirements complete. |
| 2026-10-16 | Use scoped composite foreign keys, serializable stock transactions and xmin | Enforces scope in PostgreSQL and prevents concurrent oversubscription; conflicts return409 for an explicit refresh/retry. |
| 2026-10-16 | Protect ledger rows with a database trigger and tracked-change guard | The operational ledger supports correction inserts, not history editing. |
| 2026-10-16 | Store label content/revision snapshots and generate QR with QRCoder | Keeps reprint history stable after inventory movements; output remains local preview until a real printer adapter is configured. |
| 2026-10-16 | Group reports by UOM and withhold OEE | No mixed-unit totals or inferred industrial timing; trace completeness is explicit for bounded graphs/lists. |
| 2026-10-16 | Persist development Data Protection keys in an API-owned named volume | Reduces session-key loss on container recreation; encryption-at-rest governance remains a production concern. |
| 2026-10-16 | User requests final phase commit message `Next Phase` and Git push after development | Persist the requested message and push authorization. GitHub HTTP403 remains an authentication/authorization blocker; report it accurately and preserve unpushed commits. This does not mark the advanced MES backlog complete. |
