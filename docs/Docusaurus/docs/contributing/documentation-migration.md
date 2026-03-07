---
title: Migration Pages
description: Guide Mississippi users from one exact version range to another with explicit compatibility, validation, and rollback details.
sidebar_label: Migration
sidebar_position: 9
---

# Migration Pages

Use a migration page when the reader needs to move from one exact version range to another safely.

Migration guidance must be precise about compatibility, rollout order, validation, and rollback. It is not a release-notes summary.

## Use This Page Type When

- the page describes upgrading between specific versions
- the page needs before-and-after code or config changes
- the reader must understand data, storage, wire, or serialization compatibility

## Required Structure

1. exact scope statement
2. `## Who should read this`
3. `## Compatibility summary`
4. `## Breaking changes`
5. `## Required preparation`
6. `## Upgrade sequence`
7. `## Code and configuration changes`
8. `## Data, state, and serialization implications`
9. `## Validation`
10. `## Rollback`
11. `## Related release notes and reference`

## Content Rules

- Put the exact version scope in the title.
- State mixed-version support, wire compatibility, storage compatibility, config renames, removed APIs, deprecated APIs, and default changes when relevant.
- Show before-and-after code only when it is verified.
- State exactly how the user proves the migration succeeded.

## Minimal Frontmatter Example

```yaml
---
title: Upgrade from 0.8 to 0.9
description: Migrate a Mississippi installation from version 0.8 to 0.9 with explicit compatibility, validation, and rollback guidance.
sidebar_position: 80
sidebar_label: Upgrade 0.8 to 0.9
---
```

## Page Skeleton Example

```md
# Upgrade from 0.8 to 0.9

This guide explains how to move from Mississippi 0.8 to 0.9 safely.

## Who should read this

## Compatibility summary

## Breaking changes

## Required preparation

## Upgrade sequence

## Code and configuration changes

## Data, state, and serialization implications

## Validation

## Rollback

## Related release notes and reference
```

## Related Content

- [Documentation Guide](./documentation-guide.md)
- [Release Notes](./documentation-release-notes.md)
- [Reference Pages](./documentation-reference.md)
