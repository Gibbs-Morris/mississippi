---
id: inlet-how-to
title: How To Compose Inlet In Mississippi Client Apps
sidebar_label: How To
sidebar_position: 1
description: Compose Mississippi client, Reservoir, Inlet, SignalR support, and generated registrations in a Blazor client startup path.
---

# How To Compose Inlet In Mississippi Client Apps

## Overview

Use this page when you need to wire a Blazor client that combines the Mississippi client builder, Reservoir, Inlet client support, built-in Reservoir features, SignalR projection updates, and generated client feature registrations.

## Before You Begin

- Read [Reservoir Getting Started](../../reservoir/getting-started/getting-started.md).
- Read [Inlet Getting Started](../getting-started/getting-started.md).
- Confirm that your application already has an `HttpClient` registration if you plan to use the automatic projection fetcher.

## Steps

1. Create the top-level Mississippi client builder.

This step establishes the public client root for a full Mississippi app.

```csharp
builder.AddMississippiClient(client =>
{
    // Additional composition steps go here.
});
```

1. Add generated domain composition and hand-written Reservoir features.

This example uses the verified Spring client composition pattern.

```csharp
client.AddMississippiSamplesSpringDomainClient();
client.Reservoir(reservoir =>
{
    reservoir.AddDualEntitySelectionFeature();
    reservoir.AddDemoAccountsFeature();
    reservoir.AddAuthSimulationFeature();
    reservoir.AddReservoirBlazorBuiltIns();
    reservoir.AddReservoirDevTools(options =>
    {
        options.Enablement = ReservoirDevToolsEnablement.Always;
        options.Name = "Spring Sample";
        options.IsStrictStateRehydrationEnabled = true;
    });
});
```

1. Add the Inlet client core inside `Reservoir(...)`.

```csharp
client.Reservoir(reservoir =>
{
    reservoir.AddInletClient();
});
```

This registers the projection registry, `ProjectionsFeatureState`, `IInletStore`, and `IProjectionUpdateNotifier`.

1. Configure projection-path composition.

Choose one of these patterns based on what your client code already has:

- use generated projection registrations such as `AddProjectionsFeature()` when the client generator produced them
- add explicit projection paths with `AddProjectionPath<T>(path)` when you need a manual mapping
- use `ScanProjectionDtos(...)` inside the SignalR builder when you want automatic DTO discovery for fetch operations

1. Add SignalR projection refresh support.

This example uses the automatic fetcher path verified in `InletBlazorSignalRBuilder` and the Spring sample.

```csharp
client.Reservoir(reservoir =>
{
    reservoir.AddInletBlazorSignalR(signalR => signalR
        .WithHubPath("/hubs/inlet")
        .ScanProjectionDtos(typeof(BankAccountBalanceProjectionDto).Assembly));
});
```

The SignalR builder also supports:

- `AddProjectionFetcher<TFetcher>()` for a custom fetcher
- `WithRoutePrefix(prefix)` to change the HTTP projection route prefix

1. Build and run the host.

```csharp
await builder.Build().RunAsync();
```

## Full Spring Client Example

This excerpt matches the current Spring sample startup shape.

```csharp
builder.AddMississippiClient(client =>
{
    client.AddMississippiSamplesSpringDomainClient();
    client.Reservoir(reservoir =>
    {
        reservoir.AddDualEntitySelectionFeature();
        reservoir.AddDemoAccountsFeature();
        reservoir.AddAuthSimulationFeature();
        reservoir.AddReservoirBlazorBuiltIns();
        reservoir.AddReservoirDevTools(options =>
        {
            options.Enablement = ReservoirDevToolsEnablement.Always;
            options.Name = "Spring Sample";
            options.IsStrictStateRehydrationEnabled = true;
        });

        reservoir.AddInletClient();
        reservoir.AddInletBlazorSignalR(signalR => signalR
            .WithHubPath("/hubs/inlet")
            .ScanProjectionDtos(typeof(BankAccountBalanceProjectionDto).Assembly));
    });
});
```

Source code: [Spring.Client/Program.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Client/Program.cs)

## Verify The Result

- Full Mississippi client startup should begin with `AddMississippiClient(...)`.
- Reservoir registrations should all hang off the same `IReservoirBuilder` value inside `client.Reservoir(...)`.
- Inlet client registrations should extend that Reservoir builder instead of calling unrelated `IServiceCollection` helpers.
- SignalR configuration should be expressed inside `AddInletBlazorSignalR(...)`.
- Generated domain registration methods should read like `Add{Domain}Client()` on `MississippiClientBuilder`.
- Generated feature registration methods should still read like `AddProjectionsFeature()` or `Add{Aggregate}AggregateFeature()` on `IReservoirBuilder`.

## Source Code

- [InletClientRegistrations.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletClientRegistrations.cs)
- [InletBlazorRegistrations.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletBlazorRegistrations.cs)
- [InletBlazorSignalRBuilder.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletBlazorSignalRBuilder.cs)
- [SignalRConnectionRegistrations.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/SignalRConnection/SignalRConnectionRegistrations.cs)
- [CommandClientRegistrationGenerator.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Generators/CommandClientRegistrationGenerator.cs)
- [ProjectionClientRegistrationGenerator.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Generators/ProjectionClientRegistrationGenerator.cs)
- [SagaClientRegistrationGenerator.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Generators/SagaClientRegistrationGenerator.cs)
- [DomainClientRegistrationGenerator.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Generators/DomainClientRegistrationGenerator.cs)

## Summary

Compose Inlet in full Mississippi client apps by starting with `AddMississippiClient()`, then layering Reservoir and Inlet registrations inside `client.Reservoir(...)`. Stay with `AddReservoir()` only when the app is intentionally Reservoir-only.

## Next Steps

- Use [Inlet Reference](../reference/reference.md) for the exact method surface.
- Use [Read Models and Client Sync](../../concepts/read-models-and-client-sync.md) for the end-to-end projection delivery model.
- Use [Spring Host Architecture](../../samples/spring-sample/concepts/host-applications.md) to see the client composition in the sample application.
