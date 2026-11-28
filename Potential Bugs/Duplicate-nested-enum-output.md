# Repeated enum properties make projection generation fail

## Source location

- Project: `Inlet.Client.Generators`.
- Source file: [src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs lines 254-263](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs#L254-L263).
- Type: `ProjectionClientDtoGenerator`.
- Member: `GenerateNestedTypeDto`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

Nested type discovery generates an enum DTO once for every enum property. It does not remove duplicates or use the outer deduplication check. Two properties of the same Status enum therefore both request StatusDto.g.cs.

## Trigger

A collection element record has two properties of the same enum, such as Status First and Status Second, or two generated nested types each use that enum.

## Potential impact

Roslyn rejects the repeated source name and drops all output from that generator for the compilation. Projection DTOs that would otherwise work are also missing.

## Evidence

- [src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs lines 188-190](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs#L188-L190): Enum source hint is only dtoName.g.cs.
- [src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs lines 120-124](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs#L120-L124): The outer top-level enum path has a dedup guard that this nested path bypasses.
- Isolated probe (`nested-enum-duplicate`): Valid source; CS8785 reports duplicate StatusDto.g.cs. Exception stack points to source lines 190,262,139; outputs are empty.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- This is the same enum source identity repeated within nested discovery, distinct from skipping a needed DTO in another namespace.

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
