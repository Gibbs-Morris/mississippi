---
id: glossary
title: Glossary
sidebar_label: Glossary
sidebar_position: 1
description: Look up Mississippi terms and add short definitions in one place.
---

# Glossary

<!-- Contributor note: add new terms to the matching section and keep each section alphabetized. -->

## Overview

Look up Mississippi and event-sourcing terminology in one place.

## Terms

### Architecture Patterns

| Term | Description |
| --- | --- |
| CQRS | A standard architectural pattern that separates command-side write behavior from query-side read behavior. Mississippi applies this split through aggregates on the write side and projections on the read side. |
| Event Sourcing | A standard pattern that captures every change to application state as a sequence of events so current or past state can be rebuilt from the event log. Mississippi persists domain changes as event streams in Brooks. |
| Redux | A standard predictable state-management pattern and library centered on a single store, dispatched actions, and reducers that calculate new state from prior state plus action input. Mississippi's Reservoir library implements this pattern for Blazor client state. |
| Virtual actor model | A standard distributed-systems model where stateful actors are addressed by identity, activated on demand, and communicate through message passing instead of shared mutable state. Mississippi uses Orleans virtual actors (grains) as the runtime foundation. |

### Domain and Workflow Concepts

| Term | Description |
| --- | --- |
| Aggregate | A standard DDD concept: the consistency boundary for a piece of domain state and behavior. In Mississippi, commands target one aggregate instance at a time and emit events instead of mutating state directly. |
| Command | A request to change domain state. In Mississippi, commands are validated against the current aggregate state and succeed by producing events. |
| Compensation | An explicit business-defined undo path for previously completed saga steps. In Mississippi, compensation runs only for steps that implement `ICompensatable<TSaga>`. |
| Effect | Logic that reacts after events are persisted. In Mississippi, synchronous effects may yield additional events, while fire-and-forget effects run asynchronously outside the request completion path. |
| Event | An immutable record of something that happened in the domain. In Mississippi, events are persisted to a brook and used to rebuild aggregate and projection state. |
| Projection | The Mississippi mechanism that builds a read model from events. Reducers fold events into projection state, which can be exposed over HTTP and synchronized to clients. |
| Read model | A query-optimized view of state derived from events. In Mississippi, projections are the concrete read models; see *Projection*. |
| Reducer | Logic that folds events into state. In Mississippi, reducers rebuild aggregate or projection state from the persisted event stream instead of allowing direct mutation from commands. |
| Saga | A standard long-running workflow pattern for coordinating multiple steps, often across aggregates, with explicit progress and compensation rather than one atomic transaction. In Mississippi, sagas coordinate multi-aggregate workflows with explicit compensation via `ICompensatable<TSaga>`. |
| Snapshot | A stored version of derived state used to avoid replaying an entire event history from the beginning. Mississippi uses snapshot reconstruction in Tributary to speed state rebuilds. |

### Runtime and Delivery Concepts

| Term | Description |
| --- | --- |
| Brook | A Mississippi logical event stream for one aggregate type and entity ID. Ordering is meaningful within a brook, not across the whole system. |
| Brook key | The composite identity (aggregate type + entity ID) that uniquely addresses a single brook. |
| Cursor | The tracked latest position of a brook or stream. Mississippi uses cursor grains to observe and expose brook version progress. |
| Grain | An Orleans virtual actor: a single-threaded, addressable unit of state and logic activated on demand within a silo. Mississippi grains host aggregates, projections, cursors, and other runtime components. |
| Silo | An Orleans host process that manages a set of grains and participates in the cluster. Mississippi applications run one or more silos. |
| Subscription | A client's registration to receive projection change notifications. In Mississippi, Inlet manages SignalR subscriptions by projection path and entity ID and deduplicates underlying brook subscriptions per connection. |
| UX projection | A Mississippi projection specifically designed for driving UI state. UX projections are defined in Domain Modeling and synchronized to clients through Inlet. |

### Technology Foundations

| Term | Description |
| --- | --- |
| ASP.NET | Microsoft's web application platform for .NET. In Mississippi, this typically means ASP.NET Core on the gateway side for HTTP APIs and SignalR hosts. |
| Microsoft Orleans | Microsoft's distributed application framework based on the virtual actor model. Mississippi uses Orleans for isolated stateful runtime execution, messaging, and cluster coordination. |
| SignalR | ASP.NET Core's real-time messaging library for pushing updates from server to connected clients without client polling. |
| SignalR backplane | A standard scaling pattern that propagates SignalR messages across multiple servers so clients receive the same real-time updates regardless of which host they are connected to. |

### Mississippi Areas

| Term | Description |
| --- | --- |
| [Aqueduct](../aqueduct/index.md) | Mississippi's Orleans-backed SignalR backplane and push-delivery layer for distributed real-time messaging across hosts. |
| [Brooks](../brooks/index.md) | Mississippi's event-stream and event-storage foundation. It owns append, read, cursor, serialization, and storage-provider boundaries. |
| [Domain Modeling](../domain-modeling/index.md) | Mississippi's domain-facing layer for aggregates, sagas, event effects, and UX projections. It turns lower-level streams and state reconstruction into concrete domain behavior. |
| [Inlet](../inlet/index.md) | Mississippi's composition and source-generation layer. It keeps projection DTOs, generated HTTP endpoints, runtime registrations, and client subscription wiring aligned across the stack. |
| [Refraction](../refraction/index.md) | Mississippi's Blazor UX component library built around a state-down, events-up interaction model that keeps UI interaction contracts separate from application state. |
| [Reservoir](../reservoir/index.md) | Mississippi's Redux-style client state-management library, including store, actions, reducers, selectors, effects, middleware, and UI integration patterns. |
| [Tributary](../tributary/index.md) | Mississippi's reducer and snapshot layer. It turns event streams into derived state that can be rebuilt efficiently and persisted as snapshots. |

## Summary

This glossary collects the core vocabulary used across Mississippi documentation and source code.

## Next Steps

- Explore the [Concepts overview](../concepts/index.md) for deeper explanations of these terms in context.
- See the [Architectural model](../concepts/architectural-model.md) for how these pieces fit together.
- Return to [Documentation Home](../index.md) for the main navigation.
