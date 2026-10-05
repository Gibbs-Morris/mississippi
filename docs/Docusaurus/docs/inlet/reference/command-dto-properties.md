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

Use concrete, nongeneric command types for this generation path. Command names requiring C# keyword escaping, such as `@event`, are unsupported: Roslyn supplies `event`, and generated command references do not restore the escape, causing compilation failure. Both constructor and object-initializer mapping instantiate the command; an abstract class or record can be discovered but fails construction with CS0144. The analysis can select an attributed generic command, but generated DTO/action declarations do not reproduce its type parameters, and names derived from `TypeName` omit them. A command such as `Batch<T>` can therefore emit unresolved `T` or an invalid bare `Batch` reference and fail compilation; generic-command support is not provided. Commands in the current compilation must also be accessible from generated files; `file` commands are discovered but cannot be referenced from those separate files. Commands referenced from a domain assembly must be accessible to the generated gateway compilation. When a generated public aggregate controller exposes `IMapper<Dto, Command>` in its constructor, the command type must also be public; an inaccessible/internal command can fail generated compilation through accessibility or inconsistent-accessibility errors. Commands must also be top-level members of a nonempty named namespace. Global-namespace commands are discovered but emit invalid `namespace ;`/`using ;` directives; top-level placement alone does not make emission valid. Server and client scans recurse through namespaces and their `GetTypeMembers()`, not into containing types; an attributed nested command is ignored.

Commands processed together must have distinct generated namespace/type names and file hints after target-namespace projection. With one target root, `Alpha.Domain.Aggregates.Order.Commands.CreateOrder` and `Beta.Domain.Aggregates.Order.Commands.CreateOrder` both map to the same aggregate DTO namespace and `CreateOrderDto`/`CreateOrderRequestDto` names. The generators submit identical hints to `AddSource` and fail; different source assembly prefixes do not separate those outputs. Client legacy fallback naming is a different path, so compare the actual generated names rather than assuming every source namespace collapses.

## Property Selection And Names

[`CommandModel`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Core/Analysis/CommandModel.cs) selects declared public instance properties with a getter. It does not walk base types to collect inherited properties. Static, nonpublic, and getterless properties are excluded by this selection. Escaped keyword names are not re-escaped during emission: a source property named `@event` becomes `event` in generated declarations/mappings and fails compilation. Avoid keyword property names for this generation path. Public instance indexers with getters are also selected; the analysis does not exclude `IsIndexer`. The emitters treat them as ordinary named properties/arguments without index parameters, producing invalid source. Indexers are unsupported on generated command types. A selected property named `Clone` is unsupported too: both DTO outputs are records, whose [reserved member rules](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/record-declaration-errors#reserved-member-names) reject that name with CS8859 even when the source command is an ordinary class. Ref-like property types, such as `Span<T>` or a custom `ref struct`, are unsupported: generated record-class properties must store them, which fails with CS8345 even when a source class can expose a computed property of that type. Pointer and function-pointer property types are unsupported: emission places them into record storage, and [C# record classes disallow instance fields with unsafe types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/classes#15162-class-members). A source unsafe command compiling successfully does not establish that its generated record compiles.

Selected names must also be compatible with their generated enclosing type names. For `PlaceOrder`, reserve `PlaceOrderDto`, `PlaceOrderRequestDto`, and `PlaceOrderAction`: emitting a member with its enclosing type's name fails with [CS0542](https://learn.microsoft.com/en-us/dotnet/csharp/misc/cs0542). A valid source command property can therefore produce an invalid DTO or action.

The [server DTO generator](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Gateway.Generators/CommandServerDtoGenerator.cs) emits a sealed record with the source property's C# name and source type name. For ordinary ASCII names, `JsonPropertyName` lowercases an uppercase first character and preserves the rest: `URL` becomes `uRL` and `IPAddress` becomes `iPAddress`. The [naming helper](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Core/Naming/NamingConventions.cs) uses character arithmetic rather than Unicode lowercasing. For a multi-character name, it adds 32 to a first character classified as uppercase: `Āmount` (U+0100) becomes `Ġmount` (U+0120), rather than `āmount`. Its one-character branch instead applies bitwise OR with `0x20` to a letter. Use ASCII identifiers for predictable wire names with this generator. The source type text is minimally qualified, and DTO files do not import each property type's source namespace. Custom type names must resolve in the generated namespaces, for example through suitable global usings; unresolved names make generated source fail compilation. Selected property names must also produce distinct JSON names: `URL` and `uRL` both emit `JsonPropertyName("uRL")`. The generator does not detect that collision. System.Text.Json throws `InvalidOperationException` when building the conflicting DTO metadata, so a compiling DTO can still fail at runtime. See the [.NET 10 conflict handling](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Metadata/JsonTypeInfo.cs).

The [client DTO generator](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Generators/CommandClientDtoGenerator.cs) emits an internal sealed positional record. Every selected property becomes a constructor parameter with its source type and name. It does not copy optional defaults into those parameters. The generated client action additionally starts with a fixed `string EntityId` parameter. Source property attributes are not copied into either DTO: the server emits its own JSON-name/required attributes, and the client emits none. Source `JsonPropertyName`, `JsonConverter`, `JsonExtensionData`, and validation attributes therefore do not configure the generated DTO's naming, conversion, extension data, or validation. Reserve `EntityId` for that generated parameter: a selected command property with that name adds a duplicate parameter and fails compilation.

For a command using object-initializer mapping rather than constructor mapping, the generated server mapper must be able to construct it and assign every selected property. Each property needs an accessible setter or init accessor; a public getter-only property is still selected but its generated assignment fails compilation. Object-initializer construction must also satisfy inherited required members, which this declared-only property selection omits. Without a constructor marked `SetsRequiredMembers`, an omitted inherited `required` member can fail mapping with CS9035. See [C# required-member construction](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/required).

`CommandModel` selects the first non-static constructor with parameters without checking that it is a primary constructor. A record class's synthesized copy constructor can therefore trigger constructor mapping for a property-style record too. The server mapper emits `new Command(allSelectedProperties...)` in property enumeration order; C# overload resolution must find an accessible constructor accepting those arguments. An extra selected computed property can cause an argument-count mismatch, while reordered same-type arguments can receive unintended values. The generator does not validate that compatibility.

## Server Required-Field Inference

[`PropertyModel`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Core/Analysis/PropertyModel.cs) marks a property required when it is non-nullable and has no detected declared default. This includes non-nullable value types: an `int` without a detected default is required too.

Nullability includes annotated nullable reference types and `Nullable<T>` value types. An oblivious reference type from disabled nullable annotations has `NullableAnnotation.None`, which this analysis does not classify as nullable. Without a detected default, it is therefore marked required too. Defaults are detected from available property initializer or parameter syntax, or from a same-named constructor parameter with an explicit default. Constructor parameter matching uses ordinal, case-sensitive names. Source initializers require syntax to be available to this analysis. Properties imported from a referenced assembly have no declaring syntax for those initializers, so a compiled property initializer is not a detected default. A same-named constructor parameter's explicit default can still be read from metadata. Required-field inference can therefore differ from analysis of the command's source in the current compilation.

The server emitter then applies these rules:

- Required properties receive both the C# `required` modifier and `JsonRequired`.
- Nullable properties are emitted without either required marker or an explicit initializer.
- Other non-nullable properties receive `= default!;` and no required markers.

A detected source default therefore changes required-field inference, but this server emitter does not reproduce that default value in the DTO. The source `required` modifier is not a separate input to this inference.

For a reference property, [`JsonRequired`](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.serialization.jsonrequiredattribute?view=net-10.0) checks member presence and permits a present JSON null. The C# `required` modifier alone is not a runtime non-null check either. A deserializer can therefore produce a null reference unless separate JSON options or host validation reject it; these generated markers do not provide that rejection or protect command mapping by themselves.

## Validation Boundary

Generated required markers describe the generated DTO contract. They do not implement business rules such as positive quantities, valid account identifiers, or an allowed command at the current aggregate state; those rules belong in command handling.

The [property-model tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Generators.Core.L0Tests/Analysis/PropertyModelTests.cs) cover nullable reference/value types and required inference without defaults. The [server tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Gateway.Generators.L0Tests/CommandServerDtoGeneratorTests.cs) cover generated property names and camelCase JSON names; the [client tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Client.Generators.L0Tests/CommandClientDtoGeneratorTests.cs) assert the internal sealed record declaration and source property type text. They do not assert positional parameter-list syntax; that emission shape above is verified from the generator.

## Summary

Review the selected source properties and the defaults visible to the generator when assessing server required fields. Client positional parameters and server optional-property initialization do not preserve source defaults in the same way.

## Next Steps

- Read [Generated Application Contracts](./generated-contracts.md) for generator inputs and host wiring.
- Read [Inlet Reference](./reference.md) for client registration surfaces.
