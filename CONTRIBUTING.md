# Contributing

Guevara is owned by SOMALI PAYMENT SWITCH (SPS) LTD and is maintained as part
of the Somali Instant Payment System technology ecosystem.

By contributing, contributors agree that their contributions are submitted
under the Apache License 2.0 unless a separate written agreement with SPS says
otherwise.

## Contribution Requirements

- Keep transaction processing, participant liquidity, settlement, certificate,
  monitoring, authorization, and reporting behavior explicit and tested.
- Do not include BPC or SmartVista proprietary source, confidential
  configuration, production data, credentials, certificates, or private keys.
- Use environment variables or deployment-specific secret stores for secrets.
- Include focused tests for changes to transaction ingestion, settlement,
  authorization, certificate handling, persistence, scheduled workers, or
  external-system integrations.
- Update documentation when behavior, configuration, or operational procedures
  change.

## High-Risk Areas

Changes in these areas require senior review:

- NSI, net-settlement, and RTGS file generation.
- Transaction ingestion, outcome classification, and analytics.
- Participant liquidity thresholds and alerting.
- Authentication, authorization, roles, and permissions.
- Certificates and cryptographic trust material.
- Database migrations and persistence constraints.
- Background workers and external Oracle or monitoring integrations.
