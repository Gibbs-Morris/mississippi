---
id: home
title: Mississippi Framework
sidebar_label: Home
sidebar_position: 1
slug: /
description: Landing page for the Mississippi framework documentation.
---

# Mississippi Framework

## Overview

Mississippi is a .NET framework for distributed application development. The repository identifies its technology stack as .NET 10.0, Microsoft Orleans, and Azure Cosmos DB.
See the main README for the authoritative overview and stack details: [README](https://github.com/Gibbs-Morris/mississippi/blob/main/README.md).

:::warning Early alpha
The README marks Mississippi as early alpha, notes that APIs may change without notice, and recommends against production use.
See the warning in the main README: [README](https://github.com/Gibbs-Morris/mississippi/blob/main/README.md).
:::

## Core components

The table below links each component to its primary source entry point and the related documentation page.

| Area | What it provides | Source | Docs |
|---|---|---|---|
| **Event and brook naming** | `EventStorageNameAttribute` defines event storage name and version; `BrookNameAttribute` defines brook naming. | [EventStorageNameAttribute](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Attributes/EventStorageNameAttribute.cs), [BrookNameAttribute](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Attributes/BrookNameAttribute.cs) | [Events](server-state-management/events.md) |
| **Aggregates and commands** | `GenericAggregateGrain` is a generic aggregate grain that processes commands. | [GenericAggregateGrain](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Aggregates/GenericAggregateGrain.cs) | [Aggregates](server-state-management/aggregates.md), [Commands](server-state-management/commands.md) |
| **Projections and reducers** | `GenerateProjectionEndpointsAttribute` marks projections for read endpoint generation; `ReducerTestHarness` provides a fluent reducer test harness. | [GenerateProjectionEndpointsAttribute](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateProjectionEndpointsAttribute.cs), [ReducerTestHarness](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Testing/Projections/ReducerTestHarness.cs) | [Projections](server-state-management/projections.md), [Projection Reducers](server-state-management/projection-reducers.md) |
| **Reservoir store** | `IStore` is the central state container; `IActionReducer` and `IActionEffect` define reducers and effects. | [IStore](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/IStore.cs), [IActionReducer](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/IActionReducer.cs), [IActionEffect](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/IActionEffect%7BTState%7D.cs) | [Reservoir](client-state-management/reservoir.md) |
| **Inlet SignalR** | `InletHub` manages projection subscriptions; `InletSignalRActionEffect` handles client subscription actions. | [InletHub](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Server/InletHub.cs), [InletSignalRActionEffect](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs) | [Inlet](client-server-sync/inlet.md) |
| **Source generation** | `GenerateAggregateEndpointsAttribute` and `GenerateCommandAttribute` mark types for endpoint generation; `AggregateControllerGenerator` generates aggregate controllers. | [GenerateAggregateEndpointsAttribute](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateAggregateEndpointsAttribute.cs), [GenerateCommandAttribute](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateCommandAttribute.cs), [AggregateControllerGenerator](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Server.Generators/AggregateControllerGenerator.cs) | [Source Generation](client-server-sync/source-generation.md) |
| **Testing harnesses** | `AggregateTestHarness` and `ReducerTestHarness` provide fluent test harnesses for aggregates and reducers. | [AggregateTestHarness](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Testing/Aggregates/AggregateTestHarness.cs), [ReducerTestHarness](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Testing/Projections/ReducerTestHarness.cs) | [Command Handlers](server-state-management/command-handlers.md) |
| **Pluggable storage** | `IBrookStorageProvider` and `ISnapshotStorageProvider` expose a `Format` identifier for storage providers. | [IBrookStorageProvider](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Storage/IBrookStorageProvider.cs), [ISnapshotStorageProvider](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Snapshots.Abstractions/ISnapshotStorageProvider.cs) | [Snapshots](server-state-management/snapshots.md) |

## Sample solution

The Spring sample solution lives under [samples/Spring](https://github.com/Gibbs-Morris/mississippi/tree/main/samples/Spring).

## Start exploring

- [Client state management](client-state-management/reservoir.md)
- [Server-side state management](server-state-management/aggregates.md)
- [Client/server sync](client-server-sync/inlet.md)
- [Webserver to silo sync](webserver-to-silo-sync/aspnet-setup.md)

## Summary

- Mississippi is a .NET framework for distributed application development with a .NET 10.0, Orleans, and Azure Cosmos DB stack as described in the README.
- Core components are documented in the sections linked above and map to concrete source entry points.

## Next Steps

- [Client state management](client-state-management/reservoir.md)
- [Server-side state management](server-state-management/aggregates.md)
- [Client/server sync](client-server-sync/inlet.md)
