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

| 2026-10-16 | Use the user's supplied inbound manual/source authorization for behavioral reference; implement original Green Ink workflows/UI | Latest user request authorizes the supplied references. The200-page manual was text-reviewed and requirements mapped; sourceZIP exceeds32MiB tool limit and remains unreviewed. No third-party source/screens/assets copied. |
| 2026-10-16 | First inbound expansion connects gate coordination, checklist snapshots and pack receiving to the existing ledger | Retains single-material PO compatibility and defaults new policies off; keeps all full-parity gaps explicit rather than exposing nonfunctional modes. |
| 2026-10-16 | Snapshot check obligations on truck registration and lock answers after their gate | Later master edits cannot remove or alter historical visit requirements. Docks permit queueing but serializable start enforces one active unloading truck. |


| 2026-10-17 | Build an order-focused original Material Preparation interface from the supplied staging manual's behavioral requirements | Replaces scattered default form cards with order readiness, focused scan/weigh dialogs, kits, stock/request workspaces and Kanban cards. Source ZIP exceeds32MiB and was not reviewed; solution is project metadata only. Full parity remains explicitly mapped. |
| 2026-10-17 | Separate staging movement from consumption and distinguish physical quantity from nominal requirement credit | Split transfers conserve inventory; kit stock remains reserved until issue. Nullable credits preserve old records; corrected/tolerance-approved allocations fulfill declared nominal BOM credit while recording real physical consumption and manager rule/factor snapshots. Plant UAT must confirm intended tolerance/accounting semantics. |
| 2026-10-17 | Report discrepancies before manager-reviewed immutable adjustments; preserve cancellation/delivery and stock conflicts atomically | Prevents silent stock overwrite and destructive stock-take cleanup. No fake ERP/device acknowledgment or force-completion bypass is introduced. |

| 2026-10-17 | Use supplied manufacturing DAL behavior as reference while recording the unread oversized PDF as a blocker | User authorizes the references; method indexes and selected bodies guide original code/UI. Full manual parity cannot be asserted without a readable PDF/service-layer/UAT comparison. |
| 2026-10-17 | Snapshot published SOP phases and quality tests; keep lab review separate from stock release | Configuration revisions must not rewrite active execution or sample obligations. Existing direct completion/release routes enforce the same gates. |
| 2026-10-17 | Persist out-of-range evidence and manager-reviewed deviations instead of silently overwriting readings | Corrected values retain history; retests preserve failed samples; closing a case does not automatically pass or release stock. |
| 2026-10-17 | Keep whole-order output reconciliation and add conservative same-batch packing | Avoid inventing partial/rework consumption semantics. Packing conserves existing stock with paired movements; physical packaging consumption and partial production remain explicit gaps. |
