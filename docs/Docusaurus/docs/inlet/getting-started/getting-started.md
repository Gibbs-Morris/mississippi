---
id: inlet-getting-started
title: Inlet Getting Started
sidebar_label: Getting Started
sidebar_position: 1
description: Start with Inlet by composing Mississippi client registrations through AddMississippiClient and Reservoir-only registrations through AddReservoir.
---

# Inlet Getting Started

## Overview

Use this page when you need the first verified Inlet client startup path.

For full Mississippi Blazor clients, the verified startup pattern is:

- create the Mississippi client builder with `AddMississippiClient()`
- compose Reservoir-level registrations inside `client.Reservoir(...)`
- add Inlet client registrations on that Reservoir builder
- optionally add SignalR-based projection synchronization through `AddInletBlazorSignalR(...)`

## Choose The Right Entry Point

- Full Mississippi client app: `builder.AddMississippiClient(...)`
- Reservoir-only state-management app: `builder.AddReservoir()`

Use `AddMississippiClient()` when the app is using Mississippi as the full client composition root. Stay with `AddReservoir()` when the app only wants Reservoir's client-state subsystem without the higher-level Mississippi client builder.

This layering is intentional:

```mermaid
flowchart LR
    A[AddMississippiClient] --> B[MississippiClientBuilder]
    B --> C[Reservoir(...)]
    C --> D[IReservoirBuilder]
    D --> E[Features and Inlet]
```

## First Working Setup

This example matches the current Spring client startup shape.

```csharp
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using Mississippi.Hosting.Client;
using Mississippi.Inlet.Client;


WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.AddMississippiClient(client =>
{
    client.AddMyDomainClient();
    client.Reservoir(reservoir =>
    {
        reservoir.AddInletClient();
        reservoir.AddInletBlazorSignalR(signalR => signalR
            .WithHubPath("/hubs/inlet")
            .ScanProjectionDtos(typeof(MyProjectionDto).Assembly));
    });
});
```

## Migration From The Old Client Root

Before:

```csharp
IReservoirBuilder reservoir = builder.AddReservoir();
reservoir.AddMyDomainClient();
reservoir.AddInletClient();
```

After:

```csharp
builder.AddMississippiClient(client =>
{
    client.AddMyDomainClient();
    client.Reservoir(reservoir =>
    {
        reservoir.AddInletClient();
    });
});
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

The feature-level methods extend `IReservoirBuilder`. The domain-level method extends `MississippiClientBuilder` and routes its work through `client.Reservoir(...)`.

## When To Stay In Inlet

Stay in Inlet when the concern is alignment across client fetch, projection metadata, generated registrations, SignalR notifications, and projection DTO discovery.

Move to [Reservoir](../../reservoir/index.md) when the issue is only about client-state composition and not about projection delivery or generated cross-layer alignment.

## Summary

Inlet client startup for full Mississippi apps now begins with `AddMississippiClient()`, then composes Reservoir and Inlet registrations through `client.Reservoir(...)`. Reservoir-only apps should continue to begin with `AddReservoir()`.

## Next Steps

- Read [Inlet How To](../how-to/how-to.md) for a full builder-composition walkthrough.
- Use [Inlet Reference](../reference/reference.md) for the exact public and generated registration surface.
- Continue into the [Spring Sample](../../samples/spring-sample/index.md) for a verified end-to-end client example.
