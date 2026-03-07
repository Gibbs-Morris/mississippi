---
title: Getting-Started Pages
description: Get a new Mississippi user to a first successful outcome with the smallest verified path and the least theory.
sidebar_label: Getting Started
sidebar_position: 3
---

# Getting-Started Pages

Use a getting-started page when the reader is new to Mississippi and needs one verified path to first success.

Assume the reader is already a professional developer. The goal is not to teach every concept; the goal is to get them safely to a working result and then point them to deeper material.

## Use This Page Type When

- the reader is new to Mississippi
- the page should optimize for a shortest verified happy path
- the page should avoid branches, options, and production complexity

## Required Structure

1. one-sentence outcome statement
2. `## What you will build` or `## What you will achieve`
3. `## Prerequisites`
4. `## Install`
5. `## Create the project` or `## Run the sample`
6. `## Verify it works`
7. `## What happened`
8. `## Next steps`

## Content Rules

- Prefer a known-good sample when one exists.
- Prefer local defaults over production infrastructure.
- Use one happy path.
- Explain only the minimum theory needed to keep the reader safe.
- Link to concept pages instead of embedding deep explanation.

## Verification Rules

- Every command must be executable as written.
- Every expected result must come from an actual run.
- If setup differs by OS, use tabs.
- If external infrastructure is required, provide the lowest-friction verified option.

## Minimal Frontmatter Example

```yaml
---
title: Build Your First Reservoir Feature
description: Create and verify a minimal Reservoir feature so you can see Mississippi state management working end to end.
sidebar_position: 10
sidebar_label: First Reservoir Feature
---
```

## Page Skeleton Example

```md
# Build Your First Reservoir Feature

Create a minimal Reservoir feature and verify that actions update state in a running app.

## What you will build

## Prerequisites

## Install

## Create the project

## Verify it works

## What happened

## Next steps
```

## Related Content

- [Documentation Guide](./documentation-guide.md)
- [Tutorial Pages](./documentation-tutorials.md)
- [Concept Pages](./documentation-concepts.md)
