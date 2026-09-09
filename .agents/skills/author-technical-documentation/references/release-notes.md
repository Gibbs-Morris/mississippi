# Release notes

## Rules

- This reference **MUST** be applied only when the page is classified as `release-notes`. Why: Release notes are concise change summaries, not broad docs pages.
- Release notes **MUST** include exact version and release date, a concise summary, breaking changes, features, fixes, deprecations, security notes when relevant, upgrade guidance, and links. Why: Readers need a standard release summary shape.
- Release notes **MUST** lead with user impact and **MUST** include exact identifiers when relevant, such as version numbers, issue numbers, PR numbers, config keys, removed APIs, or changed defaults. Why: Precise identifiers make release notes actionable.
- Breaking changes **MUST** be called out even when a workaround exists. Why: Breakage belongs in the breaking-changes section, not buried in prose.
- Release notes **SHOULD** link to migration, how-to, or reference pages for detail instead of duplicating them. Why: Release notes should stay concise.
- Release notes **MUST NOT** restate commit messages verbatim or use marketing language. Why: They are engineering change summaries.

## Default structure

1. exact version and release date
2. one-paragraph summary
3. `## Breaking changes`
4. `## Features`
5. `## Fixes`
6. `## Deprecations`
7. `## Security` when relevant
8. `## Upgrade guidance`
9. `## Links`
