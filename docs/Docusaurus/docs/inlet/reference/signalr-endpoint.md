---
title: SignalR Endpoint Configuration
description: Reference the Inlet hub path, client URI resolution, and separate HTTP projection route.
sidebar_position: 5
---

# SignalR Endpoint Configuration

Inlet configures the client hub address and gateway hub mapping separately. Both default to `/hubs/inlet`; changing one side does not rewrite the other.

## Applies To

- `Mississippi.Inlet.Client.InletBlazorSignalRBuilder`
- `Mississippi.Inlet.Client.ActionEffects.InletSignalRActionEffectOptions`
- `Mississippi.Inlet.Gateway.InletServerRegistrations.MapInletHub`

## Client Hub Path

[`InletSignalRActionEffectOptions.HubPath`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/InletSignalRActionEffectOptions.cs) defaults to `/hubs/inlet`. The [builder's](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletBlazorSignalRBuilder.cs) `WithHubPath(string)` replaces its options object with the supplied path and returns the builder.

That method rejects null, empty, or whitespace-only values. Other values are stored unchanged: it does not trim the string, check that an endpoint exists, or validate a deployment's routing arrangement.

Configure the builder inside `AddInletBlazorSignalR`'s callback. Configuration closes when registration begins or the callback exits, including a failed callback. Subsequent configuration and configuration with a read-only parent service collection throw `InvalidOperationException`.

Build uses `TryAddSingleton` for the options and `TryAddScoped` for `IHubConnectionProvider`. Existing registrations are preserved; registering options earlier can therefore determine the object resolved by the provider.

## URI Resolution

The [built-in provider](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/HubConnectionProvider.cs) passes `NavigationManager.ToAbsoluteUri(HubPath)` to SignalR's `WithUrl` during construction.

`HttpClient.BaseAddress` is not the hub-address source in that implementation. A client that fetches projections from one HTTP origin does not automatically connect its hub to that origin. URI resolution failures occur when the provider constructs the connection, rather than through an endpoint-existence check in `WithHubPath`.

A custom `IHubConnectionProvider` owns its own connection construction; these address rules describe the built-in provider.

## Gateway Mapping And Projection HTTP Routes

[`MapInletHub(pattern)`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Gateway/InletServerRegistrations.cs) maps the gateway's `InletHub` with default pattern `/hubs/inlet`. Coordinate that mapped endpoint with the client address and any host routing configuration; the client builder does not change gateway routes.

`WithRoutePrefix` is a separate setting for the [automatic projection fetcher's](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs) HTTP URLs. That fetcher defaults to `/api/projections`. Changing its prefix does not change `HubPath`.

The [builder tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Client.L0Tests/Registrations/InletBlazorSignalRBuilderTests.cs) cover path guards, chaining, registration lifetime, and configuration closure. They do not prove that a configured deployment endpoint is reachable.

## Summary

Set the hub path through the client callback, check the built-in navigation-based URI source, and coordinate gateway mapping separately. Projection HTTP routing is another configuration surface.

## Next Steps

- Read [Inlet Reference](./reference.md) for the complete client builder surface.
- Read [Spring Host Applications](../../samples/spring-sample/concepts/host-applications.md) for sample client and gateway setup.
