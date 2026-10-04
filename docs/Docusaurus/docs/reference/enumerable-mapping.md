---
id: enumerable-mapping
title: Enumerable Mapping
description: Reference lazy sequence mapping, element handling, and transient mapper registration.
sidebar_position: 3
sidebar_label: Enumerable Mapping
---

# Enumerable Mapping

## Overview

Collection mappers adapt an element-level `IMapper<TFrom, TTo>` to synchronous or asynchronous sequences. They preserve enumeration order and invoke that same element mapper for each item.

## Applies To

- `Mississippi.Common.Abstractions.Mapping`
- `IEnumerableMapper<TFrom, TTo>` and `EnumerableMapper<TFrom, TTo>`
- `IAsyncEnumerableMapper<TFrom, TTo>` and `AsyncEnumerableMapper<TFrom, TTo>`

## Synchronous Sequences

[`EnumerableMapper.Map`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Common.Abstractions/Mapping/EnumerableMapper.cs) rejects a null input sequence immediately, then returns a lazy `Select` sequence. Calling `Map` alone does not map its elements or materialize a collection.

Enumeration invokes the element mapper in input order. Enumerating the returned sequence again runs mapping again against the input sequence; the adapter does not cache earlier results. Element-mapper failures occur when the corresponding element is enumerated.

An empty sequence produces no elements. A null element passes to the element mapper, so that mapper determines its handling; the adapter does not remove null elements.

## Asynchronous Sequences

[`AsyncEnumerableMapper.Map`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Common.Abstractions/Mapping/AsyncEnumerableMapper.cs) returns a lazy async sequence. Its null-input guard runs when enumeration starts, rather than when the iterator is obtained.

For each asynchronously received input item, it calls the synchronous element mapper and yields that result before moving to the next item. It does not start parallel mapping tasks or materialize the entire input. Empty inputs, null elements, and mapper failures follow the same per-element boundary as synchronous mapping.

`Map` exposes no cancellation-token parameter. Supply the consumer token when enumerating its output, such as through `WithCancellation(token)`. The returned iterator forwards that token to the input's enumerator; cancellation depends on the input honoring it.

## Service Registration

The [registration extensions](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Common.Abstractions/Mapping/MappingRegistrations.cs) add these transient services:

| Extension | Registration |
| --- | --- |
| `AddMapper<TFrom, TTo, TMapper>()` | `IMapper<TFrom, TTo>` implemented by `TMapper` |
| `AddIEnumerableMapper()` | Open generic `IEnumerableMapper<,>` implemented by `EnumerableMapper<,>` |
| `AddIAsyncEnumerableMapper()` | Open generic `IAsyncEnumerableMapper<,>` implemented by `AsyncEnumerableMapper<,>` |

Register the element mapper separately. The collection extensions only register their adapters. Each extension uses `AddTransient` and returns the service collection; repeated calls add registrations rather than using `TryAdd` to suppress duplicates.

`AddMapper<TFrom, TTo, TMapper>` requires `TMapper : class, IMapper<TFrom, TTo>`. A value-type mapper cannot be used as that registration helper's implementation argument.

Existing [synchronous](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Common.Abstractions.L0Tests/Mapping/EnumerableMapperTests.cs) and [asynchronous](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Common.Abstractions.L0Tests/Mapping/AsyncEnumerableMapperTests.cs) tests cover null inputs, ordered results, null elements, and empty inputs. [Cancellation tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Common.Abstractions.L0Tests/Mapping/AsyncEnumerableMapperCancellationTests.cs) verify lazy enumeration, deferred null validation, token forwarding, and source disposal after cancellation.

## Summary

Choose the adapter matching your input sequence and register its element mapper. Mapping happens during enumeration, so consumption determines when mapper work and failures occur.

## Next Steps

- Read the [Capability and Package Map](./capability-map.md) for shared mapping and consumer package boundaries.
- Read [Inlet Reference](../inlet/reference/reference.md) for generated client integration.
