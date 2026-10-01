# MCP generation puts required parameters after optional parameters

## Source location

- Project: `Inlet.Gateway.Generators`.
- Source file: [src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs lines 282-296](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs#L282-L296).
- Type: `McpAggregateToolsGenerator`.
- Member: `GenerateCommandToolMethod`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

Property initializers become optional parameter defaults, while method parameters keep source property order. A valid class can put an initialized property before a required property, producing an order that C# methods do not allow.

## Trigger

A property-based command declares Count {get;init;}=1 before Name {get;init;} with no initializer.

## Potential impact

The generated tool method fails compilation, so its MCP project cannot build.

## Evidence

- [src/Inlet.Generators.Core/Analysis/PropertyModel.cs lines 45-47](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/PropertyModel.cs#L45-L47): Initialized properties are recognized as having defaults.
- [src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs lines 291-293](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs#L291-L293): Default is appended to each parameter without reordering.
- Isolated probe (`mcp-optional-order`): Valid command class with zero input errors; generated tool fails only CS1737 (optional parameters before required).

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- This uses a class, so record copy-constructor classification cannot account for the failure.

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
