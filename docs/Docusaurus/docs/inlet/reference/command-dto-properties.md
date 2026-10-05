---
id: command-dto-properties
title: Generated Command DTO Properties
description: Reference command property selection, server JSON names, and nullable/default-based required inference.
sidebar_position: 11
sidebar_label: Generated Command DTO Properties
---

# Generated Command DTO Properties

## Overview

The command DTO generators select source properties through a shared `CommandModel`. Server required-field inference and client constructor parameters then follow different emission rules.

## Applies To

- Commands selected by `GenerateCommandAttribute`
- `CommandServerDtoGenerator` and `CommandClientDtoGenerator`
- Shared `CommandModel` and `PropertyModel` analysis

## Property Selection And Names

[`CommandModel`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Core/Analysis/CommandModel.cs) selects declared public instance properties with a getter. It does not walk base types to collect inherited properties. Static, nonpublic, and getterless properties are excluded by this selection.

The [server DTO generator](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Gateway.Generators/CommandServerDtoGenerator.cs) emits a sealed record with the source property's C# name and source type name. Each property receives `JsonPropertyName` using the camelCase form of that name.

The [client DTO generator](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Generators/CommandClientDtoGenerator.cs) emits an internal sealed positional record. Every selected property becomes a constructor parameter with its source type and name. It does not copy optional defaults into those parameters.

For a command using object-initializer mapping rather than constructor mapping, the generated server mapper must be able to construct it and assign every selected property. Each property needs an accessible setter or init accessor; a public getter-only property is still selected but its generated assignment fails compilation.

For constructor-mapped records, the server mapper passes every selected property in enumeration order to the selected parameterized constructor. Its count, order, and types must accept that sequence. An extra selected computed property can add an argument absent from the constructor and fail compilation; compatible same-type parameters in a different order can receive unintended values. The generator does not validate that compatibility.

## Server Required-Field Inference

[`PropertyModel`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Core/Analysis/PropertyModel.cs) marks a property required when it is non-nullable and has no detected declared default. This includes non-nullable value types: an `int` without a detected default is required too.

Nullability includes annotated nullable reference types and `Nullable<T>` value types. Defaults are detected from available property initializer or parameter syntax, or from a same-named constructor parameter with an explicit default. Constructor parameter matching uses ordinal, case-sensitive names. Source initializers require syntax to be available to this analysis.

The server emitter then applies these rules:

- Required properties receive both the C# `required` modifier and `JsonRequired`.
- Nullable properties are emitted without either required marker or an explicit initializer.
- Other non-nullable properties receive `= default!;` and no required markers.

A detected source default therefore changes required-field inference, but this server emitter does not reproduce that default value in the DTO. The source `required` modifier is not a separate input to this inference.

## Validation Boundary

Generated required markers describe the generated DTO contract. They do not implement business rules such as positive quantities, valid account identifiers, or an allowed command at the current aggregate state; those rules belong in command handling.

The [property-model tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Generators.Core.L0Tests/Analysis/PropertyModelTests.cs) cover nullable reference/value types and required inference without defaults. The [server tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Gateway.Generators.L0Tests/CommandServerDtoGeneratorTests.cs) cover generated property names and camelCase JSON names; the [client tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Client.Generators.L0Tests/CommandClientDtoGeneratorTests.cs) assert the internal sealed record declaration and source property type text. They do not assert positional parameter-list syntax; that emission shape above is verified from the generator.

## Summary

Review the selected source properties and the defaults visible to the generator when assessing server required fields. Client positional parameters and server optional-property initialization do not preserve source defaults in the same way.

## Next Steps

- Read [Generated Application Contracts](./generated-contracts.md) for generator inputs and host wiring.
- Read [Inlet Reference](./reference.md) for client registration surfaces.
