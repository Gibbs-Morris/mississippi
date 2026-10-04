---
title: Brooks Event Envelope
description: Reference persisted event fields, default metadata, payload bytes, and downstream validation boundaries.
sidebar_position: 6
sidebar_label: Event Envelope
---

# Brooks Event Envelope

`BrookEvent` carries serialized event bytes and their metadata across the Brooks read/write boundary. It is a sealed record with init-only properties.

## Applies To

- `Mississippi.Brooks.Abstractions.BrookEvent`
- Event envelopes passed to Brooks writers and returned by Brooks readers

## Fields And Defaults

The [record declaration](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Abstractions/BrookEvent.cs) defines these defaults for a newly constructed envelope:

| Property | Meaning And Default |
|----------|---------------------|
| `EventType` | Semantic event-type name; empty string by default |
| `Source` | Producer-supplied source metadata; empty string by default |
| `Id` | Event-instance identifier; empty string by default |
| `Time` | Optional `DateTimeOffset` timestamp; `null` by default |
| `DataContentType` | Payload-format label; empty string by default |
| `Data` | `ImmutableArray<byte>` payload; empty by default |
| `DataSizeBytes` | Separate `long` byte-size metadata; `0` by default |

The [existing envelope tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Brooks.Abstractions.L0Tests/BrookEventTests.cs) cover defaults and values supplied through an initializer.

## Interpreting Metadata

The record stores supplied values. It does not generate an ID or timestamp, calculate `DataSizeBytes` from `Data`, or validate that the metadata agrees with the payload. Empty defaults do not establish that an envelope is ready for a particular storage provider.

The [default domain-event converter](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/BrookEventConverter.cs) supplies the registry's event name, a generated ID, UTC time from its `TimeProvider`, serialized bytes, and a matching byte count. Its `Source` is the complete [stream key](./stream-keys.md), such as `SPRING.BANKING.ACCOUNT|acc-123`, including the entity ID.

That converter copies the configured serialization provider's `Format` into `DataContentType`. The built-in [JSON provider](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Serialization.Json/JsonSerializationProvider.cs) reports `System.Text.Json`. The envelope accepts that label; it does not enforce MIME syntax or a fixed list of content types.

## Failure Behavior

The envelope's property initializers perform no validation. Consumers and providers apply their own requirements after construction. For example, the [Cosmos event mapper](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime.Storage.Cosmos/Mapping/EventToStorageMapper.cs) throws `InvalidOperationException` when `Time` is null, as its [existing tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Brooks.Runtime.Storage.Cosmos.L0Tests/Mapping/EventToStorageMapperTests.cs) verify.

## Summary

A `BrookEvent` pairs payload bytes with independently supplied metadata. Its defaults and init-only properties describe the envelope representation; conversion and storage services establish the values and downstream requirements.

## Next Steps

- Read [Brooks Stream Keys](./stream-keys.md) for the default converter's source identity.
- Read [Brooks Cosmos Storage](../storage-providers/cosmos.md) for provider configuration and persistence behavior.
