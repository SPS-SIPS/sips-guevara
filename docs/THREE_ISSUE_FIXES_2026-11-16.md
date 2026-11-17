# Role delegation and supported runtimes — 16 November 2026

This follow-up fixes the three findings in the security re-review. Login remains username and password only. RTGS import, XML generation, transfer, manual settlement processing and physical-token signing are unchanged. Deployment and production credential rotation remain separate operational actions.

## Changes

### Permission delegation

`PermissionDelegation.EnsureWithinAuthority` rejects unknown permissions and permissions the authenticated staff caller does not currently hold. It is applied to role creation, role editing and removal, user-role assignment, and management/deletion of existing user accounts. Existing protected-role/account checks remain.

This closes both the reproduced self-edit escalation and alternate routes: creating an equivalent role, assigning an existing higher-authority role to a new/current user, or resetting/deleting a higher-authority account. Current permissions still come from live session validation rather than stale JWT role claims.

### Supported C# baseline

- All projects target `net10.0`.
- `global.json` requires SDK 10.0.404 or a compatible newer .NET 10 feature band, excluding previews.
- The API requires .NET/ASP.NET Core **10.0.15 or a newer 10.0 patch** at runtime. It does not silently fall back to an older runtime or another major version.
- EF Core and ASP.NET packages are 10.0.15; Npgsql EF provider is 10.0.3; naming conventions are 10.0.1. Logging integration is updated to the matching supported dependency family. The direct System.Text.Json 8.x reference is removed in favor of the patched shared framework.
- The backend image uses .NET 10.0.15 / SDK 10.0.404 on Ubuntu 24.04, with the compatible `libldap2` package and current available OS updates.
- Non-root UID 1654 and port 8080 are retained from the preceding hardening batch.
- Refresh-token credentials explicitly retain random UUIDv4 generation, independent of new provider defaults. Login's original four-field token response is retained; no factors or enrollment are added.

### Supported Go images

The `balance-monitoring` and `monitoring` final stages use Alpine 3.24 and install available package updates. Go source behavior, ports, non-root users, TLS rules, tokens and CA trust remain as previously implemented. Docker contexts exclude local credentials and build artifacts.

CI builds/scans the three images with a commit-pinned Trivy action. The gate rejects an end-of-support OS, high/critical findings, and medium findings with an available fix. JSON results retain all severity levels; lower-severity entries without a vendor fix are not hidden.

## Breaking changes

| Area | Impact |
| --- | --- |
| Delegated administration | Possessing `CR`, `UR`, `RR`, `CUA`, `UUA` or `DUA` alone no longer authorizes changes involving permissions outside the caller's own authority. These requests return 403. Use an appropriately privileged operator or narrow the role/user being managed. |
| Development/build | Install the pinned or compatible .NET 10 SDK. .NET 8/9 cannot build or consume the upgraded application assemblies. CI and Docker builds use the new SDK baseline. |
| Hosting | Framework-dependent deployments require both .NET and ASP.NET Core 10.0.15+ in the 10.0 family, or the rebuilt image. Existing .NET 8 hosts cannot run this build. |
| Container customization | The C# base distribution changes from Debian to Ubuntu 24.04; native package names/custom image extensions may need adjustment. Existing writable mounts must still be accessible to the non-root UID. Go base packages move to Alpine 3.24. |
| Database/provider integrations | EF/Npgsql major versions change. All application projects must be rebuilt together. The model/migration tests find **no additional schema migration required by this follow-up**; the prior `AddPersistentLoginThrottling` migration is still required if not applied. Custom Npgsql telemetry integrations may need updated metric/span names. Newly provider-generated public GUID identifiers may use UUIDv7; treat IDs as opaque. Refresh credentials remain UUIDv4. |
| Security CI | New image checks may fail when upstream advisories or lifecycle status change. Rebuild/update the relevant base/packages rather than bypass the checks. |

API login, refresh, role/user request schemas and financial XML contracts are not changed by this follow-up. Previously documented LDAP TLS, bank/staff access boundaries and diagnostics changes still apply.

## Verification and limits

The verification report and scanner JSON are under the workspace `security-review` directory. Checks include the original escalation converted into a rejection test, alternate role/user escalation routes, a permitted delegation case, PostgreSQL migration and password-login/refresh integration tests, OpenAPI generation, real image builds and network-disabled runtime inspection.

Only disposable test databases and local images were used. No production credentials, live directory or replacement portal were accessed. Credential rotation and GitHub cache-purge completion are separate prior operational actions. The excluded RTGS findings remain excluded.
