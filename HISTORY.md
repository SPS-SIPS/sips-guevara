# Project History

## Origin

guevara originated from the legacy SPSS backend repository maintained for the Somali Instant Payment System ecosystem.

The legacy codebase was originally developed under the `spss` backend project and later moved into the current `backend.sln` repository, now published as the `SPS-SIPS/sips-guevara` repository.

## Preserved Legacy History

To make the project origin reviewable, the legacy SPSS backend history has been preserved in this repository as sanitized Git ancestry.

Reviewer entry points:

- Branch: `legacy/spss-sanitized`
- Tag: `legacy-spss-sanitized-import`
- Preservation merge: `chore: preserve sanitized legacy SPSS history`

The preservation merge uses Git's `ours` strategy. This intentionally keeps the current guevara source tree unchanged while linking the sanitized legacy commits into the repository graph.

Useful review commands:

```bash
git log --graph --oneline --decorate --all
git log --oneline legacy/spss-sanitized
git diff legacy-spss-sanitized-import..main --stat
```

## Sanitization Rationale

The raw legacy repository was not imported directly. Before preservation, obvious operational and sensitive artifacts were removed from the legacy history, including:

- Environment and application settings files.
- Sample HTTP files containing internal endpoint hints.
- RTGS XML artifacts.
- Legacy operational documentation containing concrete production endpoint examples or sample webhook credentials.

During the preservation work, the current guevara history was also sanitized for the same configuration and sample HTTP files. This keeps the combined reviewer history useful while avoiding unnecessary exposure of operational configuration and sensitive historical material.

Application settings should be supplied through deployment configuration, environment variables, or non-committed local files. Public examples should use placeholders only.

## Ownership

guevara is owned by SOMALI PAYMENT SWITCH (SPS) LTD. The preserved legacy history is included to document project provenance and continuity within the Somali Payment Switch institutional context.
