---
title: Tutorial Pages
description: Teach Mississippi through one linear end-to-end path with complete verified steps and checkpoints.
sidebar_label: Tutorials
sidebar_position: 4
---

# Tutorial Pages

Use a tutorial when the reader needs guided learning and you are responsible for keeping them on a safe, linear path.

Tutorials are not reference pages and they are not concept essays. They should teach by building or configuring something real from start to finish.

## Use This Page Type When

- the reader is learning by doing
- the page should provide one complete end-to-end path
- the reader should not need to infer hidden code or missing setup

## Required Structure

1. one-sentence statement of what the reader will build
2. `## Before you begin`
3. `## Step 1`, `## Step 2`, and so on in strict sequence
4. verification checkpoints after major steps
5. `## Recap`
6. `## Next steps`

## Content Rules

- Use one linear path from start to finish.
- Introduce concepts only when the next step needs them.
- Use complete, verified code.
- Prefer one real project over many fragments.
- Move deeper explanation to concept pages and link out.

## Minimal Frontmatter Example

```yaml
---
title: Build a Money Transfer Saga
description: Learn Mississippi saga orchestration by building a verified end-to-end money transfer workflow.
sidebar_position: 20
sidebar_label: Money Transfer Saga
---
```

## Page Skeleton Example

```md
# Build a Money Transfer Saga

Build a saga that coordinates a transfer workflow and verify each major step as you go.

## Before you begin

## Step 1

## Checkpoint

## Step 2

## Checkpoint

## Recap

## Next steps
```

## Related Content

- [Documentation Guide](./documentation-guide.md)
- [Getting-Started Pages](./documentation-getting-started.md)
- [Concept Pages](./documentation-concepts.md)
