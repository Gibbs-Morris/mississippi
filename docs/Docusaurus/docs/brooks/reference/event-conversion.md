---
title: Brooks Event Conversion
description: Reference the synchronous event converter, generated storage metadata, and type-resolution failures.
sidebar_position: 9
sidebar_label: Event Conversion
---

# Brooks Event Conversion

`IBrookEventConverter` converts domain event objects to storage records and reconstructs domain objects from those records. Conversion prepares data; it does not append events to a brook.

## Applies To

- `Mississippi.Brooks.Abstractions.IBrookEventConverter`
- The default converter in `Mississippi.DomainModeling.Runtime`

## Methods

The [interface](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Abstractions/IBrookEventConverter.cs) exposes two synchronous methods:

| Method | Input and result |
|--------|------------------|
| `ToStorageEvents` | A `BrookKey` and `IReadOnlyList<object>` become an `ImmutableArray<BrookEvent>` |
| `ToDomainEvent` | One `BrookEvent` becomes one deserialized `object` |

Neither method accepts a cancellation token. The [default implementation](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/BrookEventConverter.cs) uses `IEventTypeRegistry`, `ISerializationProvider`, and an optional `TimeProvider`.

## Storage Conversion

`ToStorageEvents` processes the list eagerly in order. For each object's runtime type, it resolves the registered event name, serializes the object, and copies the returned bytes into an immutable array.

Each result has the resolved `EventType`, full brook key as `Source`, serializer `Format` as `DataContentType`, and byte length as `DataSizeBytes`. The converter generates a new GUID in `N` format for `Id` and calls `TimeProvider.GetUtcNow()` for `Time`. An omitted time provider defaults to `TimeProvider.System`.

An empty list returns an empty immutable array. If a later item fails, the call throws without returning its partially built array. Serializer and time-provider calls already made are not rolled back.

The [existing tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.Runtime.L0Tests/BrookEventConverterTests.cs) verify generated IDs, source and format metadata, injected and default timestamps, empty input, and unresolved names.

## Domain Reconstruction

`ToDomainEvent` resolves a CLR type from `BrookEvent.EventType`, then calls the configured provider's runtime-type `Deserialize` overload with that type and `Data`.

It does not select a different serializer from `DataContentType` or validate `DataSizeBytes`, `Time`, `Id`, or `Source`. Those fields are not arguments to its deserialization call. Provider compatibility and payload validity still matter.

## Failure Boundary

- A null event list or null storage event throws `ArgumentNullException`.
- An unresolved runtime type name during writing, or unresolved stored event name during reading, throws `InvalidOperationException`.
- A null element inside a non-null list reaches `GetType()` and throws `NullReferenceException`; there is no per-element null guard.
- Registry, serializer, and time-provider exceptions propagate from the call.

The converter does not persist the returned records or make conversion and persistence one transaction.

## Summary

The default converter synchronously combines registered event identities, serialization, and generated write metadata. Reading uses the configured provider and stored event type; conversion alone establishes no append outcome.

## Next Steps

- Read [Brooks Storage Providers](../storage-providers/index.md) for the persistence boundary.
- Read [Brook Append Outcomes](../../reference/brook-append-outcomes.md) for commitment and cursor publication behavior.
