# Projection dictionary mapping examines the key instead of the value

## Source location

- Project: `Inlet.Generators.Core`.
- Source file: [src/Inlet.Generators.Core/Analysis/TypeAnalyzer.cs lines 30-41](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/TypeAnalyzer.cs#L30-L41).
- Type: `TypeAnalyzer`.
- Member: `GetCollectionElementType`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

Collection analysis uses the first generic argument as the element type. In `Dictionary<string,Entry>`, that is string rather than Entry. The DTO type still changes Entry into EntryDto, but discovery and mapper selection examine the key and omit the required value type and conversion.

## Trigger

A projection exposes `Dictionary<string,Entry>` or an immutable dictionary with custom values.

## Potential impact

Generated code can refer to an EntryDto that was never generated and cannot assign the original dictionary to the changed DTO type. Valid dictionary projections then fail compilation.

## Evidence

- [src/Inlet.Generators.Core/Analysis/TypeAnalyzer.cs lines 70-78](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/TypeAnalyzer.cs#L70-L78): All generic type arguments are recursively rewritten to DTO names.
- [src/Inlet.Generators.Core/Analysis/TypeAnalyzer.cs lines 212-242](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/TypeAnalyzer.cs#L212-L242): Mapper selection depends on the first generic argument.
- [src/Inlet.Generators.Core/Analysis/PropertyModel.cs lines 52-60](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/PropertyModel.cs#L52-L60): Nested element metadata copies that first argument.
- Isolated probe (`dictionary-value`): Zero input errors; generated `Dictionary<string,EntryDto>` references nonexistent EntryDto and raises CS0246.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- Dictionary requires preserving KeyValuePair/key/value semantics; it is separate from arrays returning false.

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
