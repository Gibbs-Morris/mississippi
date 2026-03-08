---
id: concepts-read-models-and-client-sync
title: Read Models and Client Sync
sidebar_label: Read Models and Client Sync
sidebar_position: 4
description: Explain how Mississippi builds projections, exposes them over HTTP, and synchronizes client state through SignalR notifications plus HTTP refreshes.
---

# Read Models and Client Sync

## Overview

Mississippi treats projections as reducer-driven read models over a brook.

Projection types are rebuilt from events by reducers, served by `IUxProjectionGrain<TProjection>`, exposed through generated read endpoints, and optionally synchronized into client state through Inlet and Reservoir.

## The Problem This Solves

Event-sourced write models are not usually the best shape for reads.

Applications need read-optimized views, stable HTTP query endpoints, and a client synchronization path that does not duplicate projection logic in several places. Mississippi addresses that by separating projection definition from delivery:

- reducers define how events become read state
- UX projection grains serve latest and historical versions
- generated controllers expose the read model over HTTP
- SignalR notifications tell clients which projection changed
- Reservoir reducers update client state from actions, not from direct mutation

## Core Idea

The read model is event-derived, versioned, and eventually observed by clients.

Mississippi does not push full projection payloads over SignalR by default. Instead, it pushes `(path, entityId, version)` metadata and lets the client fetch the exact projection version over HTTP.

## How It Works

This diagram shows the verified production path used by Inlet.

```mermaid
flowchart LR
    A[Aggregate events appended to Brooks] --> B[Projection reducers rebuild snapshots]
    B --> C[UxProjectionGrain latest version moves]
    C --> D[InletSubscriptionGrain observes brook cursor]
    D --> E[SignalR ProjectionUpdatedAsync(path entityId version)]
    E --> F[AutoProjectionFetcher GET /api/projections/{path}/{entityId}/at/{version}]
    F --> G[ProjectionUpdatedAction<T>]
    G --> H[ProjectionsReducer updates Reservoir state]
```

The main pieces are:

- Projection records are annotated with `[ProjectionPath(...)]`, `[BrookName(...)]`, and usually `[GenerateProjectionEndpoints]`.
- Projection reducers inherit from `EventReducerBase<TEvent, TProjection>`.
- `IUxProjectionGrain<TProjection>` serves the latest projection state, historical versions, and the latest version number.
- `UxProjectionControllerBase<TProjection, TDto>` exposes `GET`, `GET at version`, and `GET version` endpoints.
- `InletHub` manages client subscriptions by projection path and entity ID.
- `InletSubscriptionGrain` deduplicates brook stream subscriptions per SignalR connection and sends only `(path, entityId, newVersion)` to the client.
- `InletSignalRActionEffect` fetches the updated DTO over HTTP and dispatches Reservoir actions.

## Guarantees

- UX projection grains support both latest reads and historical version reads.
- Generated read endpoints use the projection path and entity ID shape rather than exposing brook internals to clients.
- Latest projection endpoints set an `ETag` header containing the current version and return `304 Not Modified` when the client sends a matching `If-None-Match` header.
- SignalR update messages carry metadata, not the full projection body.
- `InletSubscriptionGrain` deduplicates brook subscriptions so several projections on the same brook and entity can share one underlying stream subscription per connection.
- Reservoir projection state is updated through actions and reducers, not through direct mutation.

## Non-Guarantees

- Mississippi does not give clients strong read-after-write consistency across the entire stack. Projection delivery is eventually consistent.
- A successful command does not mean every client has already received or fetched the new projection version.
- Client subscription generation is optional. `GenerateProjectionEndpointsAttribute.GenerateClientSubscription` can be set to `false`.
- The repository contains an older `UxProjectionSubscriptionGrain` abstraction, but the active Inlet SignalR path is implemented through `InletHub` and `InletSubscriptionGrain`.

## Trade-Offs

- Splitting notification from fetch keeps WebSocket payloads small and allows versioned HTTP caching, but it adds one more hop between update detection and UI refresh.
- Projection paths make subscriptions easier for clients to reason about, but they also make projection naming part of the public surface.
- Reservoir keeps client state predictable, but teams must learn action and reducer flow instead of mutating view-model state directly.

## Related Tasks and Reference

- Use [Write Model](./write-model.md) for the event-producing side of this flow.
- Use [Sagas and Orchestration](./sagas-and-orchestration.md) when the projection reflects a multi-step workflow rather than a single aggregate.
- Use [Inlet](../inlet/index.md) and [Reservoir](../reservoir/index.md) for subsystem-specific detail.

## Summary

Mississippi read models are reducer-built projections served by version-aware grains and synchronized to clients through SignalR metadata plus HTTP fetches.

## Next Steps

- [Sagas and Orchestration](./sagas-and-orchestration.md)
- [Design Goals and Trade-Offs](./design-goals-and-trade-offs.md)
- [Inlet](../inlet/index.md)
- [Reservoir](../reservoir/index.md)
