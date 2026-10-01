# Omitted command values lose their declared defaults

## Source location

- Project: `Inlet.Gateway.Generators`.
- Source file: [src/Inlet.Gateway.Generators/CommandServerDtoGenerator.cs lines 121-134](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/CommandServerDtoGenerator.cs#L121-L134).
- Type: `CommandServerDtoGenerator`.
- Member: `GenerateDto`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The generator makes properties with declared defaults optional in the request DTO, but initializes them to ordinary CLR defaults instead of their declared values. The mapper then explicitly passes every DTO value to the domain constructor, bypassing its default.

## Trigger

A command declares Transfer(int Amount=42) and an HTTP request omits amount, or a saga input has a meaningful default.

## Potential impact

For a command with Amount = 42, an omitted amount becomes 0 after deserialization and mapping. This can change command or saga behaviour.

## Evidence

- [src/Inlet.Generators.Core/Analysis/PropertyModel.cs lines 45-47](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/PropertyModel.cs#L45-L47): Existing defaults cause IsRequired=false.
- [src/Inlet.Generators.Core/Analysis/PropertyModel.cs lines 184-195](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/PropertyModel.cs#L184-L195): Constructor defaults are read and formatted.
- [src/Inlet.Gateway.Generators/CommandServerDtoGenerator.cs lines 179-187](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/CommandServerDtoGenerator.cs#L179-L187): Mapping explicitly supplies every DTO property.
- [src/Inlet.Gateway.Generators/SagaControllerGenerator.cs lines 249-260](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/SagaControllerGenerator.cs#L249-L260): Saga request DTO uses the same default! path.
- Isolated probe (`command-default`): Input and generated output compile. Runtime JsonSerializer.Deserialize("{}") gives Amount=0; invoking generated mapper produces command Amount=0 instead of declared 42.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- No hosted JSON/API pipeline was run; isolated generated DTO deserialization and mapper execution demonstrate the value change.

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
