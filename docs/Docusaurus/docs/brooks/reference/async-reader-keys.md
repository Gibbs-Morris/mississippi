---
id: async-reader-keys
title: Brooks Streaming Reader Keys
description: Reference streaming-reader grain identity, GUID suffixes, parsing, and factory lifecycle boundaries.
sidebar_position: 5
sidebar_label: Streaming Reader Keys
---

# Brooks Streaming Reader Keys

## Overview

`BrookAsyncReaderKey` identifies a streaming-reader grain using an event-stream key and an instance GUID. Resolve a streaming reader through `IBrookGrainFactory.GetBrookAsyncReaderGrain(brookKey)`.

## Applies To

- `Mississippi.Brooks.Abstractions.BrookAsyncReaderKey`
- `Mississippi.Brooks.Abstractions.Factory.IBrookGrainFactory`

## Identity And Encoding

The [key contract](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Abstractions/BrookAsyncReaderKey.cs) exposes two read-only properties:

| Property | Meaning |
|----------|---------|
| `BrookKey` | Event stream being read |
| `InstanceId` | GUID included in the reader grain's identity |

`ToString()` and the implicit conversion to a string produce `brookName|entityId|instanceId`. The GUID uses `N` format: 32 hexadecimal digits without hyphens. One literal key is `SPRING.BANKING.ACCOUNT|acc-123|12345678123412341234123456789abc`.

`Create(brookKey)` calls `Guid.NewGuid()`. The [default grain factory](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime/Factory/BrookGrainFactory.cs) calls it on each `GetBrookAsyncReaderGrain()` resolution, including repeated resolutions for the same brook. The suffix changes the reader identity while retaining the underlying event-stream identity.

## Construction And Parsing

The explicit constructor stores the supplied `BrookKey` and GUID without validation. It accepts `Guid.Empty` and a default stream key. It also permits reusing a supplied GUID; the key type does not enforce one-time use.

`Parse()`, `FromString()`, and the implicit string-to-key conversion share the parser. It splits at the first two pipes, parses the remaining suffix with `Guid.TryParse()`, then constructs a `BrookKey` from the first two fields. Parsing accepts a hyphenated GUID as well as the canonical output format.

The nested stream key applies its [field and length checks](./stream-keys.md). Its `4192` UTF-16-unit limit includes the first separator; the async-reader key adds no check on the total length including the GUID suffix.

`default(BrookAsyncReaderKey)` and parameterless `new BrookAsyncReaderKey()` have a default `BrookKey` and `Guid.Empty`. Their string representation is `||00000000000000000000000000000000`. Parsing it succeeds with empty brook-name and entity-ID fields, so the round trip changes the nested default key's null fields.

## Reader Lifetime

The [streaming reader implementation](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime/Reader/BrookAsyncReaderGrain.cs) relies on Orleans' idle deactivation policy. It does not explicitly deactivate when enumeration completes or is canceled. Resolving a reader creates its identity; starting another enumeration on that already-resolved reference does not create another key.

The [factory tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Brooks.Runtime.L0Tests/Factory/BrookGrainFactoryTests.cs) verify different keys across repeated resolutions. The [key tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Brooks.Abstractions.L0Tests/BrookAsyncReaderKeyTests.cs) cover encoding, parsing, equality, and round trips.

## Failure Behavior

- A parser input with a missing first or second separator, or an invalid GUID suffix, throws `ArgumentException`. Null input follows the missing-first-separator path.
- Parsed brook-name and entity-ID fields pass through the `BrookKey` constructor. Its combined-length failure throws `ArgumentException`.

## Summary

A streaming-reader key adds an instance GUID to the event-stream identity. The default factory generates that suffix for each resolution; explicit construction accepts supplied values. Parsing validates the suffix and nested stream key, and the reader relies on Orleans idle cleanup.

## Next Steps

- Read [Brooks Stream Keys](./stream-keys.md) for the underlying stream identity.
- Use [Brooks Reader Options](./reader-options.md) for streaming read slices and range validation.
