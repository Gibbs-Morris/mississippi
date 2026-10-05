---
id: mcp-command-metadata
title: Generated MCP Command Metadata
description: Reference command tool hints, titles, descriptions, and parameter-description precedence.
sidebar_position: 12
sidebar_label: Generated MCP Command Metadata
---

# Generated MCP Command Metadata

## Overview

`McpAggregateToolsGenerator` emits MCP command tools with descriptions and behavioral annotations. These values help a tool caller understand the command; the generated method still executes the aggregate command.

## Applies To

- Aggregates marked with `GenerateMcpToolsAttribute`
- Commands marked with `GenerateCommandAttribute` in the aggregate namespace's immediate `Commands` subnamespace
- `GenerateMcpToolMetadataAttribute` and `GenerateMcpParameterDescriptionAttribute`

## Command Tool Hints

Apply [`GenerateMcpToolMetadata`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateMcpToolMetadataAttribute.cs) to the command type to supply a title, description, or behavioral hints. The [generator](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs) uses these defaults when no override is supplied:

| Hint | Default |
|------|---------|
| `Destructive` | `true` |
| `ReadOnly` | `false` |
| `Idempotent` | `false` |
| `OpenWorld` | `false` |

Every generated command method receives these four values on its `McpServerTool` attribute. A nonempty title is included there too. The tool name is generated from the aggregate's tool prefix and command type name by lowercasing uppercase letters and inserting underscores before them; the title does not rename it. This conversion preserves other characters and performs no prefix validation. Use letters, digits, and underscores for predictable snake_case names; a prefix containing spaces or punctuation is not sanitized into that form and must meet the host/client's tool-name requirements.

A null description falls back to a sentence naming the command and aggregate. An explicitly empty description is retained. The method receives a `Description` attribute with the selected text. `GenerateMcpToolsAttribute.Description` is a separate aggregate-level property. The generator currently reads and stores it but does not emit it; the tools class receives a fixed summary. It does not replace command descriptions on the generated methods.

## Parameter Descriptions

[`GenerateMcpParameterDescription`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateMcpParameterDescriptionAttribute.cs) accepts description text on a command property or constructor parameter. The generator first collects descriptions from public instance properties declared on the command type with getters. Inherited properties are excluded from both descriptions and generated MCP parameters. For a record, it then selects the first non-static constructor with parameters and fills descriptions not already supplied by properties. This includes an explicit parameterized record constructor, rather than only positional-record syntax; parameter names must match the selected property names for emission.

Null or empty description text is ignored; whitespace text is retained. At emission, a selected property's name is used to look up its description. Without a matching custom description, the generator derives human-readable text from that name. The generated method parameter name is the camelCase form of the property name.

Keep description and title strings on one line. The generator escapes backslashes and quotes for ordinary C# string literals, but retains carriage returns and line feeds; those characters can make the emitted source invalid. Command descriptions also enter an XML documentation summary without XML escaping. Raw `&` or `<` can produce malformed-XML compiler warnings and fail a warnings-as-errors build that emits documentation; use plain text without those characters for this generator.

The entity ID parameter has the fixed description `The entity identifier`. A property or constructor parameter description does not replace it. Command property names whose camelCase form is `entityId` or `cancellationToken` collide with fixed generated parameters. In particular, a public `EntityId` property emits a duplicate parameter and makes the generated method fail compilation; reserve those names for the generated parameters. Also avoid names whose generated first-letter-lowercase form is a C# keyword: `Event`, `Class`, and `Namespace` emit unescaped `event`, `class`, and `namespace` parameters and fail compilation.

A selected parameterized record constructor is invoked with every declared public readable property in property enumeration order. The generator does not validate constructor count, order, or compatible argument types. The constructor must accept that complete sequence: mismatches can fail compilation, while reordered same-type arguments can supply the wrong values. Name matching for descriptions alone does not establish constructor compatibility.

For property-based commands, generated construction uses `new() { ... }`. The command needs an accessible parameterless constructor and every selected property needs an accessible setter or init accessor. A readable getter alone does not satisfy construction; missing constructors or writable accessors make the generated method fail compilation.

## Runtime Boundary

The generated method constructs the command, resolves its aggregate grain by entity ID, and calls `ExecuteAsync` with the cancellation token. Changing a hint does not change that path, prevent writes, deduplicate repeated calls, or add authorization checks.

Descriptions can explain constraints, but this metadata does not enforce them. Choose text and hints that agree with the actual command handler and host authorization behavior.

The [generator tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Gateway.Generators.L0Tests/McpAggregateToolsGeneratorTests.cs) cover default hints, metadata overrides, generated descriptions, and property/positional-parameter descriptions. These are generated-source assertions rather than proof of runtime permissions or idempotency.

## Summary

Use command metadata to describe the tool accurately, and parameter descriptions to explain its inputs. Keep those descriptions aligned with the aggregate behavior and the host's access controls.

## Next Steps

- Read [Generated Application Contracts](./generated-contracts.md) for generator inputs and registration boundaries.
- Read [Inlet Reference](./reference.md) for the wider subsystem surface.
