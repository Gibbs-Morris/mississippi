---
id: command-http-outcomes
title: Client Command HTTP Outcomes
description: Reference command effect lifecycle actions, HTTP response handling, failure codes, and exception boundaries.
sidebar_position: 10
sidebar_label: Client Command HTTP Outcomes
---

# Client Command HTTP Outcomes

## Overview

`CommandActionEffectBase` maps a matching command action to an HTTP request and yields lifecycle actions. The executing action is emitted before the request; a terminal action depends on the response or the kind of exception raised.

## Applies To

- `Mississippi.Inlet.Client.Abstractions.ActionEffects.CommandActionEffectBase`
- `OperationResultDto` and factories for command lifecycle actions
- Generated client command and saga-start effects that inherit this base

## Request Lifecycle

The [base effect](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs) handles actions assignable to its `TAction`. Other actions produce no lifecycle actions. Its async iterator performs work as it is enumerated and ignores the supplied feature state.

For a matching action, it generates a command ID using the `N` GUID format, uses the action type's name as the command type, and yields an executing action with the current `TimeProvider` timestamp. The provider defaults to `TimeProvider.System`.

It then resolves the endpoint, maps the action to a request DTO, and calls `PostAsJsonAsync`. The default endpoint joins `{AggregateRoutePrefix}/{EntityId}/{Route}` directly, without escaping the entity ID. Derived effects can override `GetEndpoint`. Generated aggregate effects use root-relative `/api/aggregates/...` URLs, and generated saga-start effects use `/api/sagas/{routePrefix}/{sagaId}`. Both need a host-provided `HttpClient.BaseAddress`; generated registration does not configure it. Without a base address, the relative request throws `InvalidOperationException` after the executing action. That exception follows the uncaught base-effect/store boundary below and can leave normal store state executing without a terminal action.

Generated command clients always POST. They derive the aggregate route segment from the command namespace and do not read the gateway aggregate attribute's `RoutePrefix` override. An override can therefore make the generated client POST to a different URL and receive HTTP 404. Keep the routes aligned or use a custom client effect. The [gateway generator](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Gateway.Generators/AggregateControllerGenerator.cs) supports other verbs through `GenerateCommand.HttpMethod`, but the client base does not select that verb. A generated non-POST endpoint normally rejects this client request with HTTP 405; use compatible POST routes or an appropriate custom effect.

The cancellation token is forwarded to the POST and response reads. That token is available when calling/enumerating the effect directly. Normal Reservoir `Store.Dispatch` has no token parameter, and the built-in store invokes effects with `CancellationToken.None`; dispatching a generated command action does not supply caller cancellation to this request. The base effect does not add a retry loop. Returning from `Store.Dispatch` does not await the HTTP effect or a terminal lifecycle action. Separate command dispatches can run overlapping requests and finish out of dispatch order; an immediate command-state snapshot need not show completion.

## Response And Failure Actions

When the request and body read complete, an unsuccessful HTTP status produces failure code `HttpError`, with a message containing the numeric status and response body. Cancellation or a network failure during response buffering or reading instead follows the caught-exception outcomes below. Successful HTTP statuses are parsed as [`OperationResultDto`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Abstractions/Commands/OperationResultDto.cs):

- A null parsed result produces code `NoResponse` and message `No response from server.`
- A result with `Success` false produces the result's error code/message, substituting `Unknown` and `Unknown error` only for null values.
- A result with `Success` true produces a succeeded action; any result error fields do not alter that outcome.

`OperationResultDto` is a record with independently supplied success and error fields. It does not enforce agreement between them.

The built-in [aggregate controller base](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Gateway/AggregateControllerBase.cs) returns HTTP 400 for an unsuccessful domain `OperationResult`. This client's non-success branch then emits `HttpError`; the domain error remains inside the response text, rather than becoming the action's structured error code. The structured error-code branch above applies to unsuccessful results returned with a successful HTTP status.

The terminal action retains the generated command ID and receives a new timestamp from the same time provider.

That ID belongs to local lifecycle actions; the base effect does not add it to the request DTO or headers as an idempotency key. A transport failure or cancellation can occur after the server committed, so `HttpError` is not proof of rejection. Retrying needs application-level outcome reconciliation or idempotency; this effect provides no deduplication guarantee.

## Exception Boundary

The request block converts `HttpRequestException` to `HttpError` with a network-error message. It also converts `TaskCanceledException` to `HttpError` with a request-cancelled message. An `HttpClient.Timeout` expiration can throw that same exception type, including when the store supplies `CancellationToken.None`; it receives the same failure code and request-cancelled message prefix. That outcome therefore does not distinguish timeout from caller cancellation. See the [.NET 10 timeout handling](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Net.Http/src/System/Net/Http/HttpClient.cs).

When directly enumerating this base effect, other exceptions, including JSON parsing failures and mapper failures outside those caught types, propagate after the executing action. An `OperationCanceledException` that is not a `TaskCanceledException` also propagates there. In normal Reservoir dispatch, [`RootActionEffect`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Core/RootActionEffect.cs) stops enumeration on noncritical failures and the [store](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Core/Store.cs) catches effect exceptions. Those ordinary failures do not reach the application dispatch caller and can leave the executing entry without a terminal action.

The [command effects generator](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Generators/CommandClientActionEffectsGenerator.cs) and [saga effects generator](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Generators/SagaClientActionEffectsGenerator.cs) supply the derived effect's action/DTO types and routes. The HTTP outcome rules above come from the shared base implementation.

## Summary

Use lifecycle actions to track the client request outcome, including the documented failure codes. They do not by themselves establish projection freshness or guarantee a terminal action for an uncaught exception.

## Next Steps

- Read [Inlet Reference](./reference.md) for generated client feature registration.
- Read [Read Models And Client Synchronization](../../concepts/read-models-and-client-sync.md) for the separate projection flow.
