# Line breaks in MCP descriptions produce invalid generated string literals

## Source location

- Project: `Inlet.Gateway.Generators`.
- Source file: [src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs lines 152-155](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs#L152-L155).
- Type: `McpAggregateToolsGenerator`.
- Member: `EscapeForStringLiteral`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

MCP text escaping handles quotes and backslashes but leaves actual line breaks in decoded attribute text. It inserts that text into ordinary C# quoted strings, where literal line breaks are invalid.

## Trigger

An MCP command title or parameter description contains an escaped newline or multiline C# string; projection and saga metadata use the same escaping strategy.

## Potential impact

A valid multiline tool title or parameter description can make generated MCP code fail compilation. The summary-comment path has a separate defect documented in its own report.

## Evidence

- [src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs lines 277](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs#L277): Description is embedded into an ordinary quoted DescriptionAttribute literal.
- [src/Inlet.Gateway.Generators/McpProjectionToolsGenerator.cs lines 56-59](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/McpProjectionToolsGenerator.cs#L56-L59): Projection metadata has the same incomplete escape function.
- [src/Inlet.Gateway.Generators/McpSagaToolsGenerator.cs lines 125-128](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/McpSagaToolsGenerator.cs#L125-L128): Saga metadata has the same incomplete escape function.
- Isolated probe (`mcp-multiline-description`): Input attribute Description="First line\nSecond line" is valid; generated output raises CS1010 plus bare-text parse/type errors.
- Isolated probe (`mcp-multiline-title`): Valid Title metadata only, with default single-line summary, reproduces CS1010 and attribute parse errors independently of summary emission.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- Runtime text/attribute support is unrestricted by the public description contracts; a newline in descriptive prose is realistic.
- Multiline summary comment emission is a separate SourceBuilder cause with its own report and isolated probe.

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
