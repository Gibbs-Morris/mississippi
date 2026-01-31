---
id: client-registration
title: Inlet Client Registration
sidebar_label: Client Registration
sidebar_position: 1
description: Register Inlet client services using the generated composite method or manual wiring for advanced scenarios.
---

# Inlet Client Registration

## Overview

Inlet provides a single, generated registration entry point for client apps, plus a manual path for advanced scenarios. Use the generated method unless you need full control over registration order or custom fetcher behavior.

## Quick Start (Generated registration)

### 1) Add the assembly attribute

Add the composite attribute to your client assembly (example from the Spring sample):

```csharp
using Mississippi.Inlet.Generators.Abstractions;

[assembly: GenerateInletClientComposite(AppName = "Spring")]
```

Source: [Spring.Client AssemblyInfo](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Client/Properties/AssemblyInfo.cs)

The attribute configures the generated method name and lets you set the hub path via `HubPath` if needed. Source: [GenerateInletClientCompositeAttribute](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateInletClientCompositeAttribute.cs)

### 2) Call the generated method in Program.cs

```csharp
builder.Services.AddSpringInlet();
```

Source: [Spring.Client Program](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Client/Program.cs)

### 3) Optional: configure SignalR behavior

The generated method includes an overload that accepts `Action<InletBlazorSignalRBuilder>` for optional configuration:

```csharp
builder.Services.AddSpringInlet(signalR =>
{
    signalR.WithHubPath("/hubs/inlet");
    signalR.WithRoutePrefix("/api/projections");
});
```

Source: [InletClientCompositeGenerator](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Generators/InletClientCompositeGenerator.cs)

## Manual registration (advanced scenarios)

Use this when you want full control and are not using the generated composite registration.

```csharp
builder.Services.AddInletClient();
builder.Services.AddReservoirBlazorBuiltIns();

builder.Services.AddInletBlazorSignalR(signalR =>
{
    signalR.WithHubPath("/hubs/inlet");
    signalR.RegisterProjectionDtos(registry =>
    {
        registry.Register("accounts/balance", typeof(AccountBalanceProjectionDto));
    });
});
```

Reference APIs:

- `AddInletClient` + `AddInletBlazorSignalR`: [InletBlazorSignalRBuilder](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletBlazorSignalRBuilder.cs)
- Reservoir built-ins: [ReservoirBlazorBuiltInRegistrations](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Blazor/BuiltIn/ReservoirBlazorBuiltInRegistrations.cs)

## Projection DTO registration options

### Option A: Explicit registration (recommended for compile-time registration)

Use `RegisterProjectionDtos` to explicitly map projection paths to DTO types:

```csharp
signalR.RegisterProjectionDtos(registry =>
{
    registry.Register("accounts/balance", typeof(AccountBalanceProjectionDto));
});
```

Reference: [InletBlazorSignalRBuilder](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletBlazorSignalRBuilder.cs)

### Option B: Assembly scanning (reflection-based)

Use `ScanProjectionDtos` to register DTOs by scanning assemblies for `ProjectionPathAttribute`:

```csharp
signalR.ScanProjectionDtos(typeof(AccountBalanceProjectionDto).Assembly);
```

Reference: [InletBlazorSignalRBuilder](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletBlazorSignalRBuilder.cs)

`ProjectionPathAttribute` identifies the path used for subscriptions and fetches:

Source: [ProjectionPathAttribute](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Abstractions/ProjectionPathAttribute.cs)

## Route prefix for projection fetches

If you are using the automatic projection fetcher, you can set a custom route prefix:

```csharp
signalR.WithRoutePrefix("/api/projections");
```

If you do not set it, the default route prefix is `/api/projections`. Source: [AutoProjectionFetcher](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs)

## Server requirements (for clients to connect)

Clients expect the server to expose the Inlet hub and projection endpoints. In the Spring sample, this is handled by:

- Mapping the hub: [SpringServerMiddlewareRegistrations](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Server/Infrastructure/SpringServerMiddlewareRegistrations.cs)
- Registering Inlet server + projection scanning: [SpringServerRealtimeRegistrations](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Server/Infrastructure/SpringServerRealtimeRegistrations.cs)

## Summary

- Use `Add{App}Inlet()` as the single entry point for client registration.
- Use the optional overload to customize SignalR behavior when needed.
- Use manual registration only when you want full control.
- Register projection DTOs explicitly or by assembly scan, depending on your requirements.
