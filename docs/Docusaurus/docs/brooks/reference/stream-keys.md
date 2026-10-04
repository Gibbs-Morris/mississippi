---
title: Brooks Stream Keys
description: Reference BrookKey identity, string encoding, constructor validation, and typed name factories.
sidebar_position: 4
sidebar_label: Stream Keys
---

# Brooks Stream Keys

`BrookKey` identifies one event stream by its brook name and entity ID. Typed factories can obtain the name from a type's `[BrookName]` attribute.

## Applies To

- `Mississippi.Brooks.Abstractions.BrookKey`
- `BrookKey.ForGrain<TGrain>(entityId)` and `BrookKey.ForType<T>(entityId)`

## Fields And String Format

The [key contract](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Abstractions/BrookKey.cs) exposes two read-only string properties:

| Property | Meaning |
|----------|---------|
| `BrookName` | Name of the brook |
| `EntityId` | Entity identifier within that brook |

`ToString()`, the implicit conversion to a string, and `FromBrookKey()` produce `brookName|entityId`. For example, Spring's banking stream for entity `acc-123` is `SPRING.BANKING.ACCOUNT|acc-123`.

## Construction And Parsing

The two-argument constructor accepts a literal brook name and entity ID. It applies these checks:

- Both fields must be non-null and must not contain `|`.
- Empty fields are accepted by this key type.
- `brookName.Length + entityId.Length + 1` must not exceed `4192`. The length includes the separator and counts UTF-16 code units, as [`string.Length`](https://learn.microsoft.com/en-us/dotnet/api/system.string.length) does.

The key has no escaping scheme for the delimiter. `FromString()` splits at the first `|` and passes both resulting fields to the constructor. An additional `|` remains in the entity ID and fails constructor validation.

`default(BrookKey)` and parameterless `new BrookKey()` bypass the two-argument constructor and have null fields. String conversion does not validate them: all three string-conversion paths produce `|`. Parsing that string succeeds with empty fields, so the round trip does not preserve the default key's null fields.

The [existing key tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Brooks.Abstractions.L0Tests/BrookKeyTests.cs) cover construction, validation, string encoding, and round trips.

## Typed Name Factories

`ForGrain<TGrain>(entityId)` and `ForType<T>(entityId)` both call [BrookNameHelper](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Abstractions/BrookNameHelper.cs) to read `[BrookName]` from the type, then run the same key constructor. Both constrain their type argument to a reference type; neither requires an Orleans grain interface. `ForType` also works with attributed projection records, as the [typed-factory tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Brooks.Abstractions.L0Tests/BrookKeyForTests.cs) demonstrate.

The [attribute](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Abstractions/Attributes/BrookNameAttribute.cs) combines its three components as `AppName.ModuleName.Name`. It is not inherited, so a derived type needs its own attribute for these factories.

Spring's [BankAccountAggregate](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Aggregates/BankAccount/BankAccountAggregate.cs) and [BankAccountBalanceProjection](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Projections/BankAccountBalance/BankAccountBalanceProjection.cs) both declare the components `SPRING`, `BANKING`, and `ACCOUNT`. Their brook name is `SPRING.BANKING.ACCOUNT`; the pipe separates that name from the entity ID in a `BrookKey`.

## Failure Behavior

- A null name or entity ID passed to the two-argument constructor throws `ArgumentNullException`.
- Either constructor field containing `|`, or a combined length above the limit, throws `ArgumentException`.
- A null input to `FromString()` throws `ArgumentNullException`; an input with no separator throws `FormatException`.
- An extra separator in parsed input reaches the constructor as part of the entity ID and throws `ArgumentException`.
- A typed factory whose type has no `[BrookName]` attribute throws `InvalidOperationException` during name lookup, before key construction.

## Summary

A stream key encodes `brookName|entityId`. Explicit construction and parsing validate its two fields and UTF-16 length; typed factories first obtain the dotted brook name from an attribute. Default values bypass validation; parsing their string representation produces empty fields.

## Next Steps

- Read [Brooks Range Keys](./range-keys.md) for adding start and count, including the expanded key's length limit.
- Use [Brooks Reader Options](./reader-options.md) for range partitioning.
- Read [Spring Key Concepts](../../samples/spring-sample/concepts/key-concepts.md) for the banking sample's domain terms.
