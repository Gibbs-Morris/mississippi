---
id: concepts-read-models-and-client-sync
title: Read Models and Client Sync
sidebar_label: Read Models and Client Sync
sidebar_position: 4
description: Explain how Mississippi builds projections, exposes them over HTTP, and synchronizes client state through SignalR notifications plus HTTP refreshes.
---

# Read Models and Client Sync

## Overview

Mississippi gives teams a complete path from domain events to client state without hand-wiring each layer.

Projection types are rebuilt from events by reducers. When teams opt into Mississippi's generated delivery surface, those projections become available as latest or historical versions, are exposed through generated HTTP endpoints, and are synchronized into client state through Inlet notifications and Reservoir reducers.

## The Problem This Solves

Event-sourced write models are not usually the best shape for reads.

Applications need read-optimized views, stable HTTP query endpoints, and a client synchronization path that does not duplicate projection logic in several places. Mississippi addresses that by separating projection definition from delivery:

- reducers define how events become read state
- projection access supports latest and historical versions
- generated controllers can expose the read model over HTTP
- SignalR notifications tell clients which projection changed
- Reservoir reducers update client state from actions, not from direct mutation

## Core Idea

The read model is event-derived, versioned, and delivered efficiently.

Mississippi does not push full projection payloads over SignalR. Instead, it sends lightweight `(path, entityId, version)` metadata and lets the client fetch the exact projection over HTTP with ETag-based caching. This keeps WebSocket frames small, enables HTTP-level caching, and avoids the bandwidth cost of pushing full payloads on every change.

## How It Works

This page combines read models and client sync because Mississippi treats them as one delivery path: event-derived state becomes a projection, and that projection becomes HTTP responses and client-state updates.

This diagram shows the projection delivery path from event stream to client state.

```mermaid
flowchart TB
    A[Aggregate events appended to Brooks] --> B[Projection reducers rebuild state]
    B --> C[Latest projection version advances]
    C --> D[SignalR sends path entityId version]
    D --> E[Client fetches projection over HTTP]
    E --> F[Reservoir updates client state]
```

The main pieces in the generated HTTP and Blazor SignalR path are:

- Projection records are annotated with `[ProjectionPath(...)]` and `[BrookName(...)]`, and they add `[GenerateProjectionEndpoints]` when the generated HTTP and client surface is desired.
- Projection reducers inherit from `EventReducerBase<TEvent, TProjection>`.
- The projection runtime serves the latest projection state, historical versions, and the latest version number through `IUxProjectionGrain<TProjection>`.
- `UxProjectionControllerBase<TProjection, TDto>` exposes `GET`, `GET at version`, and `GET version` endpoints.
- `InletHub` manages client subscriptions by projection path and entity ID.
- `InletSubscriptionGrain` deduplicates brook stream subscriptions per SignalR connection and sends only `(path, entityId, newVersion)` to the client.
- In Blazor clients, `builder.AddReservoir()` creates the `IReservoirBuilder` that client packages compose on. `AddInletClient()` and `AddInletBlazorSignalR(...)` both extend that builder.
- When `AddInletBlazorSignalR(...)` is configured, `InletSignalRActionEffect` fetches the updated DTO over HTTP and dispatches Reservoir actions.

## Guarantees

- The projection runtime supports both latest reads and historical version reads.
- Generated read endpoints use the projection path and entity ID shape rather than exposing brook internals to clients.
- Latest projection endpoints set an `ETag` header containing the current version and return `304 Not Modified` when the client sends a matching `If-None-Match` header.
- SignalR update messages carry metadata, not the full projection body.
- `InletSubscriptionGrain` deduplicates brook subscriptions so several projections on the same brook and entity can share one underlying stream subscription per connection.
- Reservoir projection state is updated through actions and reducers, not through direct mutation.

## Non-Guarantees

- Mississippi does not give clients strong read-after-write consistency across the entire stack. Projection delivery is eventually consistent.
- A successful command does not mean every client has already received or fetched the new projection version.
- Client subscription generation is optional. `GenerateProjectionEndpointsAttribute.GenerateClientSubscription` can be set to `false`.
- Not every projection needs real-time client subscription. Teams can expose read endpoints without opting into generated client subscription behavior.

## Trade-Offs

- Splitting notification from fetch keeps WebSocket payloads small and allows versioned HTTP caching, but it adds one more hop between update detection and UI refresh.
- Projection paths make subscriptions easier for clients to reason about, but they also make projection naming part of the public surface.
- Reservoir keeps client state predictable, but teams must learn action and reducer flow instead of mutating view-model state directly.

## Related Tasks and Reference

- Use [Write Model](./write-model.md) for the event-producing side of this flow.
- Use [Sagas and Orchestration](./sagas-and-orchestration.md) when the projection reflects a multi-step workflow rather than a single aggregate.
- Use [Inlet](../inlet/index.md) and [Reservoir](../reservoir/index.md) for subsystem-specific detail.

## Summary

Mississippi read models move from event streams to versioned projection access, generated HTTP endpoints, and client-state updates through one coherent delivery path - no hand-wired controllers, no manual SignalR subscriptions, no imperative client state mutation.

## Next Steps

- [Sagas and Orchestration](./sagas-and-orchestration.md)
- [Design Goals and Trade-Offs](./design-goals-and-trade-offs.md)
- [Inlet](../inlet/index.md)
- [Reservoir](../reservoir/index.md)
