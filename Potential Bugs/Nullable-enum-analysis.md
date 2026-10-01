# Nullable enum properties produce missing types or invalid mappings

## Source location

- Project: `Inlet.Generators.Core`.
- Source file: [src/Inlet.Generators.Core/Analysis/TypeAnalyzer.cs lines 153-156](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/TypeAnalyzer.cs#L153-L156).
- Type: `TypeAnalyzer`.
- Member: `IsEnumType`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

Enum analysis does not unwrap `Nullable<T>`, although DTO naming still changes the underlying enum's name. Top-level discovery can omit the enum DTO. Nested discovery does unwrap it, but nested mapping can still directly assign the original nullable enum to the different DTO enum.

## Trigger

A projection has Status? State, or a list element record contains a nullable domain enum.

## Potential impact

Valid nullable enum read models fail generated compilation because the DTO enum is missing or the generated assignment uses incompatible types.

## Evidence

- [src/Inlet.Generators.Core/Analysis/PropertyModel.cs lines 34-44](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/PropertyModel.cs#L34-L44): Records nullable state independently, but IsEnum uses the non-unwrapping predicate.
- [src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs lines 275-288](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs#L275-L288): Top-level enum discovery depends on PropertyModel.IsEnum/ElementIsEnum.
- [src/Inlet.Gateway.Generators/ProjectionEndpointsGenerator.cs lines 301-315](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/ProjectionEndpointsGenerator.cs#L301-L315): Nested enum DTO discovery unwraps nullable types.
- [src/Inlet.Gateway.Generators/ProjectionEndpointsGenerator.cs lines 678-686](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/ProjectionEndpointsGenerator.cs#L678-L686): Nested mapper tests the original nullable type then falls through to direct assignment.
- Isolated probe (`nullable-enum`): Zero input errors; no StatusDto output and CS0246.
- Isolated probe (`server-nested-nullable-enum`): StatusDto exists, but EntryDtoMapper gives CS0266 converting Status? to StatusDto?.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- The two emitted symptoms must be checked when correcting nullable type analysis: discovery and conversion both need the underlying enum and preserved null values.

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
