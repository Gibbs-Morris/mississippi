---
id: inlet-reference
title: Inlet Reference
sidebar_label: Reference
sidebar_position: 1
description: Reference the current builder-based Inlet client registration surface and the generated client method shapes for MississippiClientBuilder and IReservoirBuilder.
---

# Inlet Reference

## Overview

Inlet is the Mississippi composition and source-generation layer.

## Applies To

- `Mississippi.Inlet.Abstractions`
- `Mississippi.Inlet.Client`
- `Mississippi.Inlet.Gateway`
- `Mississippi.Inlet.Runtime`

## Verified Client Registration Surface

These client-side Inlet extensions compose on `IReservoirBuilder`. Full Mississippi client apps reach them through `MississippiClientBuilder.Reservoir(...)`.

| Method | Receiver | Purpose |
|--------|----------|---------|
| `AddInletClient()` | `IReservoirBuilder` | Register the client projection registry, `ProjectionsFeatureState`, `IInletStore`, and `IProjectionUpdateNotifier` |
| `AddProjectionPath<T>(path)` | `IReservoirBuilder` | Add an explicit projection-path mapping |
| `AddInletBlazor()` | `IReservoirBuilder` | Add the Blazor-specific registration extension point |
| `AddInletBlazorSignalR(...)` | `IReservoirBuilder` | Configure SignalR-driven projection refresh |
| `AddSignalRConnectionFeature()` | `IReservoirBuilder` | Register the SignalR connection state feature |

Source code:

- [InletClientRegistrations.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletClientRegistrations.cs)
- [InletBlazorRegistrations.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletBlazorRegistrations.cs)
- [SignalRConnectionRegistrations.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/SignalRConnection/SignalRConnectionRegistrations.cs)

## InletBlazorSignalRBuilder

`AddInletBlazorSignalR(...)` configures a dedicated `InletBlazorSignalRBuilder`.

| Member | Purpose |
|--------|---------|
| `AddProjectionFetcher<TFetcher>()` | Use a custom `IProjectionFetcher` implementation |
| `ScanProjectionDtos(params Assembly[] assemblies)` | Enable automatic projection DTO discovery and the auto fetcher |
| `WithHubPath(hubPath)` | Set the SignalR hub path |
| `WithRoutePrefix(prefix)` | Set the HTTP projection route prefix used by the auto fetcher |

`AddInletBlazorSignalR(...)` also adds the SignalR connection feature automatically during `Build()`.

Source code:

- [InletBlazorSignalRBuilder.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletBlazorSignalRBuilder.cs)

## Generated Client Method Shapes

The current Inlet client generators emit builder-based client registrations.

| Generator | Generated method shape | Receiver |
|-----------|------------------------|----------|
| Command client generator | `Add{Aggregate}AggregateFeature()` | `IReservoirBuilder` |
| Saga client generator | `Add{Saga}SagaFeature()` | `IReservoirBuilder` |
| Projection client generator | `AddProjectionsFeature()` | `IReservoirBuilder` |
| Domain client generator | `Add{Domain}Client()` | `MississippiClientBuilder` |

Source code:

- [CommandClientRegistrationGenerator.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Generators/CommandClientRegistrationGenerator.cs)
- [SagaClientRegistrationGenerator.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Generators/SagaClientRegistrationGenerator.cs)
- [ProjectionClientRegistrationGenerator.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Generators/ProjectionClientRegistrationGenerator.cs)
- [DomainClientRegistrationGenerator.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Generators/DomainClientRegistrationGenerator.cs)

## Verified Ownership Boundary

- Shared abstractions for projection paths and related metadata
- Client support for projection state and subscriptions
- Gateway support for generated APIs and SignalR delivery
- Runtime support for discovery and generated registrations
- Source generators that align those layers

## Related But Separate Areas

- [Aqueduct](../../aqueduct/index.md) owns the real-time backplane.
- [Reservoir](../../reservoir/index.md) owns the client-state model.
- [Domain Modeling](../../domain-modeling/index.md) owns domain behavior.

## Defaults And Constraints

This reference covers the verified subsystem boundary and the current client-side builder surface. Inlet client registrations assume a Reservoir builder exists; full Mississippi client apps create that builder by starting with `AddMississippiClient()` and then using `Reservoir(...)`.

## Failure Behavior

For generator and runtime registration failure behavior, refer to the [Inlet Operations](../operations/operations.md) page. Generator misalignment typically surfaces at compile time.

## Summary

Use this page as the current active reference for Inlet's builder-based client registrations and the generated client method shapes that compose through `MississippiClientBuilder` and `IReservoirBuilder`.

## Next Steps

- Read [Inlet Concepts](../concepts/concepts.md).
- Read [How To Compose Inlet In Mississippi Client Apps](../how-to/how-to.md) for startup composition guidance.
- Use the [Spring Sample](../../samples/spring-sample/index.md) to see Inlet composition patterns in practice.
