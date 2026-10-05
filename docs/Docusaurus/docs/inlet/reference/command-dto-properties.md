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

[`CommandModel`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Core/Analysis/CommandModel.cs) selects declared public instance properties with a getter. It does not walk base types to collect inherited properties. Static, nonpublic, and getterless properties are excluded by this selection. Escaped keyword names are not re-escaped during emission: a source property named `@event` becomes `event` in generated declarations/mappings and fails compilation. Avoid keyword property names for this generation path. Public instance indexers with getters are also selected; the analysis does not exclude `IsIndexer`. The emitters treat them as ordinary named properties/arguments without index parameters, producing invalid source. Indexers are unsupported on generated command types.

The [server DTO generator](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Gateway.Generators/CommandServerDtoGenerator.cs) emits a sealed record with the source property's C# name and source type name. For ordinary ASCII names, `JsonPropertyName` lowercases an uppercase first character and preserves the rest: `URL` becomes `uRL` and `IPAddress` becomes `iPAddress`. The [naming helper](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Core/Naming/NamingConventions.cs) uses character arithmetic rather than Unicode lowercasing. For a multi-character name, it adds 32 to a first character classified as uppercase: `Āmount` (U+0100) becomes `Ġmount` (U+0120), rather than `āmount`. Its one-character branch instead applies bitwise OR with `0x20` to a letter. Use ASCII identifiers for predictable wire names with this generator. The source type text is minimally qualified, and DTO files do not import each property type's source namespace. Custom type names must resolve in the generated namespaces, for example through suitable global usings; unresolved names make generated source fail compilation. Selected property names must also produce distinct JSON names: `URL` and `uRL` both emit `JsonPropertyName("uRL")`. The generator does not detect that collision. System.Text.Json throws `InvalidOperationException` when building the conflicting DTO metadata, so a compiling DTO can still fail at runtime. See the [.NET 10 conflict handling](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Metadata/JsonTypeInfo.cs).

The [client DTO generator](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Generators/CommandClientDtoGenerator.cs) emits an internal sealed positional record. Every selected property becomes a constructor parameter with its source type and name. It does not copy optional defaults into those parameters. The generated client action additionally starts with a fixed `string EntityId` parameter. Reserve `EntityId` for that generated parameter: a selected command property with that name adds a duplicate parameter and fails compilation.

For a command using object-initializer mapping rather than constructor mapping, the generated server mapper must be able to construct it and assign every selected property. Each property needs an accessible setter or init accessor; a public getter-only property is still selected but its generated assignment fails compilation.

`CommandModel` selects the first non-static constructor with parameters without checking that it is a primary constructor. A record class's synthesized copy constructor can therefore trigger constructor mapping for a property-style record too. The server mapper emits `new Command(allSelectedProperties...)` in property enumeration order; C# overload resolution must find an accessible constructor accepting those arguments. An extra selected computed property can cause an argument-count mismatch, while reordered same-type arguments can receive unintended values. The generator does not validate that compatibility.

## Server Required-Field Inference

[`PropertyModel`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Core/Analysis/PropertyModel.cs) marks a property required when it is non-nullable and has no detected declared default. This includes non-nullable value types: an `int` without a detected default is required too.

Nullability includes annotated nullable reference types and `Nullable<T>` value types. An oblivious reference type from disabled nullable annotations has `NullableAnnotation.None`, which this analysis does not classify as nullable. Without a detected default, it is therefore marked required too. Defaults are detected from available property initializer or parameter syntax, or from a same-named constructor parameter with an explicit default. Constructor parameter matching uses ordinal, case-sensitive names. Source initializers require syntax to be available to this analysis. Properties imported from a referenced assembly have no declaring syntax for those initializers, so a compiled property initializer is not a detected default. A same-named constructor parameter's explicit default can still be read from metadata. Required-field inference can therefore differ from analysis of the command's source in the current compilation.

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
