---
title: Aqueduct Message Delivery
description: Reference local and remote connection sends, broadcast recipient selection, and local write boundaries.
sidebar_position: 4
---

# Aqueduct Message Delivery

`AqueductHubLifetimeManager<THub>` chooses a local send or the client-grain route for a single connection. Incoming backplane messages are then delivered to connections present in the receiving gateway's local registry.

## Applies To

- `Mississippi.Aqueduct.Gateway.AqueductHubLifetimeManager<THub>`
- `ServerMessage`, `AllMessage`, and `LocalMessageSender`
- Single-connection sends and hub-wide broadcasts

## Connection And Broadcast Sends

The [lifetime manager](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs) derives its hub name from `typeof(THub).Name`. `SendConnectionAsync` requires non-null, nonempty connection ID and method name, then checks the local registry:

- A present connection is sent to through `ILocalMessageSender`.
- Otherwise, the manager resolves the hub's client grain for that ID and calls `SendMessageAsync`.

Null argument arrays become empty arguments. The method's cancellation token is not passed to the local sender or client-grain send, whose called APIs have no token parameter.

`SendAllAsync` and `SendAllExceptAsync` first ensure the shared backplane is initialized, then publish an `AllMessage`. The token is passed to initialization; `PublishToAllAsync` has no token parameter. Except sends include the supplied exclusion list in the message.

## Incoming Recipient Selection

[`ServerMessage`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Abstractions/Messages/ServerMessage.cs) contains one connection ID, a method name, and arguments. Its receiver looks up that ID locally and sends only when it is present. A missing local connection completes without another forwarding attempt.

[`AllMessage`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Abstractions/Messages/AllMessage.cs) contains a method name, arguments, and optional excluded connection IDs. Its receiver enumerates local connections and skips those with an aborted connection token or an ID in the exclusion list.

Broadcast writes are awaited sequentially. The receiver does not catch a send failure and continue to later recipients. Neither receiver returns a client acknowledgment or a count of clients that executed the invocation.

The message records default to empty method names and arguments; `ServerMessage` also defaults to an empty connection ID, while broadcast exclusions default to null. These records do not validate their independently initialized fields.

## Local Write Boundary

The [local sender](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Gateway/LocalMessageSender.cs) rejects a null connection and null or empty method name. Whitespace-only method names are not rejected by that guard.

It creates a SignalR `InvocationMessage`, reusing an argument array or converting another list to an array, then awaits `connection.WriteAsync` with `CancellationToken.None`. Its completion reports the connection write, rather than execution of a client handler. The sender adds no retry loop.

The [lifetime-manager tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Aqueduct.Gateway.L0Tests/AqueductHubLifetimeManagerTests.cs) assert the local/client-grain branches. The [local-sender tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Aqueduct.Gateway.L0Tests/LocalMessageSenderTests.cs) cover guards and sends with empty, list, and array arguments. Recipient selection above is verified from the callback implementation.

## Summary

Delivery selects connections available on the receiving gateway and awaits their writes. Treat that completion separately from durable delivery or client execution, and account for missing connections and broadcast write failures.

## Next Steps

- Read [Aqueduct Reference](./reference.md) for composition and stream options.
- Read [Aqueduct Concepts](../concepts/concepts.md) for the wider backplane flow.
