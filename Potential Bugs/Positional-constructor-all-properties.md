# Generated command mapping passes properties that are not constructor arguments

## Source location

- Project: `Inlet.Gateway.Generators`.
- Source file: [src/Inlet.Gateway.Generators/CommandServerDtoGenerator.cs lines 175-187](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/CommandServerDtoGenerator.cs#L175-L187).
- Type: `CommandServerDtoGenerator`.
- Member: `GenerateMapper`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

For a positional record, the mapper builds constructor arguments from every public readable property. It does not use only the constructor's parameters. A computed or extra property therefore becomes an extra argument.

## Trigger

A valid command record Transfer(int Amount) additionally exposes a computed property Computed => Amount*2, or an additional init-only property.

## Potential impact

A valid record such as Transfer(int Amount) with a computed property produces a constructor call with too many arguments and fails compilation. MCP and saga mapping contain the same pattern.

## Evidence

- [src/Inlet.Generators.Core/Analysis/CommandModel.cs lines 43-55](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/CommandModel.cs#L43-L55): Stores constructor classification and all public properties independently.
- [src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs lines 301-306](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs#L301-L306): MCP constructor arguments use every property.
- [src/Inlet.Gateway.Generators/SagaControllerGenerator.cs lines 210-214](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/SagaControllerGenerator.cs#L210-L214): Saga constructor mapping uses every input property.
- Isolated probe (`command-extra-property`): Zero input errors; mapper calls a two-argument constructor although Transfer declares one parameter and raises CS1729.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- This trigger has a real positional constructor; excluding copy constructors alone will not fix it.

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
