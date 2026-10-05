---
id: concepts-architectural-model
title: Architectural Model
sidebar_label: Architectural Model
sidebar_position: 2
description: Explain what Mississippi is, why it exists, and how its major subsystems combine into one application model.
---

# Architectural Model

## Overview

Mississippi is an opinionated application model for building event-sourced systems on Orleans.

It treats event sourcing, runtime composition, generated delivery surfaces, and client state as one architectural model instead of four separate concerns. Teams define domain types such as aggregate state, commands, events, reducers, saga steps, and projections. Mississippi then composes the Orleans runtime, generated HTTP and SignalR surfaces, and client state infrastructure around those types.

## The Problem This Solves

CQRS, event sourcing, Orleans, and real-time clients are individually well understood. The real cost appears in the seams between them - the duplicated mappings, mirrored DTOs, and hand-wired plumbing that multiply with every new aggregate or projection.

Teams often end up maintaining the same mappings in several places:

- command records, controllers, DTOs, and client request objects
- event streams, reducers, snapshots, and read endpoints
- SignalR subscription plumbing and client-side refresh logic
- test setup for domain logic versus test setup for infrastructure

Mississippi eliminates that seam work by letting a small set of domain-facing types drive the surrounding runtime and client surface.

## Core Idea

Mississippi starts from a few explicit domain artifacts and composes outward.

- Aggregate state records define write-side state.
- Command handlers validate commands against current state and emit events.
- Brooks stores those events in per-aggregate brooks.
- Tributary reducers rebuild both aggregate state and projection state from events.
- Projection types define read models over the same brook and can opt into generated delivery with `[GenerateProjectionEndpoints]`.
- Inlet generators derive gateway and client scaffolding from those types and attributes.
- Reservoir keeps client state predictable by reducing actions into immutable feature slices.

## How It Works

This diagram shows the verified runtime composition and dependency direction.

```mermaid
flowchart TB
    A[Domain types] --> DM[DomainModeling]
    DM --> TR[Tributary]
    DM --> BR[Brooks]
    TR --> BR
```

This diagram shows how projection definitions become delivery surfaces and client state.

```mermaid
flowchart TB
    P[Projection types and attributes] --> IN[Inlet code generation]
    IN --> HTTP[HTTP endpoints and controllers]
    IN --> SUB[SignalR subscription management]
    SUB --> AQ[Aqueduct SignalR transport]
    HTTP --> RS[Reservoir client state store]
    AQ --> RS
```

In public package terms, the major responsibilities break down like this.

| Area | Verified role |
| --- | --- |
| Domain Modeling | Aggregates, sagas, event effects, projection access, and test harnesses |
| Brooks | Event-stream identity, append, cursor tracking, and storage abstractions |
| Tributary | Event reducers, root reducers, and snapshot-oriented reconstruction |
| Inlet | Source generation and registration for aggregate, projection, and saga surfaces across runtime, gateway, and client layers |
| Reservoir | Redux-style store, action reducers, middleware, and action effects |
| Aqueduct | Orleans-backed SignalR transport and connection infrastructure |
| Refraction | Blazor component library and design-token system for client UIs |

## Guarantees

- Mississippi has a real end-to-end composition model rather than a single server-side helper package. The repository contains dedicated runtime, gateway, and client projects for the same concepts.
- Aggregate, projection, and saga generation is attribute-driven. For example, `[GenerateAggregateEndpoints]`, `[GenerateProjectionEndpoints]`, `[GenerateCommand]`, and `[GenerateSagaEndpoints]` each drive concrete generator output.
- The same model is visible in the Spring sample, where projection types marked with `[GenerateProjectionEndpoints]` feed generated endpoints and real-time client projection updates.

## Non-Guarantees

- Mississippi does not make the domain model disappear. Developers still write commands, events, handlers, reducers, saga steps, and projection types explicitly.
- Mississippi is not a general-purpose wrapper that infers arbitrary architecture from any C# codebase. It depends on conventions, namespaces, and generator attributes.
- Mississippi is not promising public API stability yet. The repository is explicitly pre-1.0.

## Trade-Offs

- The framework reduces boilerplate by being opinionated. That increases leverage, but it also means teams must learn Mississippi-specific conventions.
- The model is easier to follow when a team accepts the separation between write state, event streams, read models, generated transport, and client store behavior. Teams looking for a minimal abstraction layer may find this too structured.
- Several Mississippi areas can be adopted independently, but the strongest payoff comes from using the pieces together.

## Related Tasks and Reference

- Use [Write Model](./write-model.md) when the question is about commands, events, reducers, and effects inside aggregates.
- Use [Read Models and Client Sync](./read-models-and-client-sync.md) when the question is about projections, generated GET endpoints, SignalR notifications, and client refresh behavior.
- Use [Sagas and Orchestration](./sagas-and-orchestration.md) when the question is about ordered steps and compensation.
- Use [Domain Modeling](../domain-modeling/index.md) or [Inlet](../inlet/index.md) when you need subsystem-specific detail.

## Summary

Mississippi eliminates the seam work between event sourcing, Orleans, and client delivery by treating them as one architecture. Define the domain types once, and the framework composes the runtime, gateway, and client surfaces around them.

## Next Steps

- [Write Model](./write-model.md)
- [Read Models and Client Sync](./read-models-and-client-sync.md)
- [Sagas and Orchestration](./sagas-and-orchestration.md)
- [Design Goals and Trade-Offs](./design-goals-and-trade-offs.md)
