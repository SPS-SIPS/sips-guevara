# Security changes and compatibility — 16 November 2026

Follow-up: `THREE_ISSUE_FIXES_2026-11-16.md` documents the additional permission ceiling, supported .NET/Go image upgrades and their breaking changes.

Login is **username and password only**, as explicitly requested. There is no second-factor field, enrollment flow, resend action or second-factor session requirement. The unshipped second-factor implementation and migration have been removed. Other security changes remain in place. These source changes do not deploy services or rotate production credentials.

## Login contract

```http
POST /api/v1/Auth/login
Content-Type: application/json
```

```json
{ "username": "your-username", "password": "your-password" }
```

The existing response envelope returns the original four token fields in `data`: `accessToken`, `refreshToken`, `expiresIn`, `refreshExpiresIn`. A normal successful login grants access immediately according to the user's current permissions. Refresh works without additional enrollment or factors. Explicitly bootstrapped administrators still must change their initial password before normal access; this existing bootstrap control is unchanged.

## Changes and breaking behavior

| Area | New behavior | Required integration/configuration change |
| --- | --- | --- |
| Password login | Accepts only username and password. The obsolete resend endpoint and second-factor implementation, fields, helpers and settings are removed. | Clients should send the two fields above and remove any code-entry/enrollment/resend UI. There are no enrollment flags in the login response and no new encryption-key configuration requirement. |
| Persistent throttling | At most five login attempts per account per 15-minute window, shared across instances and username/phone aliases. | Handle throttling and wait before retrying. This counts all attempts, including successful attempts; existing process/IP limits also remain. |
| Database | Adds persistent login counters and removes retired token records/code-value storage. Refresh-token records are preserved. | Apply `20261116072112_AddPersistentLoginThrottling`. `docs/AddPersistentLoginThrottling.sql` is an incremental idempotent handoff script assuming the prior migration chain is applied. Startup already invokes EF migrations in normal operation. Retired token records are deleted permanently; valid refresh sessions are retained. |
| Bank isolation | Bank-linked users cannot invoke supervisor-wide operations even if assigned those permissions. Their settlement list and reconciliation derive bank scope from the current identity. | Use participant-specific settlement endpoints, `/api/v1/Participants/balance-status` and self-account actions. Cross-bank transactions, dashboards, full settlement ZIPs, threshold administration and other staff APIs return 403 for bank identities. |
| Authorization defaults | Controllers require authentication unless explicitly public. Mediator requests require explicit authorization policies. | New endpoints/commands must declare their policy. Login and refresh remain public. API-key ingestion retains its headers and `CMH` permission. |
| LDAP | Credentials bind only after LDAPS or StartTLS succeeds; no plaintext fallback or certificate-validation bypass. | `LDAP:Transport` is `Ldaps` (default port 636) or `StartTls` (default port 389); `LDAP:Port` may override the port. Use a trusted directory certificate and matching hostname. Rotate exposed directory credentials separately. |
| C# container | Runs as the non-root .NET app UID and defaults to HTTP port 8080. | Update mappings/proxy targets; writable mounts need suitable ownership/permissions. Existing HTTPS/trusted-proxy requirements remain. |
| Dependencies | Official Bouncy Castle 2.7.0 and stable Mediator 3.0.2 replace the legacy/preview packages. The obsolete GoogleAuthenticator dependency is removed. | Rebuild the projects together; verify representative certificates in your acceptance process. Existing AutoMapper licensing requirements remain. |
| Automation | Read-only GitHub security workflows and weekly Dependabot configuration. | CI runs security tests with disposable PostgreSQL databases and fails on NuGet advisories. Go workflows run race tests and pinned vulnerability scans. |

A bank identity is a human account linked to a participant. Staff identities have no participant link and still require the requested permission. A deleted participant or blank BIC does not confer staff access: session validation rejects that identity. API clients are separately configured service identities and should retain only the needed ingestion permissions.

## Verification and scope

Run `dotnet test tests/Security.Tests/Security.Tests.csproj`. Set `SPSS_SECURITY_TEST_CONNECTION` to a disposable PostgreSQL test connection with permission to create/drop databases to enable relational tests. Each relational test creates/removes its own random database and never migrates the configured database; CI supplies this setting.

Tests cover username/password login and immediate authenticated access, refresh and session revocation, distributed account throttling, schema changes/retired-token cleanup, authorization defaults, tenant boundaries, LDAP plaintext rejection and certificate interoperability. Live LDAP, the replacement portal and deployed container configuration were not used for verification.

At the owner's explicit request, RTGS imports, XML generation, transfer integrity, manual settlement approval and physical-token signing are unchanged. Those remaining settlement findings are not closed by this change. MFA was explicitly declined by the owner; the corresponding review recommendation is not implemented. Credential rotation remains a separate operational action.
