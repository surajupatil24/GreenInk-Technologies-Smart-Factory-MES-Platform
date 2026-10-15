# Git synchronization — 2026-10-16 operations update

The operational lifecycle is committed on local branch `mes-operations`. The attempted fast-forward push to GitHub main was rejected with HTTP403: the connected cloud Git credentials do not have write access. GitHub main therefore remains at `2b2ca82`; do not assume the latest operational work is remotely available yet.

A verified incremental Git bundle is prepared as `GreenInk-Operations.bundle` outside the repository for download. It includes all new commits and requires the existing `2b2ca82` baseline (which already contains the foundation and branding). No private `.env`, database dump, validation credential, dependency directory or build artifact is added by this update.

On the Windows PC, download the bundle to Downloads and run each command separately. If a command fails, stop and inspect the error. Git will refuse to overwrite conflicting uncommitted changes; no reset, force push or automatic stash is required.

```powershell
cd "D:\GitHub\GreenInk-Technologies-Smart-Factory-MES-Platform"
git fetch "$HOME\Downloads\GreenInk-Operations.bundle" HEAD
git merge --ff-only FETCH_HEAD
git push origin HEAD:main
```

Back up the existing local database following BACKUP_RESTORE.md, then rebuild **both API and web**:

```powershell
docker compose --env-file infrastructure/.env -f infrastructure/compose.yaml up -d --build
```

Keep `.env` and the database volume. Never append `--volumes` to a shutdown as part of a routine update. On Development startup the API upgrades the schema automatically; the populated-foundation upgrade regression passed. Open http://localhost:8080 and refresh. Select a plant and follow OPERATIONS_GUIDE.md. Cloud files do not change Windows files until fetched/merged locally. Complete original scope and production deployment remain separate tasks.

## Latest verified baseline and BOM guidance update

The user successfully pushed the operational update and final preference commit; GitHub main was directly verified at `94ec56e`. The older HTTP403/main-baseline description above records the earlier handoff, not the latest confirmed remote state. The saved-BOM guidance fix is prepared on top of `94ec56e` with message `Next Phase`. If cloud push is rejected, download `GreenInk-BOM-Guidance.bundle` to Downloads and import it with the same fetch/fast-forward/push procedure, substituting that filename. It contains only the newer commit chain. Both API and web need rebuilding; there is no additional database migration.
