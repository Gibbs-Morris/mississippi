---
sidebar_position: 1
title: Components
description: Reference documentation for all Mississippi components
---

Mississippi is composed of modular components that move domain changes into
live UX state. This section provides detailed reference for each component.

## Component Index

Components are listed in data-flow order (server to client):

| Component | Purpose | Tier |
| --- | --- | --- |
| [Aggregates](./aggregates.md) | Command handling and event production | Silo |
| [Brooks](./brooks.md) | Append-only event streams | Silo |
| [UX Projections](./ux-projections.md) | Composable read models for UX state | Silo |
| [Snapshots](./snapshots.md) | Accelerated projection rebuilds | Silo |
| [Aqueduct](./aqueduct.md) | Orleans-backed SignalR backplane | Silo + Server |
| [Inlet](./inlet.md) | Real-time projection subscriptions | All tiers |
| [Reservoir](./reservoir/) | Redux-style client state container | Client |
| [Refraction](./refraction.md) | Holographic HUD component library | Client |

## Extensibility

Components that support custom implementations:

| Component | Interface | Default | Documentation |
| --- | --- | --- | --- |
| Event storage | `IBrookStorageProvider` | Cosmos DB | [Custom Event Storage](./custom-event-storage.md) |
| Snapshots | `ISnapshotStorageProvider` | Cosmos DB | [Custom Snapshot Storage](./custom-snapshot-storage.md) |

## SDK Packages

Mississippi provides SDK meta-packages by deployment target:

| Package | Use Case | Components Included |
| --- | --- | --- |
| `Sdk.Client` | Blazor WebAssembly | Reservoir, Inlet Client |
| `Sdk.Server` | ASP.NET API | Inlet Hub, Aqueduct Server |
| `Sdk.Silo` | Orleans host | Aggregates, Brooks, Projections |

See [SDK Reference](./sdk.md) for package contents and versions.

## Source Generators

Reduce boilerplate with source generators:

| Attribute | Purpose |
| --- | --- |
| `[GenerateAggregateEndpoints]` | Silo registration, server controller, client state |
| `[GenerateCommand]` | DTOs, mappers, HTTP endpoints for commands |
| `[GenerateProjectionEndpoints]` | Read-only endpoints and client subscriptions |

## Related Topics

- [Architecture](../concepts/architecture.md) — Deployment stack diagram
- [Getting Started](../getting-started/index.md) — Installation and setup
- [Reservoir](./reservoir/) — Client state management detail
