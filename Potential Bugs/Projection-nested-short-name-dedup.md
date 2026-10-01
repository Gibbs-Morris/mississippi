# Projection generation skips a required type in another feature namespace

## Source location

- Project: `Inlet.Client.Generators`.
- Source file: [src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs lines 120-143](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs#L120-L143).
- Type: `ProjectionClientDtoGenerator`.
- Member: `GenerateClientDto / Initialize`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

One set of DTO short names is shared across all projections. After generating EntryDto in the first feature namespace, discovery skips another feature's required EntryDto because its short name already appeared.

## Trigger

Two projections in distinct .Projections.First and .Projections.Second namespaces contain collection elements named Entry, or share one Entry source type.

## Potential impact

The second feature refers to a type that exists only in the first namespace and fails compilation. Different source types with the same short name can also be confused.

## Evidence

- [src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs lines 438-443](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs#L438-L443): The generatedNestedTypes set is created once for the entire projection output loop.
- [src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs lines 129-143](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs#L129-L143): Dedup key is ElementDtoTypeName, with no source identity or output namespace.
- Isolated probe (`nested-short-name-collision`): Zero input errors; only one EntryDto output; SecondProjectionDto raises CS0246 in its feature namespace.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- Separate from duplicate nested enum AddSource calls: this path suppresses required generation across namespaces.

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
