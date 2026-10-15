# Development rules

Read `docs/IMPLEMENTATION_STATUS.md`, `docs/ARCHITECTURE.md`, and `docs/DECISION_LOG.md` before changing this project. Preserve the legacy root Node application unless explicitly tasked with its migration.

Build original Green Ink workflows. Do not access, copy, or depend on proprietary third-party MES assets. Work only within this project. Keep credentials out of tracked files and use demo data only when explicitly identified.

Use a modular monolith and PostgreSQL. Enforce tenant and plant scope on every resource operation, including lists, mutations, and audit queries. UI visibility is not authorization. Add meaningful isolation, permission, validation, and persistence tests with features. Inventory posting must eventually use atomic transactions, concurrency control, idempotency, immutable movements, and auditable reversals.

Implement demonstrable vertical slices. Never label placeholders or unexecuted checks complete. Update status, decisions, and backlog after each change; record exact validation commands and blockers. Do not introduce Redis, RabbitMQ, or other infrastructure without an evidenced requirement. Do not automatically publish, merge, or deploy externally.

Use isolated, nonconflicting file ownership when delegating work. Do not run competing agents against the same files.
