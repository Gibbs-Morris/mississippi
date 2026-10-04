---
title: Aqueduct Routing Keys
description: Reference group, client, and server-directory key formats, length limits, and parsing failures.
sidebar_position: 2
---

# Aqueduct Routing Keys

Aqueduct uses distinct key types for SignalR groups, clients, and the server directory. Group and client identities include the hub name; directory identities contain one value.

## Applies To

- `Mississippi.Aqueduct.Abstractions.Keys.SignalRGroupKey`
- `SignalRClientKey` and `SignalRServerDirectoryKey`
- Grain identities and their string representations

## Group And Client Keys

[`SignalRGroupKey`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Abstractions/Keys/SignalRGroupKey.cs) represents `{HubName}:{GroupName}`. [`SignalRClientKey`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Abstractions/Keys/SignalRClientKey.cs) represents `{HubName}:{ConnectionId}`. Both use a colon as their separator.

Their constructors apply the same rules:

- Neither component can be null.
- Neither component can contain a colon.
- The combined length, including the separator, cannot exceed 4,192 characters.

The constructors permit empty or whitespace components and preserve them without trimming or case normalization. String conversion and `ToString` use the stored values directly. For example, hub `Orders` and group `admins` produce `Orders:admins`.

## Parsing Failures

Both `Parse` methods split the input at its first colon and pass the two parts to the constructor:

- Null input raises `ArgumentNullException`.
- Input without a colon raises `FormatException`.
- An additional colon remains in the second component and raises `ArgumentException` through constructor validation.
- An overlong composite also raises `ArgumentException` through the constructor.

An empty component on either side of the separator is accepted by the constructor. These methods do not translate every invalid input into `FormatException`.

## Server Directory Keys

[`SignalRServerDirectoryKey`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Abstractions/Keys/SignalRServerDirectoryKey.cs) stores one `Value`. Its constructor and parser reject null and values longer than 4,192 characters. They do not split on a separator; colons, empty text, and whitespace are permitted.

Its named `Default` field contains the value `default`. Use that field when selecting the conventional directory identity. A C# default struct value does not run the constructor or initialize that named default.

The [group](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Aqueduct.Abstractions.L0Tests/Keys/SignalRGroupKeyTests.cs), [client](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Aqueduct.Abstractions.L0Tests/Keys/SignalRClientKeyTests.cs), and [directory tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Aqueduct.Abstractions.L0Tests/Keys/SignalRServerDirectoryKeyTests.cs) cover valid identities, null/length guards, conversions, equality, and parse round trips. Group/client tests also cover separator rejection and missing-separator parsing.

## Summary

Use the key type matching the destination. Preserve hub and component values consistently, respect the composite length limit, and handle constructor failures as well as parser format failures.

## Next Steps

- Read [Aqueduct Reference](./reference.md) for runtime composition and options.
- Read [Aqueduct Concepts](../concepts/concepts.md) for backplane responsibilities.
