# Line breaks leave generated summary text outside its comment

## Source location

- Project: `Inlet.Generators.Core`.
- Source file: [src/Inlet.Generators.Core/Emit/SourceBuilder.cs lines 117-124](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Emit/SourceBuilder.cs#L117-L124).
- Type: `SourceBuilder`.
- Member: `AppendSummary`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

AppendSummary prefixes only the first line with ///. Subsequent lines from a valid multiline description are emitted as bare text and are parsed as C# code.

## Trigger

A generated MCP description contains a newline; the shared builder receives that description directly.

## Potential impact

Generated code fails compilation. This still fails when attribute-string escaping is handled, because the comment writer is a separate cause.

## Evidence

- [src/Inlet.Generators.Core/Emit/SourceBuilder.cs lines 122-124](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Emit/SourceBuilder.cs#L122-L124): Interpolates the entire summary into one prefixed AppendLine call.
- [src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs lines 255-259](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs#L255-L259): A public metadata description goes directly into AppendSummary.
- [src/Inlet.Gateway.Generators/McpProjectionToolsGenerator.cs lines 144-147](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/McpProjectionToolsGenerator.cs#L144-L147): Projection descriptions also enter the shared summary builder.
- Isolated probe (`sourcebuilder-multiline-summary`): Probe calls the unchanged SourceBuilder.AppendSummary with First line newline Second line and emits a class. No attribute literal is emitted; generated output has three compiler errors from bare Second line.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- Independent from EscapeForStringLiteral: this probe contains no string-valued generated attribute.

## Confidence

**High**. Exact shared builder source and an isolated generated-output compiler probe reproduce the defect.
