# Generated projection DTOs omit inherited public properties

## Source location

- Project: `Inlet.Generators.Core`.
- Source file: [src/Inlet.Generators.Core/Analysis/ProjectionModel.cs lines 60-66](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/ProjectionModel.cs#L60-L66).
- Type: `ProjectionModel`.
- Member: `ProjectionModel constructor`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

Property discovery reads only members declared on the marked type. It does not walk its base types. Public readable properties inherited from a base record or class therefore disappear from the generated contract without a diagnostic.

## Trigger

A marked projection derives from a shared base record with Id or other public state.

## Potential impact

Generation can compile while API responses and client DTOs silently omit inherited fields such as Id. Command and saga input discovery use the same approach and can also omit inherited input.

## Evidence

- [src/Inlet.Generators.Core/Analysis/CommandModel.cs lines 49-55](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/CommandModel.cs#L49-L55): Command model uses declared-member-only enumeration.
- [src/Inlet.Client.Generators/SagaClientGeneratorHelper.cs lines 251-257](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/SagaClientGeneratorHelper.cs#L251-L257): Saga input property discovery repeats the pattern.
- [src/Inlet.Gateway.Generators/SagaControllerGenerator.cs lines 474-480](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/SagaControllerGenerator.cs#L474-L480): Server saga input DTO/mapping also sees only declared fields.
- [src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs lines 232-235](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs#L232-L235): Nested DTO property discovery repeats it.
- Isolated probe (`inherited-projection`): Valid DerivedProjection:Base with Base.Id and Derived.Own yields DerivedProjectionDto(int Own), no Id, without any diagnostic.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- Verify inherited public API state is supported; no diagnostics or attribute contract excludes ordinary inheritance. The loss itself is reproduced.

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
