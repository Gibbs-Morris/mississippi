# Line breaks in referenced command defaults produce invalid generated literals

## Source location

- Project: `Inlet.Generators.Core`.
- Source file: [src/Inlet.Generators.Core/Analysis/PropertyModel.cs lines 235-248](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/PropertyModel.cs#L235-L248).
- Type: `PropertyModel`.
- Member: `FormatDefaultValue`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

Defaults read from a referenced assembly are decoded values. The formatter escapes quotes and backslashes but leaves line breaks in ordinary string and character literals. The generated parameter default can therefore contain invalid C# text.

## Trigger

A generated MCP command comes from a referenced domain assembly and declares an optional string default such as "first\nsecond"; a newline char default follows the same formatter branch.

## Potential impact

A valid compiled domain command can make its consuming MCP project fail compilation. The executed string case produced a newline-in-constant error; the character case follows the adjacent formatter branch.

## Evidence

- [src/Inlet.Generators.Core/Analysis/PropertyModel.cs lines 184-195](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/PropertyModel.cs#L184-L195): Referenced-domain constructor metadata defaults use the literal formatter.
- [src/Inlet.Generators.Core/Analysis/PropertyModel.cs lines 235-248](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/PropertyModel.cs#L235-L248): String/char branches do not encode newline/control characters.
- [src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs lines 438-447](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs#L438-L447): Uses PropertyModel.DefaultValueExpression directly.
- Isolated probe (`referenced-mcp-string-default`): Domain optional string source compiles. Generated AccountMcpTools.g.cs lines53-54 split note="first newline second" across physical lines and raise CS1010; no multiline MCP metadata was supplied.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- Separate from the MCP metadata EscapeForStringLiteral helper and from SourceBuilder.AppendSummary: this probe uses ordinary single-line tool metadata and exercises the core metadata-default formatter.
- The char control-character consequence follows the adjacent exact branch; the string path was independently executed.

## Confidence

**High**. A separate domain assembly compiles without errors; its metadata default is rendered by the unchanged production formatter and causes an isolated generated compilation error.
