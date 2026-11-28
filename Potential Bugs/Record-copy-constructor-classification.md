# Record copy constructor is mistaken for a property-value constructor

## Source location

- Project: `Inlet.Generators.Core`.
- Source file: [src/Inlet.Generators.Core/Analysis/CommandModel.cs lines 43-46](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/CommandModel.cs#L43-L46).
- Type: `CommandModel`.
- Member: `CommandModel constructor`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

Any record constructor with parameters makes the generator classify the record as positional. A property-based record has a compiler-generated copy constructor, so it can be misclassified even without a positional constructor.

## Trigger

A [GenerateCommand] record declares public int Amount {get;init;} and no positional constructor; the same form is used for a saga input.

## Potential impact

The mapper tries to construct the record using its property values, but no matching public constructor exists. Generated command, saga, or MCP code can fail compilation.

## Evidence

- [src/Inlet.Gateway.Generators/CommandServerDtoGenerator.cs lines 175-187](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/CommandServerDtoGenerator.cs#L175-L187): Positional classification selects constructor emission.
- [src/Inlet.Gateway.Generators/SagaControllerGenerator.cs lines 471-480](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/SagaControllerGenerator.cs#L471-L480): Saga server repeats the constructor selection.
- [src/Inlet.Gateway.Generators/SagaControllerGenerator.cs lines 206-214](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/SagaControllerGenerator.cs#L206-L214): Saga mapping uses the erroneous positional path.
- [src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs lines 228-230](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/McpAggregateToolsGenerator.cs#L228-L230): MCP commands repeat the record test.
- [src/Inlet.Gateway.Generators/McpSagaToolsGenerator.cs lines 612-620](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/McpSagaToolsGenerator.cs#L612-L620): MCP saga inputs repeat it.
- [tests/Inlet.Generators.Core.L0Tests/Integration/RoslynCompilationTests.cs lines 153-187](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/tests/Inlet.Generators.Core.L0Tests/Integration/RoslynCompilationTests.cs#L153-L187): Existing test calls this property-based record positional, masking the erroneous assumption.
- Isolated probe (`command-copy-constructor`): Source has zero errors; generated TransferDtoMapper raises CS1729 for the single field argument.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- Do not conflate with additional non-constructor properties on a genuinely positional record, documented separately.

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
