---
id: aggregate-keys
title: Aggregate Keys
description: Reference aggregate entity identity, raw string conversion, validation, and the separate brook-key boundary.
sidebar_position: 2
sidebar_label: Aggregate Keys
---

# Aggregate Keys

## Overview

`AggregateKey` represents an aggregate's entity ID. Its string representation contains that ID alone; it does not contain the aggregate's brook name.

## Applies To

- `Mississippi.DomainModeling.Abstractions.AggregateKey`
- `IAggregateGrainFactory.GetGenericAggregate<TAggregate>`

## Representation And Validation

The [readonly record struct](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Abstractions/AggregateKey.cs) stores one read-only `EntityId` property. Its constructor rejects null with `ArgumentNullException` and a string longer than 4192 UTF-16 code units with `ArgumentException`. The limit uses `string.Length`.

The constructor permits an empty string and does not reject whitespace or separator characters. It preserves the supplied value without trimming, case changes, or escaping.

`Parse`, `FromString`, and the implicit conversion from `string` construct the same raw entity key. Parsing does not split a composite key: passing `name|id` stores the entire string as `EntityId`. `ToString()` and the implicit conversion to `string` return that stored value.

The [existing key tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.Abstractions.L0Tests/AggregateKeyTests.cs) cover construction, null and oversized input, conversions, parsing, and record equality.

## Factory And Activation Boundary

The [aggregate factory](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/AggregateGrainFactory.cs) passes `AggregateKey.EntityId` directly as the Orleans primary key. Its separate `string` overload rejects null, empty, or whitespace-only input before resolving the grain; the typed-key overload does not repeat that guard.

On [aggregate activation](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/GenericAggregateGrain.cs), the runtime derives the brook name from the aggregate type's `[BrookName]` attribute and combines it with the primary key through `BrookKey.ForType<TAggregate>`.

That [brook key](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Abstractions/BrookKey.cs) has separate constraints: it rejects pipe characters in either component and limits the combined name, separator, and ID to 4192 code units. An entity string accepted by `AggregateKey` can therefore still fail brook-key construction during activation. A missing `[BrookName]` also fails activation.

## Default Struct State

`default(AggregateKey)` and parameterless `new AggregateKey()` bypass the string constructor and have a null `EntityId`. Their string conversions return that null value despite the property's non-nullable declaration. They do not provide a validated entity identity.

## Summary

Aggregate keys store raw entity IDs. Typed-key construction, the factory's string overload, and activation's composite brook key each have their own validation boundary.

## Next Steps

- Read [Spring Key Concepts](../../samples/spring-sample/concepts/key-concepts.md) for aggregate and event-stream identities in the sample.
- Read [Domain Modeling Concepts](../concepts/concepts.md) for the aggregate runtime's role.
