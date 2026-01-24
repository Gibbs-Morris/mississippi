---
sidebar_position: 1
title: Introduction
description: Welcome to Mississippi, an event sourcing framework for .NET and Orleans
---

Mississippi is an event sourcing framework built on Microsoft Orleans that moves
domain changes into live client UX state. It provides the building blocks for
CQRS/ES applications with real-time Blazor WebAssembly frontends.

## What Mississippi Provides

| Layer | Component | Purpose |
| --- | --- | --- |
| **Domain** | Aggregates | Command handling and event production |
| **Storage** | Brooks | Append-only event streams |
| **Read Models** | UX Projections | Composable, cached read views |
| **Real-Time** | Aqueduct | Orleans-backed SignalR backplane |
| **Client Bridge** | Inlet | WASM subscription and update delivery |
| **State Management** | Reservoir | Redux-style Blazor state container |

## Documentation Sections

### Platform

The [Platform](./platform/index.md) section covers the server-side framework:

- **Aggregates** — Commands, events, and business rules
- **Brooks** — Event stream persistence with Cosmos DB
- **UX Projections** — Read-optimized views for UI components
- **Snapshots** — Accelerated projection rebuilds
- **Aqueduct** — Cross-server SignalR message delivery
- **Inlet** — Real-time projection subscriptions

### Reservoir

The [Reservoir](./reservoir/index.md) section covers client-side state management:

- **Actions** — Immutable messages describing events
- **Reducers** — Pure functions that transform state
- **Effects** — Async operations like API calls
- **Store** — Central state container for Blazor apps

## Quick Start

Install the SDK packages for your project type:

```bash
# Blazor WebAssembly client
dotnet add package Mississippi.Sdk.Client

# ASP.NET API server
dotnet add package Mississippi.Sdk.Server

# Orleans silo host
dotnet add package Mississippi.Sdk.Silo
```

See the [SDK Reference](./platform/sdk.md) for package contents and setup.
