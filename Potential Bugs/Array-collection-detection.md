# Projection arrays lose the information needed to generate their element types

## Source location

- Project: `Inlet.Generators.Core`.
- Source file: [src/Inlet.Generators.Core/Analysis/TypeAnalyzer.cs lines 123-145](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/TypeAnalyzer.cs#L123-L145).
- Type: `TypeAnalyzer`.
- Member: `IsCollectionType`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The collection check rejects array symbols before reaching its array-specific branch. Other code still changes Entry[] into EntryDto[], but the missing collection information prevents generation of EntryDto and selection of the element mapper.

## Trigger

A projection exposes an ordinary Entry[] or domain enum array.

## Potential impact

Valid projection arrays of custom types or domain enums can generate missing types or incompatible assignments, so the generated client or endpoint code does not compile.

## Evidence

- [src/Inlet.Generators.Core/Analysis/TypeAnalyzer.cs lines 63-67](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/TypeAnalyzer.cs#L63-L67): Array DTO name still changes Entry[] into EntryDto[].
- [src/Inlet.Generators.Core/Analysis/PropertyModel.cs lines 36-60](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/PropertyModel.cs#L36-L60): Element discovery happens only when IsCollection is true.
- [src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs lines 129-143](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs#L129-L143): Nested DTO generation depends on the omitted element information.
- Isolated probe (`custom-array`): Valid input, no generator exception, generated DTO produces CS0246 for EntryDto.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- Existing old lead revalidated against current source and current isolated output.

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
