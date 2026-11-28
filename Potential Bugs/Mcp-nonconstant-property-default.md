# Runtime property defaults become invalid MCP parameter defaults

## Source location

- Project: `Inlet.Gateway.Generators`.
- Source file: [src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs lines 438-447](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs#L438-L447).
- Type: `McpAggregateToolsGenerator`.
- Member: `GetDefaultValueExpression`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The generator copies any property initializer into an optional method parameter. A property can validly use Guid.NewGuid() or object creation, but an optional parameter cannot use those runtime expressions.

## Trigger

A property-based command uses Guid.NewGuid(), new object/collection creation, or another runtime initializer.

## Potential impact

A valid command class can produce an MCP tool method that fails compilation, even when it has only one property.

## Evidence

- [src/Inlet.Generators.Core/Analysis/PropertyModel.cs lines 208-210](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/PropertyModel.cs#L208-L210): Extracts arbitrary property initializer syntax prefixed by =.
- [src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs lines 291-293](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs#L291-L293): Copies that expression into the parameter declaration.
- Isolated probe (`mcp-nonconstant-default`): Source class with Id=Guid.NewGuid() has zero errors; generated method raises CS1736 for id.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- Separate from optional parameter ordering: this fails with only one command field.

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
