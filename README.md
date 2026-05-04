# SPS System Backend

## Development

Run the API locally in Development mode:

```sh
make dev
```

The API uses the `http` launch profile from `Apps/API/Properties/launchSettings.json` and listens at `http://localhost:5007` by default.

## OpenAPI

Generate the OpenAPI specification:

```sh
make openapi
```

The generated file is `openapi.yaml` and is exported from the `v1` Swagger document. The export runs with `OpenApiExport=true` so database initialization and background workers are skipped.

## Features

This list is generated from connected services registered in `Core/Infrastructure/DI.cs`. Each registered service is treated as an application feature.

- **Storage Broker Initializer** (`IStorageBrokerInitializer` -> `StorageBrokerInitializer`, scoped service)
- **Storage Broker** (`IStorageBroker` -> `IStorageBroker`, scoped service)
- **Identity** (`IIdentityService` -> `IdentityService`, scoped service)
- **Token** (`ITokenService` -> `TokenService`, scoped service)
- **Roles Cache** (`IRolesCache` -> `RolesCache`, singleton service)
- **Password Hash** (`IPasswordHash` -> `PasswordHash`, singleton service)
- **Password Manager** (`IPasswordManager` -> `PasswordManager`, singleton service)
- **Email Sender** (`IEmailSender` -> `EmailSender`, singleton service)
- **Template Renderer** (`ITemplateRenderer` -> `TemplateRendererService`, singleton service)
- **NSI Email Notification** (`INSIEmailNotificationService` -> `NSIEmailNotificationService`, scoped service)
- **NSI Generator** (`INSIGeneratorService` -> `NSIGeneratorService`, scoped service)
- **LDAP Certificate Fetcher** (`ICertificateRetriever` -> `LDAPCertificateFetcher`, scoped service)
- **Certificate Validator** (`CertificateValidator` -> `CertificateValidator`, scoped service)
- **Certificate Expiration Notifier** (`CertificateExpirationNotifier` -> `CertificateExpirationNotifier`, scoped service)
- **File** (`IFileStorage` -> `FileService`, scoped service)
- **Bic Statistics** (`IBicStatisticsService` -> `BicStatisticsService`, scoped service)
- **Statistics Report** (`IStatisticsReportService` -> `StatisticsReportService`, singleton service)
- **Balance Threshold** (`IBalanceThresholdService` -> `BalanceThresholdService`, scoped service)
- **Backup** (`BackupJob` -> `BackupJob`, scheduled worker)
- **NSI Monitoring** (`NSIMonitoring` -> `NSIMonitoring`, scheduled worker)
- **Certificate Fetching** (`CertificateFetchingService` -> `CertificateFetchingService`, hosted worker)
- **Certificate Validation** (`CertificateValidationService` -> `CertificateValidationService`, hosted worker)
- **Participant Status Updater** (`ParticipantStatusUpdaterWorker` -> `ParticipantStatusUpdaterWorker`, hosted worker)
- **Monit History Cleanup** (`MonitHistoryCleanupWorker` -> `MonitHistoryCleanupWorker`, hosted worker)
- **Failed Transaction Compliance Report** (`FailedTransactionComplianceReportWorker` -> `FailedTransactionComplianceReportWorker`, hosted worker)
- **Transaction Retention Cleanup** (`TransactionRetentionCleanupWorker` -> `TransactionRetentionCleanupWorker`, hosted worker)
- **Compliance Management Digest** (`ComplianceManagementDigestWorker` -> `ComplianceManagementDigestWorker`, hosted worker)
