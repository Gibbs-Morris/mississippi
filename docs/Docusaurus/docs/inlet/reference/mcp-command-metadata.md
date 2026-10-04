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

Every generated command method receives these four values on its `McpServerTool` attribute. A nonempty title is included there too. The tool name is generated from the aggregate's tool prefix and command type name, converted to snake_case; the title does not rename it.

A null description falls back to a sentence naming the command and aggregate. An explicitly empty description is retained. The method receives a `Description` attribute with the selected text.

## Parameter Descriptions

[`GenerateMcpParameterDescription`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateMcpParameterDescriptionAttribute.cs) accepts description text on a command property or constructor parameter. The generator first collects descriptions from public instance properties with getters. For a record, it then selects the first non-static constructor with parameters and fills descriptions not already supplied by properties. This includes an explicit parameterized record constructor, rather than only positional-record syntax; parameter names must match the selected property names for emission.

Null or empty description text is ignored; whitespace text is retained. At emission, a selected property's name is used to look up its description. Without a matching custom description, the generator derives human-readable text from that name. The generated method parameter name is the camelCase form of the property name.

The entity ID parameter has the fixed description `The entity identifier`. A property or constructor parameter description does not replace it.

## Runtime Boundary

The generated method constructs the command, resolves its aggregate grain by entity ID, and calls `ExecuteAsync` with the cancellation token. Changing a hint does not change that path, prevent writes, deduplicate repeated calls, or add authorization checks.

Descriptions can explain constraints, but this metadata does not enforce them. Choose text and hints that agree with the actual command handler and host authorization behavior.

The [generator tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Gateway.Generators.L0Tests/McpAggregateToolsGeneratorTests.cs) cover default hints, metadata overrides, generated descriptions, and property/positional-parameter descriptions. These are generated-source assertions rather than proof of runtime permissions or idempotency.

## Summary

Use command metadata to describe the tool accurately, and parameter descriptions to explain its inputs. Keep those descriptions aligned with the aggregate behavior and the host's access controls.

## Next Steps

- Read [Generated Application Contracts](./generated-contracts.md) for generator inputs and registration boundaries.
- Read [Inlet Reference](./reference.md) for the wider subsystem surface.
