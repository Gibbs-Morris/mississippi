---
id: connection-state
title: SignalR Connection State
description: Reference SignalR lifecycle state, retained fields, reducer transitions, and selector meanings.
sidebar_position: 6
sidebar_label: SignalR Connection State
---

# SignalR Connection State

## Overview

`SignalRConnectionState` records lifecycle observations dispatched into Reservoir. Its selectors read those recorded values; they do not query the transport or verify projection freshness.

## Applies To

- `Mississippi.Inlet.Client.SignalRConnection.SignalRConnectionState`
- `SignalRConnectionSelectors` and the registered lifecycle reducers
- The built-in hub provider's lifecycle actions

## Defaults And Feature Identity

The [record](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/SignalRConnection/SignalRConnectionState.cs) uses feature key `signalr-connection`. Its initial status is `Disconnected`, reconnect count is zero, and connection ID, error, and all timestamps are null.

The separate `InletConnectionState` uses key `inlet-connection` and has no lifecycle fields. It supplies the action effect's feature compartment; use `SignalRConnectionState` for the status data described here.

[`AddSignalRConnectionFeature`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/SignalRConnection/SignalRConnectionRegistrations.cs) registers this state and its lifecycle reducers. `AddInletBlazorSignalR` also adds that feature during build.

## State Transitions

The [reducers](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/SignalRConnection/SignalRConnectionReducers.cs) copy state and replace the fields selected by each action:

- Connecting sets status to `Connecting` and clears `LastError`, preserving the previous ID, timestamps, and count.
- Connected or reconnected sets status to `Connected`, replaces the ID and `LastConnectedAt`, resets the count to zero, and clears the error. The action permits a null ID, so `Connected` does not guarantee a non-null `ConnectionId`; use status for the recorded connection check.
- Reconnecting sets status to `Reconnecting`, assigns the action's attempt number and error, and preserves the ID and timestamps.
- Disconnected sets status to `Disconnected`, replaces `LastDisconnectedAt` and the error, and clears the ID. It does not reset the state's reconnect count.
- Message received replaces only `LastMessageReceivedAt`. The built-in [projection-update callback](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs) emits this action before DTO/subscription checks. SignalR keep-alives and other hub traffic do not emit it in this path, so the timestamp is not a general transport-health signal.

Fields not listed for a transition retain their prior values. Timestamps come from action payloads; the record does not maintain a timer or expire them.

The [built-in provider](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/HubConnectionProvider.cs) maintains its own reconnect counter for emitted actions. Resetting that private counter on closure does not reset the stored state's count through the disconnected reducer.

SignalR fires `Reconnecting` once before its [automatic retry sequence](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client#automatically-reconnect). The built-in provider therefore emits count `1` for that sequence, not one increment for each retry. It remains `1` in state after `Closed` until a later action changes it; connected/reconnected actions reset it to zero.

A failed or canceled `StartAsync` after the connecting action emits neither connected nor disconnected from that startup method. State can remain `Connecting` with `LastError` cleared after the attempt ends. See [Connection Startup](./connection-startup.md) for direct-call and action-flow failure handling.

Built-in provider disposal removes its lifecycle callbacks before disposing the transport and dispatches no disconnected action. A previously recorded `Connected` status and connection ID can therefore remain after disposal; `SignalRConnectionSelectors.IsConnected` still reads that recorded status, not the disposed transport.

## Selector Meanings

The [selectors](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/SignalRConnection/SignalRConnectionSelectors.cs) all reject null state with `ArgumentNullException`:

- `IsConnected` checks exactly `Connected`.
- `IsReconnecting` checks exactly `Reconnecting`.
- `IsDisconnected` checks any status other than `Connected`, including `Connecting` and `Reconnecting`.
- ID, timestamp, error, count, and status getters return the corresponding recorded field unchanged.

`GetConnectionId` can therefore return a retained ID during reconnection. A recent message timestamp or a connected status alone does not confirm that a particular projection has loaded or caught up.

The [state tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Client.L0Tests/SignalRConnectionStateTests.cs) cover defaults and field assignment. The [reducer tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Client.L0Tests/SignalRConnectionReducersTests.cs) cover lifecycle statuses, ID/error updates, timestamps, and reconnect count assignment/reset.

## Summary

Read lifecycle status alongside its retained fields. In particular, `IsDisconnected` means not connected, and historical ID, count, or timestamps can outlive the connection that produced them.

## Next Steps

- Read [Inlet Reference](./reference.md) for registration surfaces.
- Read [Reservoir State Flow](../../reservoir/concepts/state-flow.md) for action-driven state updates.
