---
title: Client Command HTTP Outcomes
description: Reference command effect lifecycle actions, HTTP response handling, failure codes, and exception boundaries.
sidebar_position: 10
---

# Client Command HTTP Outcomes

`CommandActionEffectBase` maps a matching command action to an HTTP request and yields lifecycle actions. The executing action is emitted before the request; a terminal action depends on the response or the kind of exception raised.

## Applies To

- `Mississippi.Inlet.Client.Abstractions.ActionEffects.CommandActionEffectBase`
- `OperationResultDto` and command executing/succeeded/failed action factories
- Generated client command effects that inherit this base

## Request Lifecycle

The [base effect](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs) handles actions assignable to its `TAction`. Other actions produce no lifecycle actions. Its async iterator performs work as it is enumerated and ignores the supplied feature state.

For a matching action, it generates a command ID using the `N` GUID format, uses the action type's name as the command type, and yields an executing action with the current `TimeProvider` timestamp. The provider defaults to `TimeProvider.System`.

It then resolves the endpoint, maps the action to a request DTO, and calls `PostAsJsonAsync`. The default endpoint joins `{AggregateRoutePrefix}/{EntityId}/{Route}` directly, without escaping the entity ID. Derived effects can override `GetEndpoint`.

The cancellation token is forwarded to the POST and response reads. The base effect does not add a retry loop.

## Response And Failure Actions

An unsuccessful HTTP status is read as text and produces failure code `HttpError`, with a message containing the numeric status and response body. Successful HTTP statuses are parsed as [`OperationResultDto`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Abstractions/Commands/OperationResultDto.cs):

- A null parsed result produces code `NoResponse` and message `No response from server.`
- A result with `Success` false produces the result's error code/message, substituting `Unknown` and `Unknown error` only for null values.
- A result with `Success` true produces a succeeded action; any result error fields do not alter that outcome.

`OperationResultDto` is a record with independently supplied success and error fields. It does not enforce agreement between them.

The terminal action retains the generated command ID and receives a new timestamp from the same time provider.

## Exception Boundary

The request block converts `HttpRequestException` to `HttpError` with a network-error message. It also converts `TaskCanceledException` to `HttpError` with a request-cancelled message.

Other exceptions, including JSON parsing failures and mapper failures outside those caught types, propagate after the executing action. An `OperationCanceledException` that is not a `TaskCanceledException` also propagates. Consumers cannot assume that every executing action from this iterator is followed by a failed or succeeded action.

The [command effects generator](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Generators/CommandClientActionEffectsGenerator.cs) supplies the derived effect's action/DTO types and routes. The HTTP outcome rules above come from the shared base implementation.

## Summary

Use lifecycle actions to track the client request outcome, including the documented failure codes. They do not by themselves establish projection freshness or guarantee a terminal action for an uncaught exception.

## Next Steps

- Read [Inlet Reference](./reference.md) for generated client feature registration.
- Read [Read Models And Client Synchronization](../../concepts/read-models-and-client-sync.md) for the separate projection flow.
