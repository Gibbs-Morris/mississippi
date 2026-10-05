---
id: message-delivery
title: Aqueduct Message Delivery
description: Reference local and remote connection sends, broadcast recipient selection, and local write boundaries.
sidebar_position: 4
sidebar_label: Aqueduct Message Delivery
---

# Aqueduct Message Delivery

## Overview

`AqueductHubLifetimeManager<THub>` chooses a local send or the client-grain route for a single connection. Incoming backplane messages are then delivered to connections present in the receiving gateway's local registry.

## Applies To

- `Mississippi.Aqueduct.Gateway.AqueductHubLifetimeManager<THub>`
- `ServerMessage`, `AllMessage`, and `LocalMessageSender`
- Single-connection sends and hub-wide broadcasts

## Connection And Broadcast Sends

The [lifetime manager](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs) derives its hub name from `typeof(THub).Name`. `SendConnectionAsync` requires non-null, nonempty connection ID and method name, then checks the local registry:

- A present connection is sent to through `ILocalMessageSender`.
- Otherwise, the manager resolves the hub's client grain for that ID and calls `SendMessageAsync`. This remote route constructs a [client routing key](./routing-keys.md): the connection ID cannot contain `:`, and the combined key is limited to 4192 UTF-16 code units. Those key violations throw `ArgumentException`; the local branch does not construct that key.

The [client grain](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Runtime/Grains/SignalRClientGrain.cs) returns successfully without publication when its stored server ID is empty, including an ID that was never connected or has disconnected. Completion of the remote send therefore does not establish that a message was published.

Null argument arrays become empty arguments. The method's cancellation token is not passed to the local sender or client-grain send, whose called APIs have no token parameter.

`SendAllAsync` and `SendAllExceptAsync` first ensure the shared backplane is initialized, then publish an `AllMessage`. The token controls initialization semaphore waits, but is not forwarded to stream subscriptions or directory registration. When initialization is already complete, the setup path returns without checking it. `PublishToAllAsync` has no token parameter, so a canceled token does not itself stop an initialized broadcast publication. Except sends include the supplied exclusion list in the message. Initial setup also awaits `HeartbeatManager.StartAsync` and its directory registration before publication. A pending registration delays the broadcast; a failed registration faults it without publishing. Later setup calls retry registration, as covered by the [startup tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Aqueduct.Gateway.L0Tests/AqueductHubStartupTests.cs). An initialized stream alone does not establish broadcast readiness.

Client-grain identity includes the short CLR hub name and connection ID within an Orleans cluster, without a stream namespace. Different configured provider/namespaces can separate hub broadcast streams, but cannot distinguish same-name hub client-grain keys. Use distinct short hub names when those client routes must be isolated.

Participating runtimes and gateways must use matching `StreamProviderName` and `ServerStreamNamespace`: the client grain publishes with runtime options while the gateway subscribes with gateway options. Gateways sharing broadcasts must also match `AllClientsStreamNamespace`. Successfully resolving different providers/namespaces can publish to a stream without the intended subscriber; completion is not evidence that the identities match.

The [default registrations](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Gateway/AqueductRegistrations.cs) supply one singleton connection registry and stream-subscription manager per DI container. The [subscription manager](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Gateway/StreamSubscriptionManager.cs) keeps the first initialized hub's broadcast stream and callbacks. Registering several hub types in that container does not isolate their broadcasts: later hubs can publish on the first stream, and callbacks can enumerate other hubs' connections in the shared registry. Use one Aqueduct hub per container with this default composition. The registry is keyed only by connection ID, without a hub name. Direct `SendConnectionAsync` local lookup and incoming `ServerMessage` lookup can therefore find another hub's connection too; the one-hub composition boundary applies to targeted sends as well as broadcasts. Initialization awaits the server-stream subscription before the all-message subscription and retains neither subscription handle. If the first succeeds and the second fails, the initialized flag remains false but the first subscription is not removed. A later setup retry can add another server-stream callback, repeating a local write independently of provider retry/replay.

## Incoming Recipient Selection

[`ServerMessage`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Abstractions/Messages/ServerMessage.cs) contains one connection ID, a method name, and arguments. Its receiver looks up that ID locally and sends only when it is present. A missing local connection completes without another forwarding attempt.

[`AllMessage`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Abstractions/Messages/AllMessage.cs) contains a method name, arguments, and optional excluded connection IDs. Its receiver enumerates local connections and skips those with an aborted connection token or an ID in the exclusion list.

The receiving callback awaits broadcast writes sequentially, but [the registry](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Gateway/ConnectionRegistry.cs) enumerates `ConcurrentDictionary.Values` in unspecified order. The receiver does not catch a send failure and continue: which recipients were written before a failure cannot be predicted from insertion order. Neither receiver returns a client acknowledgment or a count of clients that executed the invocation. Each stream delivery repeats recipient selection and sends; the messages carry no deduplication ID and the receivers do not suppress repeats. Provider retry/replay can therefore repeat a client invocation. Application handlers need their own idempotency if repeated delivery matters.

The message records default to empty method names and arguments; `ServerMessage` also defaults to an empty connection ID, while broadcast exclusions default to null. These records do not validate their independently initialized fields. Receiving a default `ServerMessage` faults with `ArgumentException` at the registry's empty-connection-ID guard, before the missing-connection no-op. A default `AllMessage` reaches the local sender's empty-method-name guard and faults with `ArgumentException` on the first eligible connection; with no eligible connection it completes without a write. These receiver exceptions propagate through their callbacks.

## Local Write Boundary

The [local sender](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Gateway/LocalMessageSender.cs) rejects a null connection and null or empty method name. Whitespace-only method names are not rejected by that guard.

It creates a SignalR `InvocationMessage`, reusing an argument array or converting another list to an array, then awaits `connection.WriteAsync` with `CancellationToken.None`. Its completion reports the connection write, rather than execution of a client handler. The sender adds no retry loop.

The [lifetime-manager tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Aqueduct.Gateway.L0Tests/AqueductHubLifetimeManagerTests.cs) assert the local/client-grain branches. The [local-sender tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Aqueduct.Gateway.L0Tests/LocalMessageSenderTests.cs) cover guards and sends with empty, list, and array arguments. Recipient selection above is verified from the callback implementation.

The current [client-grain L2 suite](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Aqueduct.Gateway.L2Tests/ClientGrainStreamingTests.cs) explicitly omits streaming delivery tests and records serialization problems with `ImmutableArray`/collection-expression arguments in `object?[]` payloads. Its connection tests do not validate remote argument delivery. This page describes the configured route, not verified support for every cross-Orleans payload; validate the intended payload shape before relying on it.

## Summary

The receiving callback selects local connections and awaits its writes. Broadcast senders directly await the stream provider's publication task, whose completion guarantees depend on the configured provider; there is no separate Aqueduct acknowledgment of every gateway's writes. Neither boundary establishes durable delivery or client execution.

## Next Steps

- Read [Aqueduct Reference](./reference.md) for composition and stream options.
- Read [Aqueduct Concepts](../concepts/concepts.md) for the wider backplane flow.
