# Generated projection mapping returns a list for incompatible collection types

## Source location

- Project: `Inlet.Gateway.Generators`.
- Source file: [src/Inlet.Gateway.Generators/ProjectionEndpointsGenerator.cs lines 403-407](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/ProjectionEndpointsGenerator.cs#L403-L407).
- Type: `ProjectionEndpointsGenerator`.
- Member: `GenerateMapper`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The mapper preserves a collection's type in its DTO property, but converts mapped elements to List unless the source is ImmutableArray. A List cannot be assigned to types such as ImmutableList or HashSet.

## Trigger

A projection exposes `ImmutableList<Entry>`, `ImmutableHashSet<Entry>`, `HashSet<Entry>`, or another recognized collection not assignable from `List<EntryDto>`.

## Potential impact

Valid custom-element collections can make the generated endpoint mapper fail compilation.

## Evidence

- [src/Inlet.Generators.Core/Analysis/TypeAnalyzer.cs lines 70-78](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/TypeAnalyzer.cs#L70-L78): DTO rendering retains the source collection generic name.
- [src/Inlet.Generators.Core/Analysis/TypeAnalyzer.cs lines 250-263](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/TypeAnalyzer.cs#L250-L263): All System.Collections generic collections, including ImmutableList, are recognized.
- [src/Inlet.Generators.Core/Analysis/PropertyModel.cs lines 52-60](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/PropertyModel.cs#L52-L60): Only ImmutableArray receives a special shape flag.
- Isolated probe (`server-immutable-list`): Zero input errors; sole generated-output failure is CS0029 converting `List<EntryDto>` to `ImmutableList<EntryDto>`.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- Separate from array detection (no mapping selected) and dictionary key/value analysis (wrong element selection).

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
