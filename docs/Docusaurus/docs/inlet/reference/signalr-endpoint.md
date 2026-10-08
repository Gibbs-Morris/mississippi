---
id: signalr-endpoint
title: SignalR Endpoint Configuration
description: Reference the Inlet hub path, client URI resolution, and separate HTTP projection route.
sidebar_position: 5
sidebar_label: SignalR Endpoint Configuration
---

# SignalR Endpoint Configuration

## Overview

Inlet configures the client hub address and gateway hub mapping separately. The client defaults to `hubs/inlet`, relative to its navigation base, and the gateway maps `/hubs/inlet` within its hosting path. Changing one side does not rewrite the other.

## Applies To

- `Mississippi.Inlet.Client.InletBlazorSignalRBuilder`
- `Mississippi.Inlet.Client.ActionEffects.InletSignalRActionEffectOptions`
- `Mississippi.Inlet.Gateway.InletServerRegistrations.MapInletHub`

## Client Hub Path

[`InletSignalRActionEffectOptions.HubPath`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/InletSignalRActionEffectOptions.cs) defaults to `hubs/inlet`, relative to the application navigation base. The [builder's](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletBlazorSignalRBuilder.cs) `WithHubPath(string)` replaces its options object with the supplied path and returns the builder.

That method rejects null, empty, or whitespace-only values. Other values are stored unchanged: it does not trim the string, check that an endpoint exists, or validate a deployment's routing arrangement.

Configure the builder inside `AddInletBlazorSignalR`'s callback, while configuration remains open. `Build()` closes it after its initial guard and before registering services. The enclosing `finally` also closes it when the callback/build path exits, including failure. Otherwise-valid subsequent configuration and configuration with a read-only parent service collection throw `InvalidOperationException`. Argument validation runs first: an invalid path or null scan-assembly array can still throw its argument exception after configuration closes.

Build uses `TryAddSingleton` for the options and `TryAddScoped` for `IHubConnectionProvider`. Existing registrations are preserved; registering options earlier can therefore determine the object resolved by the provider. The action effect also requires `IProjectionFetcher`. Configure `ScanProjectionDtos(...)` for the automatic fetcher and populated `IProjectionDtoRegistry`, or supply a custom fetcher and equivalent DTO-path registrations. `WithHubPath` alone does not supply that dependency; a missing fetcher can fail effect resolution before connection startup.

## URI Resolution

The [built-in provider](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/HubConnectionProvider.cs) passes `NavigationManager.ToAbsoluteUri(HubPath)` to SignalR's `WithUrl` during construction. Absolute URLs keep their own origin; `/hubs/inlet` resolves at the origin root, while `hubs/inlet` resolves beneath the navigation base URI. With a base of `https://example.test/app/`, those relative forms target `/hubs/inlet` and `/app/hubs/inlet` respectively. See [`ToAbsoluteUri`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.navigationmanager.toabsoluteuri?view=aspnetcore-10.0).

`HttpClient.BaseAddress` is not the hub-address source in that implementation. A client that fetches projections from one HTTP origin does not automatically connect its hub to that origin. URI resolution failures occur when the provider constructs the connection, rather than through an endpoint-existence check in `WithHubPath`.

For a browser-hosted client connecting to another origin, configure and apply a gateway CORS policy allowing the intended client origin and SignalR HTTP methods, headers, and needed credentials. URI resolution alone does not permit cross-origin negotiation or HTTP transports. Follow the [SignalR CORS guidance](https://learn.microsoft.com/en-us/aspnet/core/signalr/security?view=aspnetcore-10.0#cross-origin-resource-sharing), including its separate WebSocket origin boundary.

The built-in connection uses `WithUrl(uri)` without an `AccessTokenProvider`; projection `HttpClient` authentication does not configure this separate connection. For a bearer-protected hub or projection subscription, register a custom `IHubConnectionProvider` before the builder supplies its default and configure [SignalR bearer authentication](https://learn.microsoft.com/en-us/aspnet/core/signalr/configuration#configure-bearer-authentication) in the `WithUrl` options delegate. Browser WebSocket and Server-Sent Events transports send that bearer token as `access_token` in the query string. The gateway's JWT bearer handler must read it in `OnMessageReceived` and restrict that extraction to the actual mapped hub path, or an equivalent terminating proxy must handle it. Otherwise a client token provider can still leave those transports unauthenticated. See the [SignalR server authentication guidance](https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-10.0#bearer-token-authentication). A compatible ambient-cookie setup is a separate host choice. A custom provider owns its own connection construction; the address rules above describe the built-in provider. An open hub endpoint can still reject a protected projection subscription: the [hub](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Gateway/InletHub.cs) checks projection authorization attributes before its disabled-mode shortcut.

## Gateway Mapping And Projection HTTP Routes

[`MapInletHub(pattern)`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Gateway/InletServerRegistrations.cs) maps the gateway's `InletHub` with default pattern `/hubs/inlet`. Coordinate that mapped endpoint with the client address and any host routing configuration; the client builder does not change gateway routes. Mapping does not register its dependencies: call `AddInletServer` for Inlet/SignalR services, configure an Orleans client, and register `AddAqueduct<InletHub>` with the matching backplane provider. The [Spring gateway composition](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Gateway/Program.cs) shows these registrations. Missing SignalR services can fail mapping; missing runtime/backplane dependencies can fail hub activation. `InletHub` always requires `IAuthorizationService` and `IAuthorizationPolicyProvider` as constructor dependencies. For a protected hub endpoint or projection subscription, configure the host's authentication schemes, authorization policies, and ASP.NET Core authentication/authorization request pipeline so `Context.User` contains the intended identity. These Inlet helpers do not supply an authentication scheme or named policy. An open endpoint does not remove the authentication prerequisites for a protected subscription.

When `GeneratedApiAuthorization.Mode` is `RequireAuthorizationForAllGeneratedEndpoints` and `AllowAnonymousOptOut` is false, `MapInletHub` also requires authorization with the configured default policy, roles, and authentication schemes. If those defaults are blank, it uses the host's default authorization policy. Other combinations do not add this hub endpoint requirement through this mapper; subscription authorization is evaluated separately. The gateway must populate `IProjectionAuthorizationRegistry` through [`ScanProjectionAssemblies(...)`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Runtime/InletSiloRegistrations.cs), or equivalent registrations, using the attributed projection assemblies. `AddInletServer` supplies an initially empty registry; it does not discover those attributes. With missing metadata and the default disabled generated-authorization mode, subscription checks skip projection authorization. A separate runtime silo must also call `AddInletSilo` and `ScanProjectionAssemblies(...)`, or supply an equivalent populated `IProjectionBrookRegistry` for those paths. Scanning in the gateway does not populate the silo's DI container. Each `ScanProjectionAssemblies` call creates new registries and replaces earlier brook and authorization registrations. Combine all intended assemblies in one call in each host, or supply explicitly accumulated registries. Repeated separate scans can lose earlier routes and security metadata; in disabled generated-authorization mode, missing metadata skips that projection's authorization checks. Its subscription grain throws `InvalidOperationException` for an unregistered projection path even when the hub connection succeeded.

`WithRoutePrefix` is a separate setting for the [automatic projection fetcher's](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs) HTTP URLs. That fetcher defaults to `api/projections`, relative to `HttpClient.BaseAddress`. Changing its prefix does not change `HubPath`. `WithRoutePrefix` rejects null with `ArgumentNullException`, and empty or whitespace-only values with `ArgumentException`. The prefix is stored unchanged and joined directly with `/{path}/{escapedEntityId}` for latest fetches; `escapedEntityId` is `Uri.EscapeDataString(entityId)`. Versioned fetches append `/at/{version}` to that URL. A trailing slash in the prefix creates a double slash. An absolute URL prefix supplies its own origin and bypasses `HttpClient.BaseAddress`. Both other forms need a configured base address: `/api/projections` uses its authority and replaces its path, while `api/projections` resolves beneath its base directory. For example, a base of `https://example.test/app/` produces `/api/projections/...` and `/app/api/projections/...` respectively. Match that resolved route to the host and omit a trailing slash to avoid the doubled separator.

For browser projection fetches to another origin, apply a CORS policy to the projection HTTP endpoint that permits the client origin and required request headers. Expose the response `ETag`, for example with `WithExposedHeaders("ETag")`; the browser otherwise hides that non-safelisted header and the automatic fetcher reports version `0`. This HTTP requirement is separate from hub CORS. See [ASP.NET Core exposed response headers](https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-10.0#set-the-exposed-response-headers).

The [builder tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Client.L0Tests/Registrations/InletBlazorSignalRBuilderTests.cs) cover path guards, chaining, registration lifetime, and configuration closure. They do not prove that a configured deployment endpoint is reachable.

## Summary

Set the hub path through the client callback, check the built-in navigation-based URI source, and coordinate gateway mapping separately. Projection HTTP routing is another configuration surface.

## Next Steps

- Read [Inlet Reference](./reference.md) for the complete client builder surface.
- Read [Spring Host Applications](../../samples/spring-sample/concepts/host-applications.md) for sample client and gateway setup.
