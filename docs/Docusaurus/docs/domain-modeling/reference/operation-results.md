---
id: operation-results
title: Domain Operation Results
description: Reference success and failure factories, typed values, conversion, and default result behavior.
sidebar_position: 7
sidebar_label: Operation Results
---

# Domain Operation Results

## Overview

`OperationResult` carries an operation's success flag or failure details. `OperationResult<T>` also carries a success value, such as the event list returned by a command handler.

## Applies To

- `Mississippi.DomainModeling.Abstractions.OperationResult`
- `Mississippi.DomainModeling.Abstractions.OperationResult<T>`

## Factory Results

The [readonly record structs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Abstractions/OperationResult.cs) have private value-setting constructors and expose public factories on the non-generic `OperationResult` type:

| Factory | Result |
|---------|--------|
| `Ok()` | `Success` is true; both error fields are null |
| `Ok<T>(value)` | `Success` is true; `Value` is supplied; both error fields are null |
| `Fail(errorCode, errorMessage)` | `Success` is false; both supplied error fields are retained |
| `Fail<T>(errorCode, errorMessage)` | The same failure details, with `Value` set to `default(T)` |

Both failure factories reject a null code or message with `ArgumentNullException`, and an empty or whitespace-only value with `ArgumentException`. Nonempty supplied text is retained without trimming.

The success-value factory does not check its value for null. A supplied reference is retained rather than cloned; the result type does not make an object stored in `Value` deeply immutable. Nullable-flow annotations on `Success` do not add runtime value validation.

## Typed To Untyped Conversion

`OperationResult<T>.ToResult()` returns a non-generic result. For success, it returns `Ok()` and drops the success value. For a factory-created failure, it calls `Fail` and preserves the error code and message.

This conversion preserves the success/failure outcome, rather than transporting the typed value. It does not map errors to HTTP status codes or throw an exception to represent an ordinary factory-created failure.

The existing [non-generic tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.Abstractions.L0Tests/OperationResultTests.cs) and [generic tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.Abstractions.L0Tests/OperationResultOfTTests.cs) verify factory output, error guards, reference retention, and success/failure conversion.

## Default Struct State

A default or parameterless result has `Success` false and null error fields. A generic default also has `Value` equal to `default(T)`. This state bypasses the failure factories' detail validation.

Calling `ToResult()` on a default generic result takes the failure branch and passes its null error code to `Fail`, which throws `ArgumentNullException`. A default result therefore is not equivalent to a failure created with valid details.

## Summary

Use the factories to obtain an explicit success or a validated failure. Typed conversion drops success data, and default struct states can violate the detail assumptions expressed by the result's nullable annotations.

## Next Steps

- Read [Spring's Aggregate Tutorial](../../samples/spring-sample/tutorials/building-an-aggregate.md) for command handlers returning event-list results.
- Read [Domain Modeling Concepts](../concepts/concepts.md) for the command-handling boundary.
