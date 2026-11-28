# Projection generation stops before producing required nested object types

## Source location

- Project: `Inlet.Client.Generators`.
- Source file: [src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs lines 127-143](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs#L127-L143).
- Type: `ProjectionClientDtoGenerator`.
- Member: `GenerateClientDto / GenerateNestedTypeDto`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

Projection type names are rewritten to DTO names, but discovery generates custom DTOs only for immediate collection elements. Direct custom properties and deeper custom fields therefore refer to DTOs that discovery never creates.

## Trigger

A projection contains Entry Entry, or `List<Entry>` where Entry contains Detail.

## Potential impact

Ordinary object-valued or nested read models can produce missing DTO types and fail compilation. The corresponding nested server mapper also lacks the necessary conversion.

## Evidence

- [src/Inlet.Generators.Core/Analysis/TypeAnalyzer.cs lines 87-92](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/TypeAnalyzer.cs#L87-L92): Custom types always receive DTO names.
- [src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs lines 232-263](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs#L232-L263): Nested properties are rewritten, but only enum properties trigger further generation.
- [src/Inlet.Gateway.Generators/ProjectionEndpointsGenerator.cs lines 146-185](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/ProjectionEndpointsGenerator.cs#L146-L185): Gateway discovery likewise generates only immediate collection element DTOs.
- [src/Inlet.Gateway.Generators/ProjectionEndpointsGenerator.cs lines 668-686](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/ProjectionEndpointsGenerator.cs#L668-L686): Nested mapper directly assigns non-enum custom properties despite changed DTO types.
- Isolated probe (`direct-custom`): Entry property yields missing EntryDto (CS0246), with valid source input.
- Isolated probe (`nested-custom-recursive`): `List<Entry>` emits EntryDto but its DetailDto field has no definition (CS0246).

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- Both triggers stem from incomplete traversal of the custom DTO dependency graph; separate array/dictionary model failures have their own reports.
- No arbitrary custom type support assumption is needed: GetDtoTypeName explicitly transforms these existing properties and RequiresMapper selects direct custom mapping.

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
