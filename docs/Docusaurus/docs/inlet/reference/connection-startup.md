---
title: SignalR Connection Startup
description: Reference the active Inlet connection request, transport startup, and failure boundaries.
sidebar_position: 3
---

# SignalR Connection Startup

Inlet's client action effect starts its SignalR connection when work requires it. A `RequestSignalRConnectionAction` can request that startup before a projection subscription is needed.

## Applies To

- `Mississippi.Inlet.Client.SignalRConnection.RequestSignalRConnectionAction`
- `Mississippi.Inlet.Client.ActionEffects.IHubConnectionProvider`
- The built-in `InletSignalRActionEffect` and `HubConnectionProvider`

## Request And Registration

[`AddInletBlazorSignalR`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletBlazorSignalRBuilder.cs) registers the provider as scoped and attaches the action effect to `InletConnectionState`. It also registers the separate `SignalRConnectionState` lifecycle feature.

[`RequestSignalRConnectionAction`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/SignalRConnection/RequestSignalRConnectionAction.cs) is a payload-free Reservoir action. The [effect](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs) handles it by awaiting `EnsureConnectedAsync` and then ending its action stream.

The effect also calls `EnsureConnectedAsync` before processing supported subscribe, unsubscribe, and refresh projection actions. Provider construction alone does not start the transport.

## EnsureConnectedAsync

The [built-in provider](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/HubConnectionProvider.cs) checks the underlying `HubConnection.State`:

- When it is `Disconnected`, dispatch `SignalRConnectingAction`, await `StartAsync` with the supplied token, then dispatch `SignalRConnectedAction` with the connection ID and current provider time.
- In other states, return without starting another connection or waiting for an in-progress start/reconnect to become connected.

The method does not independently throw for a canceled token on the no-start path. The token is forwarded only when `StartAsync` is called.

There is no shared readiness task or start lock in this implementation. A returning call made while the transport is connecting or reconnecting is therefore not a readiness confirmation for subsequent hub work.

## Failures And Reconnection

An exception from `StartAsync` propagates. The provider does not dispatch `SignalRConnectedAction` after that failure, and this startup method has no catch that emits a failure action or retries the initial call.

Connection construction enables SignalR's parameterless `WithAutomaticReconnect`. Lifecycle callbacks dispatch reconnecting, reconnected, and disconnected actions. Those callbacks are separate from an application's explicit startup request; the declared `InletOptions` reconnect properties are not read by this provider.

Disposal removes the provider's own lifecycle callbacks and disposes the underlying connection. It does not alter the action effect's feature-state type into the lifecycle status record.

The [builder tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Client.L0Tests/Registrations/InletBlazorSignalRBuilderTests.cs) cover scoped provider and lazy store registrations. They inspect service descriptors rather than establishing transport readiness or concurrent startup guarantees.

## Summary

Request startup through the action flow and distinguish a completed transport start from a call that returned during another connection state. Startup exceptions and reconnect lifecycle events have separate observation paths.

## Next Steps

- Read [Inlet Reference](./reference.md) for client builder setup.
- Read [Client Composition](../../reference/client-composition.md) for host-owned service registration.
