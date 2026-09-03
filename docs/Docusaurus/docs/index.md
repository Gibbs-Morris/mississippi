---
id: home
title: Documentation
sidebar_label: Home
sidebar_position: 1
description: Choose a verified path through Mississippi concepts, subsystems, samples, reference material, and contribution guidance.
slug: /
---

# Mississippi Documentation

## Overview

These technical docs describe Mississippi's current APIs, architecture,
runtime behavior, constraints, and examples. Mississippi is pre-1.0 and is not
recommended for production use.

Start with the architectural model when evaluating the complete framework. Use
the subsystem sections when adopting one package area, or follow the Spring
sample when you want to inspect a composed application.

## Start Here

| Goal | Recommended Path |
| --- | --- |
| Understand the complete application model | [Architectural Model](./concepts/architectural-model.md) |
| Understand writes, reads, and workflow coordination | [Concepts](./concepts/index.md) |
| Examine a composed application | [Spring Sample](./samples/spring-sample/index.md) |
| Evaluate one independently adoptable subsystem | Use the subsystem table below |
| Look up exact terms or package contracts | [Reference](./reference/glossary.md) |
| Contribute to the documentation | [Documentation Guide](./contributing/documentation-guide.md) |

## Architecture Reading Path

Use this sequence to understand the main runtime model.

1. [Architectural Model](./concepts/architectural-model.md) explains the
   component boundaries and end-to-end flow.
2. [Write Model](./concepts/write-model.md) covers commands, aggregate-owned
   decisions, events, reducers, and effects.
3. [Read Models and Client Sync](./concepts/read-models-and-client-sync.md)
   covers projections, version-led delivery, and client state.
4. [Sagas and Orchestration](./concepts/sagas-and-orchestration.md) covers
   multi-step progress, failure, compensation, and recovery boundaries.
5. [Design Goals and Trade-Offs](./concepts/design-goals-and-trade-offs.md)
   explains the conventions, generated surfaces, costs, and poor-fit cases.

## Subsystems

Each subsystem has focused concepts, getting-started, how-to, reference, and
troubleshooting material where that content currently exists.

| Area | Responsibility |
| --- | --- |
| [Domain Behavior](./domain-modeling/index.md) | Aggregates, command handling, effects, sagas, and projections |
| [Reducers and Snapshots](./tributary/index.md) | State reconstruction, reducer execution, snapshots, and delta replay |
| [Event Streams](./brooks/index.md) | Ordered event storage, cursors, metadata, and serialization |
| [API and Client Sync](./inlet/index.md) | Generated gateway, runtime, client, subscription, and MCP surfaces |
| [SignalR Backplane](./aqueduct/index.md) | Orleans-backed connection, group, user, and server routing |
| [Client State](./reservoir/index.md) | Redux-style actions, reducers, effects, selectors, and test support |
| [Blazor UI](./refraction/index.md) | Presentational components, design tokens, and state-connected scenes |

Aqueduct, Brooks, Reservoir, and Refraction expose package entry points that can
be evaluated independently. The complete Mississippi model composes these
areas with Domain Modeling, Tributary, and Inlet.

## Samples

[Spring](./samples/spring-sample/index.md) is the current end-to-end sample. It
demonstrates aggregates, projections, saga coordination, generated HTTP and MCP
surfaces, SignalR delivery, Reservoir client state, and Aspire-hosted local
infrastructure.

The sample has more prerequisites than a single-package example. Review its
setup guidance before treating it as a quick-start path.

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

Use the relevant concepts, operations, and reference pages for exact behavior.

## Learn More

- [Concepts](./concepts/index.md)
- [Spring Sample](./samples/spring-sample/index.md)
- [Reference](./reference/glossary.md)
- [Architecture Decision Records](./adr/index.md)
- [Contributing](./contributing/documentation-guide.md)
- [Archived Documentation](./archived/index.md)
