# Foundation API

Same-origin JSON API. Authentication is the HTTP-only `greenink.session` cookie; never store bearer tokens in browser storage. Every mutation, including login, requires `X-GreenInk-Request: 1`. Cross-origin CORS is disabled. OpenAPI is available at `/openapi/v1.json` in Development. Unauthorized requests return 401; scope/permission denial returns 403 or scoped 404; invalid input returns 400; duplicate codes/emails return 409; login throttling returns 429.

| Route | Access and behavior |
| --- | --- |
| POST `/api/auth/login` | `{email,password}`; rate limited; returns user profile and session cookie |
| GET `/api/auth/me` | Current profile including role, tenantId and plantIds |
| POST `/api/auth/logout` | Revokes current server session and deletes cookie |
| POST `/api/auth/password` | `{currentPassword,newPassword}`; atomically changes hash and revokes all sessions; login again |
| GET `/api/roles` | Authenticated; fixed built-in role names |
| GET / POST `/api/tenants` | Scoped list; only PlatformAdmin creates `{name,code}` |
| GET / POST `/api/plants` | Scoped list; PlatformAdmin or TenantAdmin creates `{tenantId,name,code}` within authorized tenant |
| GET / POST `/api/users` | PlatformAdmin or TenantAdmin; scoped list or create `{email,displayName,password,role,tenantId,plantIds}` |
| PUT `/api/users/{id}/access` | Scoped administrators update `{role,plantIds}`; cannot edit/promote PlatformAdmin through this endpoint |
| GET `/api/dashboard?tenantId=...&plantId=...` | Validates both optional scopes; counts authorized organizations/plants and visible users; rejects mismatched tenant/plant |
| GET `/api/audit` | Scoped administrators; most recent 100 privileged events |
| GET `/health/live`, `/health/ready` | Process and database readiness; no credentials returned |

Codes contain 1–32 letters, digits, hyphens or underscores and are normalized uppercase. Names contain 1–160 characters. Passwords contain 14–128 characters with uppercase, lowercase and a digit. Email uniqueness is normalized lowercase. PlatformAdmin has no tenant or explicit plant assignments; other roles require a tenant, and assigned plants must belong to that tenant.

| Role | Current foundation permissions |
| --- | --- |
| PlatformAdmin | All organizations/plants/users, organization creation, scoped administration and audit |
| TenantAdmin | Own organization, all its plants/users, plant/user creation and access editing, tenant audit |
| PlantManager / Operator / Viewer | Own organization identity, explicitly assigned plants, own user profile and scoped dashboard |

Production workflow permissions for the last three roles arrive with the corresponding MES modules. Custom role definitions, tenant/plant editing and deletion, account suspension/reset and enterprise OIDC are backlog items. Current user lists are not yet paginated. Dashboard user counts for non-administrators represent their own visible profile, not all tenant members. Audit storage has no editing endpoint; database-admin tamper protection and detailed before/after grant history require hardening.
