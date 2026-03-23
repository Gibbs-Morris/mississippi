---
id: reservoir-getting-started
title: Reservoir Getting Started
sidebar_label: Getting Started
sidebar_position: 1
description: Start with Reservoir by creating a Reservoir-only builder and composing feature registrations on top of it.
---

# Reservoir Getting Started

## Overview

Use this page when you need the first verified Reservoir-only startup path.

Reservoir now starts from a builder-based registration model. The first successful outcome is creating an `IReservoirBuilder` with `AddReservoir()` and then composing feature registrations on that builder.

If you are building a full Mississippi client application, start with `AddMississippiClient()` instead and use `client.Reservoir(...)` to reach this same subsystem builder.

## First Working Setup

This example shows the current Blazor WebAssembly entry point verified in Reservoir and sample code.

```csharp
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Client;
using Mississippi.Reservoir.Client.BuiltIn;


WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
IReservoirBuilder reservoir = builder.AddReservoir();

reservoir.AddReservoirBlazorBuiltIns();
reservoir.AddReservoirDevTools();
```

This startup shape is verified by the public [ReservoirWebAssemblyHostBuilderRegistrations.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Client/ReservoirWebAssemblyHostBuilderRegistrations.cs) entry point and by the [LightSpeed client startup](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/LightSpeed/LightSpeed.Client/Program.cs).

## Builder Entry Points

Reservoir exposes two verified entry points that both produce the same public builder contract:

- `services.AddReservoir()` when startup code begins from an `IServiceCollection`
- `builder.AddReservoir()` when startup code begins from a `WebAssemblyHostBuilder`

Both return `IReservoirBuilder`. That builder is the public composition surface for Reservoir-only apps and for higher-level callers such as `MississippiClientBuilder.Reservoir(...)`.

Source code:

- [ReservoirRegistrations.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Core/ReservoirRegistrations.cs)
- [ReservoirWebAssemblyHostBuilderRegistrations.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Client/ReservoirWebAssemblyHostBuilderRegistrations.cs)

## What To Add On The Builder

After creating the builder, compose one line per concern:

- feature state registrations through `AddFeatureState<TState>(...)`
- built-in client features through `AddReservoirBlazorBuiltIns()`, `AddBuiltInNavigation()`, or `AddBuiltInLifecycle()`
- DevTools through `AddReservoirDevTools(...)`
- Inlet client registrations through `AddInletClient()` and `AddInletBlazorSignalR(...)`

This is the direction of the public registration model going forward. The builder keeps host startup readable while letting each package add only its own registrations.

## Feature Configuration

This example shows the feature-level builder used inside `AddFeatureState<TState>(...)`.

```csharp
reservoir.AddFeatureState<MyFeatureState>(feature => feature
    .AddReducer<MyAction>(MyReducers.Reduce)
    .AddActionEffect<MyActionEffect>());
```

The callback receives `IReservoirFeatureBuilder<TState>`, which is the public feature-scoped surface for reducers and action effects.

Source code:

- [IReservoirBuilder.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/IReservoirBuilder.cs)
- [IReservoirFeatureBuilder.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/IReservoirFeatureBuilder.cs)

## When To Stay In Reservoir

- Read the [Reservoir overview](../index.md).
- Read [Refraction](../../refraction/index.md) if the UI layer boundary is still unclear.

Stay in Reservoir when the concern is the state-management model itself: store behavior, feature state, reducers, effects, middleware, built-in client features, or the builder surface that composes them.

## Summary

Reservoir-only startup begins by creating `IReservoirBuilder` with `AddReservoir()` and composing feature registrations on that builder.

## Next Steps

- Read [Reservoir Concepts](../concepts/concepts.md) for the top-level builder and feature-builder mental model.
- Read [Reservoir Reference](../reference/reference.md) for the exact public registration surface.
- Use [Inlet Getting Started](../../inlet/getting-started/getting-started.md) if the next step is a full Mississippi client app with client sync on top of Reservoir.
