# Referenced enum defaults are emitted as numbers of the wrong type

## Source location

- Project: `Inlet.Generators.Core`.
- Source file: [src/Inlet.Generators.Core/Analysis/PropertyModel.cs lines 270-277](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/PropertyModel.cs#L270-L277).
- Type: `PropertyModel`.
- Member: `FormatDefaultValue`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

For defaults from a referenced assembly, the formatter handles a boxed long value before checking whether its declared type is an enum. A Mode:long default of Mode.Fast can therefore be emitted as 5L instead of an enum expression.

## Trigger

A generated MCP command in a referenced domain assembly declares Transfer(Mode Mode=Mode.Fast), where enum Mode:long has Fast=5.

## Potential impact

The generated MCP parameter Mode mode = 5L fails compilation even though the original compiled command is valid.

## Evidence

- [src/Inlet.Generators.Core/Analysis/PropertyModel.cs lines 184-195](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/PropertyModel.cs#L184-L195): Metadata-backed constructor defaults call FormatDefaultValue(parameter.ExplicitDefaultValue,parameter.Type).
- [src/Inlet.Generators.Core/Analysis/PropertyModel.cs lines 270-277](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/PropertyModel.cs#L270-L277): The long-value branch returns before the enum cast branch.
- [src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs lines 291-293](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs#L291-L293): The formatted default is emitted in the tool method signature.
- Isolated probe (`referenced-mcp-enum-default`): Domain is compiled to a metadata reference. Consumer input has zero errors; generated AccountMcpTools.g.cs line53 has Mode mode=5L and raises CS1750.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- This is separate from generated enum DTO underlying-type loss: this probe generates only MCP tools, emits no enum DTO, and uses the existing domain enum.
- The equivalent syntax-backed source default retains Mode.Fast and can mask this metadata-only path.

## Confidence

**High**. A separate domain assembly compiles without errors; its metadata default is rendered by the unchanged production formatter and causes an isolated generated compilation error.
