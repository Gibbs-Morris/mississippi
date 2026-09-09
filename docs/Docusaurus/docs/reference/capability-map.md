---
title: Capability and Package Map
description: Find the Mississippi capability, package, and development path for the application you want to build.
sidebar_position: 2
sidebar_label: Capability and Package Map
---

# Capability and Package Map

## Overview

Choose a Mississippi capability by the work your application needs to do, then use the package tables to find its contracts and implementation. A domain feature commonly spans several packages: your business rules stay in the domain model while generators connect the runtime, gateway, and client.

This reference maps the projects under `src/` to their consumer roles. Package identities use the `Mississippi.` prefix defined in [Directory.Build.props](https://github.com/Gibbs-Morris/mississippi/blob/main/Directory.Build.props); [src/Directory.Build.props](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Directory.Build.props) enables packing for source projects.

## Choose a Capability

| Your application needs to | Capability | Start with |
| --- | --- | --- |
| Accept a business request only when its rules hold | Commands, handlers, and aggregate state | [Write model](../concepts/write-model.md) |
| Keep a durable history of accepted changes | Named brooks and persisted events | [Brooks](../brooks/index.md) |
| Reconstruct state from events | Event reducers and snapshots | [Tributary](../tributary/index.md) |
| Present several views of the same business history | UX projections | [Build projections](../samples/spring-sample/tutorials/building-projections.md) |
| Coordinate work across aggregates | Saga steps and compensation | [Build a saga](../samples/spring-sample/tutorials/building-a-saga.md) |
| React to accepted events | Event effects and worker-grain effects | [Domain Modeling](../domain-modeling/index.md) |
| Generate HTTP endpoints, DTOs, and client actions | Inlet source generators | [Inlet](../inlet/index.md) |
| Refresh subscribed UI state after server changes | Inlet projection subscriptions and Reservoir state | [Read models and client sync](../concepts/read-models-and-client-sync.md) |
| Make client state changes explicit and inspectable | Reservoir actions, reducers, selectors, and effects | [Reservoir](../reservoir/index.md) |
| Deliver SignalR messages across gateway instances | Aqueduct backplane | [Aqueduct](../aqueduct/index.md) |
| Compose Blazor screens from state and events | Refraction components and scenes | [Refraction](../refraction/index.md) |
| Expose domain operations to an AI tool client | Generated MCP tools and metadata | [Spring MCP setup](../samples/spring-sample/how-to/mcp-server-vscode-testing.md) |
| Develop a feature with an AI coding assistant | Explicit rules, generated integration, and behavioral verification | [Build a feature with AI](../how-to/build-with-ai.md) |

## Package Roles

`Abstractions` packages provide contracts and extension points. `Runtime` packages supply Orleans execution or server implementations. `Gateway` packages integrate with ASP.NET Core. `Client` packages provide client integration. `TestHarness` packages support tests of the behavior you author.

Use the SDK composition packages when following the full application pattern. Use individual packages for a focused capability, such as Reservoir state management or Aqueduct's SignalR backplane. A package's project file, linked below, is the authoritative dependency list.

## Application Composition

| Package | Consumer role |
| --- | --- |
| [Mississippi.Sdk.Runtime](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Sdk.Runtime/Sdk.Runtime.csproj) | Composes runtime dependencies, Cosmos providers, and Inlet runtime generators for a silo project |
| [Mississippi.Sdk.Gateway](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Sdk.Gateway/Sdk.Gateway.csproj) | Composes gateway dependencies and Inlet gateway generators |
| [Mississippi.Sdk.Client](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Sdk.Client/Sdk.Client.csproj) | Composes client hosting, Inlet, Reservoir, and Inlet client generators |
| [Mississippi.Hosting.Client](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Hosting.Client/Hosting.Client.csproj) | Provides `AddMississippiClient()` and `MississippiClientBuilder` for Blazor WebAssembly startup |

See [Spring host applications](../samples/spring-sample/concepts/host-applications.md) for the domain, runtime, gateway, and client project boundaries. For a client entry point, use [Inlet getting started](../inlet/getting-started/getting-started.md).

## Business Behavior

| Package | Consumer role |
| --- | --- |
| [Mississippi.DomainModeling.Abstractions](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Abstractions/DomainModeling.Abstractions.csproj) | Command handlers, operation results, aggregate/projection grain contracts, event effects, and saga contracts |
| [Mississippi.DomainModeling.Runtime](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/DomainModeling.Runtime.csproj) | Executes aggregate commands, event effects, sagas, and UX projections |
| [Mississippi.DomainModeling.Gateway](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Gateway/DomainModeling.Gateway.csproj) | Base classes for aggregate services/controllers and projection controllers |
| [Mississippi.DomainModeling.TestHarness](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.TestHarness/DomainModeling.TestHarness.csproj) | Tests handlers, reducers, aggregate scenarios, and effects without hosting the distributed application |

Start with [building an aggregate](../samples/spring-sample/tutorials/building-an-aggregate.md). The same explicit command and event types give developers and AI assistants a small, named unit of behavior to implement and review.

## Event History, State, and Storage

| Package | Consumer role |
| --- | --- |
| [Mississippi.Brooks.Abstractions](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Abstractions/Brooks.Abstractions.csproj) | Brook identity, event envelopes, storage-name attributes, and read/write grain contracts |
| [Mississippi.Brooks.Runtime](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime/Brooks.Runtime.csproj) | Brook readers, writers, and cursor tracking |
| [Mississippi.Brooks.Runtime.Storage.Abstractions](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime.Storage.Abstractions/Brooks.Runtime.Storage.Abstractions.csproj) | Event storage provider, reader, and writer contracts |
| [Mississippi.Brooks.Runtime.Storage.Cosmos](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime.Storage.Cosmos/Brooks.Runtime.Storage.Cosmos.csproj) | Cosmos event storage with Azure Blob lease locking |
| [Mississippi.Brooks.Serialization.Abstractions](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Serialization.Abstractions/Brooks.Serialization.Abstractions.csproj) | Serialization provider, reader, and writer contracts |
| [Mississippi.Brooks.Serialization.Json](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Serialization.Json/Brooks.Serialization.Json.csproj) | JSON event serialization |
| [Mississippi.Tributary.Abstractions](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Abstractions/Tributary.Abstractions.csproj) | Event reducer, snapshot, and snapshot-key contracts |
| [Mississippi.Tributary.Runtime](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime/Tributary.Runtime.csproj) | Composes reducers and reconstructs, caches, and persists snapshots |
| [Mississippi.Tributary.Runtime.Storage.Abstractions](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime.Storage.Abstractions/Tributary.Runtime.Storage.Abstractions.csproj) | Snapshot storage provider, reader, and writer contracts |
| [Mississippi.Tributary.Runtime.Storage.Cosmos](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime.Storage.Cosmos/Tributary.Runtime.Storage.Cosmos.csproj) | Cosmos snapshot storage |
| [Mississippi.Common.Runtime.Storage.Abstractions](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Common.Runtime.Storage.Abstractions/Common.Runtime.Storage.Abstractions.csproj) | Shared retry-policy contract for storage implementations |
| [Mississippi.Common.Runtime.Storage.Cosmos](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Common.Runtime.Storage.Cosmos/Common.Runtime.Storage.Cosmos.csproj) | Cosmos retry policy for transient storage failures |

Use [Brooks storage providers](../brooks/storage-providers/index.md) and [Tributary storage providers](../tributary/storage-providers/index.md) for the event and snapshot provider boundaries. Keep the persisted event history distinct from the derived state that reducers reconstruct.

## Generated Interfaces and Client Synchronization

| Package | Consumer role |
| --- | --- |
| [Mississippi.Inlet.Abstractions](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Abstractions/Inlet.Abstractions.csproj) | Shared projection-path metadata |
| [Mississippi.Inlet.Generators.Abstractions](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/Inlet.Generators.Abstractions.csproj) | Attributes for generated endpoints, commands, sagas, DTO customization, authorization, and MCP tools |
| [Mississippi.Inlet.Generators.Core](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Core/Inlet.Generators.Core.csproj) | Shared analysis and generation support used by the target-specific generators |
| [Mississippi.Inlet.Runtime.Abstractions](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Runtime.Abstractions/Inlet.Runtime.Abstractions.csproj) | Projection discovery, subscription, and authorization metadata contracts |
| [Mississippi.Inlet.Runtime](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Runtime/Inlet.Runtime.csproj) | Runtime projection registration and subscription handling |
| [Mississippi.Inlet.Runtime.Generators](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Runtime.Generators/Inlet.Runtime.Generators.csproj) | Generates domain, aggregate, projection, and saga silo registrations and saga status reducers |
| [Mississippi.Inlet.Gateway.Abstractions](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Gateway.Abstractions/Inlet.Gateway.Abstractions.csproj) | Server projection notification contracts and in-process notifier integration |
| [Mississippi.Inlet.Gateway](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Gateway/Inlet.Gateway.csproj) | Inlet SignalR hub, gateway registration, and subscription authorization options |
| [Mississippi.Inlet.Gateway.Generators](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Gateway.Generators/Inlet.Gateway.Generators.csproj) | Generates HTTP controllers, server DTOs, MCP tools, and gateway registrations |
| [Mississippi.Inlet.Client.Abstractions](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Abstractions/Inlet.Client.Abstractions.csproj) | Projection state, subscription actions, selectors, and client contracts |
| [Mississippi.Inlet.Client](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/Inlet.Client.csproj) | Fetches projections, manages SignalR integration, and exposes projection-aware Blazor components |
| [Mississippi.Inlet.Client.Generators](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Generators/Inlet.Client.Generators.csproj) | Generates client DTOs, mappers, command/saga actions, effects, state, reducers, and registrations |

Generation operates on attributed domain types. Application code still supplies business rules, host configuration, and UI composition. See [Inlet reference](../inlet/reference/reference.md) for the current client builder entry points.

## Client State and Blazor UI

| Package | Consumer role |
| --- | --- |
| [Mississippi.Reservoir.Abstractions](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/Reservoir.Abstractions.csproj) | Store, feature state, action, reducer, effect, middleware, and builder contracts |
| [Mississippi.Reservoir.Core](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Core/Reservoir.Core.csproj) | Store execution, feature registration, reducer/effect composition, and memoized selectors |
| [Mississippi.Reservoir.Client](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Client/Reservoir.Client.csproj) | Blazor store components, navigation/lifecycle features, and Redux DevTools integration |
| [Mississippi.Reservoir.TestHarness](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.TestHarness/Reservoir.TestHarness.csproj) | Given/When/Then scenarios for feature reducers, effects, emitted actions, and resulting state |
| [Mississippi.Refraction.Abstractions](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Refraction.Abstractions/Refraction.Abstractions.csproj) | Theme, focus, and motion preference contracts |
| [Mississippi.Refraction.Client](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Refraction.Client/Refraction.Client.csproj) | Blazor components, state/event contracts, and design tokens |
| [Mississippi.Refraction.Client.StateManagement](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Refraction.Client.StateManagement/Refraction.Client.StateManagement.csproj) | Reservoir-connected scenes that pass state to Refraction components and handle their events |

For state management alone, start with [Reservoir getting started](../reservoir/getting-started/getting-started.md). Use [Refraction](../refraction/index.md) for component composition on top of that state.

## Distributed SignalR and Shared Mapping

| Package | Consumer role |
| --- | --- |
| [Mississippi.Aqueduct.Abstractions](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Abstractions/Aqueduct.Abstractions.csproj) | Backplane grain contracts, messages, keys, and options |
| [Mississippi.Aqueduct.Gateway](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Gateway/Aqueduct.Gateway.csproj) | SignalR lifetime manager, local connection handling, and Orleans stream integration |
| [Mississippi.Aqueduct.Runtime](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Runtime/Aqueduct.Runtime.csproj) | Orleans grains for connection routing, groups, users, and server tracking |
| [Mississippi.Common.Abstractions](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Common.Abstractions/Common.Abstractions.csproj) | Mapper contracts, collection mapping, and mapping registration |

## Summary

Packages define installation and extension boundaries. Commands, events, reducers, projections, and actions define the application behavior that you and your team work on.

## Next Steps

- [Build a feature with AI](../how-to/build-with-ai.md) for a repeatable development workflow.
- [Build an aggregate](../samples/spring-sample/tutorials/building-an-aggregate.md) for a concrete domain example.
- [Reservoir getting started](../reservoir/getting-started/getting-started.md) for client state management.
