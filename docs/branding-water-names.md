# Mississippi Framework – Water-Themed Naming Glossary

## Purpose

This document explains the water-themed names used throughout the Mississippi codebase (framework, modules, and samples) and what each name means in our context.

## Glossary

| Name | Where It Appears | Meaning In Our Context |
| --- | --- | --- |
| Mississippi | Repo name and `Mississippi.*` namespaces | The overall framework: "the river" where events flow through distributed components to produce consistent state. |
| Brooks | `Mississippi.EventSourcing.Brooks` | The event-stream layer: grains and storage for append-only event history (readers, writers, cursors). |
| Brook | `BrookKey`, `BrookNameAttribute`, `BrookWriterGrain`, `BrookReaderGrain`, etc. | One event stream for a single entity; identified by `brookName|entityId`, with `brookName` formatted like `APP.MODULE.NAME`. |
| Reservoir | `Mississippi.Reservoir` packages | Pure Redux-like state management: stores, reducers, effects, middleware. WASM-safe, no server dependencies. |
| Inlet | `Mississippi.Inlet` packages | Server-side projection management built on Reservoir: Orleans/SignalR integration, real-time updates. |
| Cascade | `samples/Cascade` | Sample application (chat) that showcases event-sourced domain + projections + real-time UI. |
| Crescent | `samples/Crescent` | Sample application(s) used as the "hello world" for the framework; uses `CRESCENT.*` brook names (for example, `CRESCENT.SAMPLE.COUNTER`). |

## The "Water" Story (System Metaphor)

- Commands go into aggregates (write side) and append events into a Brook.
- Events flow downstream into Snapshots and UX Projections (read side).
- Projection changes flow outward to clients via HTTP + SignalR, with Inlet/Reservoir providing a consistent Blazor developer experience.

## What Mississippi Offers (High Level)

- Distributed execution model using Microsoft Orleans (virtual actors / grains).
- Event sourcing building blocks: aggregates (command handling), brooks (event streams), reducers (event → state), snapshots (fast reads), and UX projections (read models for UI/API).
- Real-time UX delivery: projection APIs + SignalR notifications, plus Reservoir/Inlet libraries for reactive Blazor components (Server + WASM).
- Azure Cosmos DB integration for event and snapshot persistence (including Cosmos-specific brooks/snapshots implementations).
- ASP.NET Core integrations backed by Orleans (for example, distributed cache, authentication ticket store, output cache, SignalR hub lifetime manager).
- Early-alpha status: APIs and package names may evolve (see `README.md`).

## Pointers (Repo References)

- `docs/architecture/c1-inlet-context.md`
- `docs/grain-dependencies.md`
- `docs/data-flow-browser-to-cosmos.md`
- `docs/brook-reader-performance-analysis.md`
- `src/EventSourcing.Brooks.Abstractions/Attributes/BrookNameAttribute.cs`
- `src/Inlet.Abstractions/IInletStore.cs`