# Local PostgreSQL backup and recovery

This is a development recovery procedure. A cloud logical dump/restore and schema/count comparison was executed successfully; see IMPLEMENTATION_STATUS.md. Full recovered-application verification and production recovery policy remain pending. Database exports may contain personal and customer information; store them securely outside Git. Verify the target database and backup identity before every operation.

From the repository root, with the Compose services running, create a custom-format backup inside the database container and copy it out. This avoids binary redirection problems in Windows PowerShell:

```powershell
docker compose --env-file infrastructure/.env -f infrastructure/compose.yaml exec -T db sh -c 'pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc -f /tmp/greenink.dump'
docker compose --env-file infrastructure/.env -f infrastructure/compose.yaml cp db:/tmp/greenink.dump ./greenink-local.dump
```

Move the export to approved private backup storage. To validate recovery, create a separate database in your local development database container. The commands below do not overwrite the source database; choose a fresh recovery name if it exists:

```powershell
docker compose --env-file infrastructure/.env -f infrastructure/compose.yaml cp ./greenink-local.dump db:/tmp/greenink-restore.dump
docker compose --env-file infrastructure/.env -f infrastructure/compose.yaml exec -T db sh -c 'createdb -U "$POSTGRES_USER" greenink_restore_check'
docker compose --env-file infrastructure/.env -f infrastructure/compose.yaml exec -T db sh -c 'pg_restore -U "$POSTGRES_USER" -d greenink_restore_check --no-owner --exit-on-error /tmp/greenink-restore.dump'
docker compose --env-file infrastructure/.env -f infrastructure/compose.yaml exec -T db sh -c 'psql -U "$POSTGRES_USER" -d greenink_restore_check -c "\dt"'
```

Check migration history and compare expected tenant, plant, membership and audit counts. In an isolated API process configured for the recovery database, verify login, scope restrictions and representative workflows. Do not point an active application at a recovery database until its purpose and users are understood. Record backup timestamp, PostgreSQL version, file integrity hash, restore duration and validation evidence.

Production needs an explicit recovery point/time objective, encrypted offsite backups, retention, access control, monitoring, periodic restore drills and, when required, physical backup/WAL archiving for point-in-time recovery. Logical dumps alone do not provide continuous recovery. An incident recovery procedure must consider revoking authentication cookies and rotating compromised protection keys.
