---
id: inlet-getting-started
title: Inlet Getting Started
sidebar_label: Getting Started
sidebar_position: 1
description: Start with Inlet by composing its client registrations on top of the Reservoir builder.
---

# Inlet Getting Started

## Overview

Use this page when you need the first verified Inlet client startup path.

For Blazor clients, Inlet now composes on top of `IReservoirBuilder`. The verified startup pattern is:

- create the Reservoir builder with `AddReservoir()`
- add Inlet client registrations on that builder
- optionally add SignalR-based projection synchronization through `AddInletBlazorSignalR(...)`

## First Working Setup

This example matches the public Inlet client extensions and the Spring sample startup.

```csharp
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using Mississippi.Inlet.Client;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Client;


WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
IReservoirBuilder reservoir = builder.AddReservoir();

reservoir.AddInletClient();
reservoir.AddInletBlazorSignalR(signalR => signalR
    .WithHubPath("/hubs/inlet")
    .ScanProjectionDtos(typeof(MyProjectionDto).Assembly));
```

The builder-based receiver is verified in:

- [InletClientRegistrations.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletClientRegistrations.cs)
- [InletBlazorRegistrations.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletBlazorRegistrations.cs)
- [InletBlazorSignalRBuilder.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletBlazorSignalRBuilder.cs)

## What Inlet Adds To The Reservoir Builder

The verified client-side extensions are:

- `AddInletClient()` to register projection state, projection registry support, and the client store adapter
- `AddProjectionPath<T>(path)` to register explicit projection-path mappings
- `AddInletBlazor()` for Blazor-specific client composition points
- `AddInletBlazorSignalR(...)` to configure SignalR-driven projection refresh

## Generated Client Composition

Inlet client generators now emit builder-based registration methods.

Depending on the generated surface for a domain, those methods can include:

- `Add{Aggregate}AggregateFeature()`
- `Add{Saga}SagaFeature()`
- `AddProjectionsFeature()`
- `Add{Domain}Client()`

All of those generated client methods extend `IReservoirBuilder`.

## When To Stay In Inlet

Stay in Inlet when the concern is alignment across client fetch, projection metadata, generated registrations, SignalR notifications, and projection DTO discovery.

Move to [Reservoir](../../reservoir/index.md) when the issue is only about client-state composition and not about projection delivery or generated cross-layer alignment.

## Summary

Inlet client startup now begins on the Reservoir builder. Create `IReservoirBuilder` with `AddReservoir()`, then compose Inlet client, projection, and SignalR registrations on that builder.

## Next Steps

- Read [Inlet How To](../how-to/how-to.md) for a full builder-composition walkthrough.
- Use [Inlet Reference](../reference/reference.md) for the exact public and generated registration surface.
- Continue into the [Spring Sample](../../samples/spring-sample/index.md) for a verified end-to-end client example.
