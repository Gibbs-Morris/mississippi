---
id: signalr-endpoint
title: SignalR Endpoint Configuration
description: Reference the Inlet hub path, client URI resolution, and separate HTTP projection route.
sidebar_position: 5
sidebar_label: SignalR Endpoint Configuration
---

# SignalR Endpoint Configuration

## Overview

Inlet configures the client hub address and gateway hub mapping separately. Both default to `/hubs/inlet`; changing one side does not rewrite the other.

## Applies To

- `Mississippi.Inlet.Client.InletBlazorSignalRBuilder`
- `Mississippi.Inlet.Client.ActionEffects.InletSignalRActionEffectOptions`
- `Mississippi.Inlet.Gateway.InletServerRegistrations.MapInletHub`

## Client Hub Path

[`InletSignalRActionEffectOptions.HubPath`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/InletSignalRActionEffectOptions.cs) defaults to `/hubs/inlet`. The [builder's](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletBlazorSignalRBuilder.cs) `WithHubPath(string)` replaces its options object with the supplied path and returns the builder.

That method rejects null, empty, or whitespace-only values. Other values are stored unchanged: it does not trim the string, check that an endpoint exists, or validate a deployment's routing arrangement.

Configure the builder inside `AddInletBlazorSignalR`'s callback, while configuration remains open. `Build()` closes it after its initial guard and before registering services. The enclosing `finally` also closes it when the callback/build path exits, including failure. Subsequent configuration and configuration with a read-only parent service collection throw `InvalidOperationException`.

Build uses `TryAddSingleton` for the options and `TryAddScoped` for `IHubConnectionProvider`. Existing registrations are preserved; registering options earlier can therefore determine the object resolved by the provider.

## URI Resolution

The [built-in provider](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/HubConnectionProvider.cs) passes `NavigationManager.ToAbsoluteUri(HubPath)` to SignalR's `WithUrl` during construction. Absolute URLs keep their own origin; `/hubs/inlet` resolves at the origin root, while `hubs/inlet` resolves beneath the navigation base URI. With a base of `https://example.test/app/`, those relative forms target `/hubs/inlet` and `/app/hubs/inlet` respectively. See [`ToAbsoluteUri`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.navigationmanager.toabsoluteuri?view=aspnetcore-10.0).

`HttpClient.BaseAddress` is not the hub-address source in that implementation. A client that fetches projections from one HTTP origin does not automatically connect its hub to that origin. URI resolution failures occur when the provider constructs the connection, rather than through an endpoint-existence check in `WithHubPath`.

The built-in connection uses `WithUrl(uri)` without an `AccessTokenProvider`; projection `HttpClient` authentication does not configure this separate connection. For a bearer-protected hub, register a custom `IHubConnectionProvider` before the builder supplies its default and configure [SignalR bearer authentication](https://learn.microsoft.com/en-us/aspnet/core/signalr/configuration#configure-bearer-authentication) in the `WithUrl` options delegate. A compatible ambient-cookie setup is a separate host choice. A custom provider owns its own connection construction; the address rules above describe the built-in provider.

## Gateway Mapping And Projection HTTP Routes

[`MapInletHub(pattern)`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Gateway/InletServerRegistrations.cs) maps the gateway's `InletHub` with default pattern `/hubs/inlet`. Coordinate that mapped endpoint with the client address and any host routing configuration; the client builder does not change gateway routes. Mapping does not register its dependencies: call `AddInletServer` for Inlet/SignalR services, configure an Orleans client, and register `AddAqueduct<InletHub>` with the matching backplane provider. The [Spring gateway composition](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Gateway/Program.cs) shows these registrations. Missing SignalR services can fail mapping; missing runtime/backplane dependencies can fail hub activation.

When `GeneratedApiAuthorization.Mode` is `RequireAuthorizationForAllGeneratedEndpoints` and `AllowAnonymousOptOut` is false, `MapInletHub` also requires authorization with the configured default policy, roles, and authentication schemes. If those defaults are blank, it uses the host's default authorization policy. Other combinations do not add this hub endpoint requirement through this mapper; subscription authorization is evaluated separately.

`WithRoutePrefix` is a separate setting for the [automatic projection fetcher's](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs) HTTP URLs. That fetcher defaults to `/api/projections`. Changing its prefix does not change `HubPath`. The prefix is stored unchanged and joined directly with `/{path}/{entityId}`; a trailing slash creates a double slash. An absolute URL prefix supplies its own origin and bypasses `HttpClient.BaseAddress`. Both other forms need a configured base address: `/api/projections` uses its authority and replaces its path, while `api/projections` resolves beneath its base directory. For example, a base of `https://example.test/app/` produces `/api/projections/...` and `/app/api/projections/...` respectively. Match that resolved route to the host and omit a trailing slash to avoid the doubled separator.

The [builder tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Client.L0Tests/Registrations/InletBlazorSignalRBuilderTests.cs) cover path guards, chaining, registration lifetime, and configuration closure. They do not prove that a configured deployment endpoint is reachable.

## Summary

Set the hub path through the client callback, check the built-in navigation-based URI source, and coordinate gateway mapping separately. Projection HTTP routing is another configuration surface.

## Next Steps

- Read [Inlet Reference](./reference.md) for the complete client builder surface.
- Read [Spring Host Applications](../../samples/spring-sample/concepts/host-applications.md) for sample client and gateway setup.
