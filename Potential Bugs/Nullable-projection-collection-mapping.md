# A null projection collection makes the generated mapper throw

## Source location

- Project: `Inlet.Gateway.Generators`.
- Source file: [src/Inlet.Gateway.Generators/ProjectionEndpointsGenerator.cs lines 403-407](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/ProjectionEndpointsGenerator.cs#L403-L407).
- Type: `ProjectionEndpointsGenerator`.
- Member: `GenerateMapper`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The generator knows a collection property is nullable but calls its collection mapper without a null check. The repository's EnumerableMapper rejects null input.

## Trigger

A projection contains `List<Entry>?` Entries and legitimately uses null to represent unavailable/absent entries.

## Potential impact

Reading a valid projection with a null collection throws ArgumentNullException during mapping, so the HTTP read can fail.

## Evidence

- [src/Inlet.Generators.Core/Analysis/PropertyModel.cs lines 40-44](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/PropertyModel.cs#L40-L44): Source nullability is available in the model.
- [src/Common.Abstractions/Mapping/EnumerableMapper.cs lines 35-40](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Common.Abstractions/Mapping/EnumerableMapper.cs#L35-L40): Real mapper explicitly throws when its input is null.
- Isolated probe (`server-nullable-list`): Input and output compile. Probe compiles the unchanged EnumerableMapper.cs alongside emitted projection mapper; mapping a null Entries property throws ArgumentNullException with ParamName input.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- Execution used real repository EnumerableMapper code and generated mapper, with small DI/controller interface stubs.
- Direct nullable custom-property mapping uses the same unconditional call pattern at source lines409-412, but direct custom discovery has its own earlier failure.

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
