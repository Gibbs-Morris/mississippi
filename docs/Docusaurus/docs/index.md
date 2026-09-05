---
title: Documentation
sidebar_label: Overview
sidebar_position: 1
description: Choose a verified Mississippi path to start, learn, complete a task, understand the model, or look up a contract.
slug: /
---

# Mississippi Documentation

## Overview

These technical docs describe Mississippi's current APIs, architecture,
runtime behavior, constraints, and examples. Mississippi is pre-1.0 and is not
recommended for production use.

The navigation is organized around reader intent. You do not need to understand
Mississippi's internal package names before finding a useful starting point.

## Start Here

| Goal | Recommended Path |
| --- | --- |
| Configure a complete Mississippi client | [Mississippi Client Setup](getting-started/mississippi-client.md) |
| Configure Reservoir without the full client stack | [Reservoir-Only Client Setup](getting-started/reservoir-only-client.md) |
| Learn by building a working domain | [Spring Tutorials](tutorials/spring/index.md) |
| Complete a focused integration or verification task | [How-To Guides](how-to/compose-inlet-client.md) |
| Understand the complete application model | [Architectural Model](concepts/architectural-model.md) |
| Understand writes, reads, and workflow coordination | [Concepts](concepts/index.md) |
| Find the package that owns a concern | [Package and Subsystem Map](reference/package-map.md) |
| Look up an exact term or registration contract | [Reference](reference/glossary.md) |
| Contribute to the documentation | [Documentation Guide](contributing/documentation-guide.md) |

## Recommended Evaluation Path

Use this sequence to understand the main runtime model.

1. [Architectural Model](concepts/architectural-model.md) explains the
   component boundaries and end-to-end flow.
2. [Write Model](concepts/write-model.md) covers commands, aggregate-owned
   decisions, events, reducers, and effects.
3. [Read Models and Client Sync](concepts/read-models-and-client-sync.md)
   covers projections, version-led delivery, and client state.
4. [Sagas and Orchestration](concepts/sagas-and-orchestration.md) covers
   multi-step progress, failure, compensation, and recovery boundaries.
5. [Design Goals and Trade-Offs](concepts/design-goals-and-trade-offs.md)
   explains the conventions, generated surfaces, costs, and poor-fit cases.

## How The Documentation Is Organized

- **Getting Started** reaches a first working client setup through the smallest
  verified path.
- **Tutorials** teach Mississippi by building against a composed sample.
- **How-To Guides** solve focused integration and verification tasks.
- **Concepts** explain the architecture, behavior, guarantees, and trade-offs.
- **Reference** records package ownership, terminology, and exact registration
  surfaces.

Subsystem names such as Brooks, Tributary, and Inlet appear in the
[Package and Subsystem Map](reference/package-map.md). They are reference
vocabulary, not prerequisites for navigating the documentation.

## Current Boundaries

Keep these constraints in view while evaluating the documentation:

- Mississippi is early alpha and its public API is not stable.
- Cosmos DB is the current concrete event and snapshot provider.
- Projection and client delivery are eventually consistent.
- Sagas coordinate and compensate; they do not create distributed
  transactions.
- External effects still require application-specific retry and idempotency.
- Generated MCP tools reuse domain operations but do not automatically inherit
  generated HTTP endpoint authorization.

Use the relevant concepts and reference pages for exact behavior. Operational
or troubleshooting sections will appear only when repository evidence supports
specific, actionable guidance.

## Learn More

- [Getting Started](getting-started/mississippi-client.md)
- [Spring Tutorials](tutorials/spring/index.md)
- [How-To Guides](how-to/compose-inlet-client.md)
- [Concepts](concepts/index.md)
- [Package and Subsystem Map](reference/package-map.md)
- [Glossary](reference/glossary.md)
- [Architecture Decision Records](adr/index.md)
- [Contributing](contributing/documentation-guide.md)
