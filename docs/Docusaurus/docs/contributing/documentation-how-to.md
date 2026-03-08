---
id: documentation-how-to
title: How-To Guides
description: Help a competent Mississippi user complete one real task with exact steps, verification, and task-specific safety notes.
sidebar_label: How-To
sidebar_position: 5
---

# How-To Guides

## Overview

Use a how-to guide when the reader already understands the basics and wants the fastest reliable route to a result.

The task comes first. Long conceptual explanation does not.

The primary reader question is: "How do I complete this specific task safely and verify the result?"

## Use This Page Type When

- the reader wants to complete a concrete task
- the page should provide exact steps in execution order
- the reader needs specific verification and rollback guidance

## Required Structure

1. direct task statement
2. `## When to use this`
3. `## Before you begin`
4. `## Steps`
5. `## Verify the result`
6. `## Troubleshooting` or `## Common problems` when justified
7. `## Summary`
8. `## Next Steps`

## Content Rules

- Start from the task, not the subsystem.
- Make prerequisites explicit.
- State defaults, side effects, and irreversible actions.
- Link to reference for exact option definitions.
- Link to concept pages for rationale instead of embedding long explanation.

## Safety Rules

- Document rollback or cleanup when the task changes production state.
- State whether the change is safe for live systems.
- State whether restart, redeploy, or cluster rebalance is required.
- State whether the change affects existing activations, persisted state, or message flow.

## Minimal Frontmatter Example

```yaml
---
title: Configure a Cosmos Storage Provider
description: Configure and verify a Cosmos-backed storage provider for Mississippi runtime components.
sidebar_position: 40
sidebar_label: Cosmos Storage
---
```

## Page Skeleton Example

```md
# Configure a Cosmos Storage Provider

Configure a Cosmos-backed storage provider for Mississippi and verify that the runtime reads and writes state successfully.

## When to use this

## Before you begin

## Steps

## Verify the result

## Common problems

## Summary

## Next Steps
```

## Summary

- how-to guides optimize for task completion, not guided learning
- they must make prerequisites, side effects, and verification explicit
- they should link out to concepts and reference instead of embedding long background sections

## Next Steps

- [Documentation Guide](./documentation-guide.md)
- [Reference Pages](./documentation-reference.md)
- [Operations Pages](./documentation-operations.md)
