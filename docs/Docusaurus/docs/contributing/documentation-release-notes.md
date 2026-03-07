---
title: Release Notes
description: Summarize Mississippi release changes concisely with exact version identifiers, user impact, and upgrade links.
sidebar_label: Release Notes
sidebar_position: 10
---

# Release Notes

Use release notes to tell readers what changed, why it matters, and what action is required.

Release notes should be concise and impact-oriented. They should not duplicate migration guides or restate commit messages verbatim.

## Use This Page Type When

- the page documents one release or release train
- the reader needs a concise summary of impact and required action
- the details belong in linked migration, how-to, or reference pages

## Required Structure

1. exact version and release date
2. one-paragraph summary
3. `## Breaking changes`
4. `## Features`
5. `## Fixes`
6. `## Deprecations`
7. `## Security` when relevant
8. `## Upgrade guidance`
9. `## Links`

## Content Rules

- Be brief and exact.
- Lead with user impact.
- Include exact identifiers when relevant: version numbers, issue numbers, PR numbers, config keys, removed APIs, or changed defaults.
- If a change can break an existing application, deployment, build, stored state, or protocol interaction, name it under `## Breaking changes` even if a workaround exists.

## Minimal Frontmatter Example

```yaml
---
title: Mississippi 0.9 Release Notes
description: Summarize the user-visible changes, breaking changes, and upgrade guidance for Mississippi 0.9.
sidebar_position: 90
sidebar_label: Mississippi 0.9
---
```

## Page Skeleton Example

```md
# Mississippi 0.9 Release Notes

Mississippi 0.9 focuses on verified documentation governance, runtime fixes, and clearer upgrade guidance.

## Breaking changes

## Features

## Fixes

## Deprecations

## Upgrade guidance

## Links
```

## Related Content

- [Documentation Guide](./documentation-guide.md)
- [Migration Pages](./documentation-migration.md)
- [Reference Pages](./documentation-reference.md)
