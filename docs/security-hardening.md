# Security hardening

Financial ingestion now requires the `CMH` service scope. Manual transaction reports require `UFT`
and a reporting window of at most seven days. All production API requests require HTTPS; configure
`Security:TrustedProxies` for a trusted reverse proxy and `CorsPolicies:Origins` for browser clients.

JWT configuration requires a signing secret of at least 32 UTF-8 bytes, issuer/audience, and an
access-token lifetime of at most 30 minutes. Users must log in again after this update. Account
state, session revocation, current permissions and tenant membership are checked on each request.
Logout accepts a GUID session ID. OTP resend is disabled. Authentication and reports are rate limited.

Administrator creation requires explicit `Bootstrap:Enabled` and `Bootstrap:AdminPassword` settings.
The password must contain at least 16 characters and must be changed through the password-change
endpoint before using other authenticated endpoints. Protected accounts/roles cannot be modified
through delegated administration.

Passwords use PBKDF2-SHA256 with 600,000 iterations; successful login upgrades older hashes.
The design-time database factory reads `SPSS_DESIGN_TIME_CONNECTION_STRING` when no argument is
provided. Connection strings, API credentials and signing secrets must be supplied outside Git.

AutoMapper was updated to 15.1.4, which has upstream licensing requirements. The application accepts
`AutoMapper:LicenseKey`. Other affected NuGet packages are updated and pinned.

Run `dotnet test backend.sln` for the security regression tests. The suite covers authorization,
service scopes, token validation/revocation, protected accounts, deleted users, password upgrades,
bootstrap and plaintext transport rejection. The solution passed 28 tests and its NuGet audit
reported no known vulnerable packages at the time of this change.
