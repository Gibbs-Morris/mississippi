---
id: cosmos-storage-retries
title: Cosmos Storage Retries
description: Reference the shared Cosmos retry policy's attempt count, delay selection, cancellation, and exception behavior.
sidebar_position: 42
sidebar_label: Cosmos Storage Retries
---

# Cosmos Storage Retries

## Overview

`CosmosRetryPolicy` is the shared retry implementation registered by the Brooks and Tributary Cosmos storage providers. It retries selected Cosmos failures around one supplied asynchronous operation.

## Applies To

- `Mississippi.Common.Runtime.Storage.Abstractions.Retry.IRetryPolicy`
- `Mississippi.Common.Runtime.Storage.Cosmos.Retry.CosmosRetryPolicy`

## Attempts And Delays

The [constructor](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Common.Runtime.Storage.Cosmos/Retry/CosmosRetryPolicy.cs) defaults `maxRetries` to `3`: one initial call and up to three retries. `maxRetries: 0` permits only the initial call. The constructor does not validate the retry count.

| Cosmos status | Retried while attempts remain |
|---------------|-------------------------------|
| `429 TooManyRequests` | Yes |
| `503 ServiceUnavailable` | Yes |
| `408 RequestTimeout` | Yes |
| `500 InternalServerError` | Yes |
| `504 GatewayTimeout` | Yes |

Before a retry, the policy uses the exception's `RetryAfter` when present. Otherwise its delay is `100 * 2^attempt` milliseconds, where the initial attempt is numbered zero. With the default count, fallback delays are 100, 200, and 400 milliseconds. There is no policy delay after the final permitted attempt fails.

These are the wrapper's delays and attempt limits, rather than a bound on all underlying Cosmos client requests or their total elapsed time.

## Cancellation Boundary

The [interface](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Common.Runtime.Storage.Abstractions/Retry/IRetryPolicy.cs) accepts a `Func<Task<T>>` and a separate optional cancellation token. It checks that token before each attempt and passes it to `Task.Delay`.

The delegate has no token parameter. Canceling the policy token therefore does not automatically cancel a running operation; the supplied operation needs its own token handling.

A `TaskCanceledException` from the operation is wrapped in `OperationCanceledException`, with the original exception as its inner exception and the policy token attached. The message distinguishes a requested cancellation from an operation canceled before completion. Neither case is retried.

Cancellation detected before entering an attempt, or during the retry delay, escapes directly from the token check or `Task.Delay`. Those paths do not use the operation's custom cancellation wrapper or message.

## Failure Outcomes

- A null operation throws `ArgumentNullException` before an attempt.
- `404 NotFound` passes through as the original `CosmosException`, without retry.
- `413 RequestEntityTooLarge` becomes an `InvalidOperationException` explaining the payload-size limit, with the Cosmos exception retained as its inner exception.
- Other non-retryable Cosmos failures, and a retryable failure on the final attempt, become `InvalidOperationException` with the Cosmos status and original inner exception.
- Other operation exceptions propagate without policy retry.

For ordinary retry exhaustion, the final-attempt Cosmos branch produces the status-bearing wrapper. The separate `Operation failed after ... attempts` fallback is not that normal outcome. A negative retry count skips the loop and reaches that fallback without calling the operation.

The [existing tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Common.Runtime.Storage.Cosmos.L0Tests/CosmosRetryPolicyTests.cs) assert two calls before transient success and exception outcomes for exhausted retries, not-found, oversized requests, and cancellation. The exhaustion and not-found tests do not assert attempt counts or preservation of the original exception instance; those details above come from the implementation.

## Summary

The default policy allows four operation calls and retries five Cosmos statuses. Its token controls attempt entry and delay; exception wrapping and retry do not establish whether a storage write committed.

## Next Steps

- Read [Brooks Cosmos Storage](../brooks/storage-providers/cosmos.md) for event-provider setup.
- Read [Tributary Cosmos Storage](../tributary/storage-providers/cosmos.md) for snapshot-provider setup.
- Read [Brook Append Outcomes](./brook-append-outcomes.md) for write commitment and publication failures.
