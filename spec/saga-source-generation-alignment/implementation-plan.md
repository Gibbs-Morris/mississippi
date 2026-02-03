# Implementation plan

## Initial outline
1. Inventory saga DTO/mapper/reducer patterns and aggregate/projection generator patterns.
2. Assess generic attribute support with repo target frameworks and language version.
3. Design saga source generation changes aligned to existing generator conventions.
4. Implement generator updates and migrate manual saga DTO/mapper/reducers.
5. Add/update tests and validation.

## Assumptions
- Generator additions can target the same test projects used for aggregate/projection generators.
- Samples can be updated to consume generated saga DTOs/mappers without functional changes.

## Unknowns
- Required changes in sample projects or build settings.
- Whether any existing saga reducer patterns are not suitable for generation.

## Revised plan (detailed)
### 0. Confirm scope and conventions
- Verify all saga status reducers under Spring projection and note required properties on projection state.
- Confirm projection generator mapping rules for enums and custom types in Inlet.Generators.Core.
- Decide whether to add new attributes for saga status generation or reuse existing ones (document decision).

### 1. Generic attribute decision
- Confirm generic attributes are supported by LangVersion 14.0 and choose a path:
	- Option A: Add generic attribute `GenerateSagaEndpointsAttribute<TInput>` while keeping existing non-generic
		attribute for backwards compatibility.
	- Option B: Keep existing attribute (document rationale if generic attribute is not adopted).
- If Option A, update generator discovery logic to honor both attribute forms.

### 2. Saga DTO and mapper generation
- Extend projection generators to generate enum DTOs for top-level enum properties (not only nested types).
- Generate mappers for top-level custom types (e.g., `SagaPhase` to `SagaPhaseDto`) alongside projection mapper
	registrations to ensure runtime resolution without manual mapper classes.
- Ensure client DTO generator emits enum DTOs for top-level enum properties.

### 3. Saga status reducer generation
- Introduce a generator (likely in Inlet.Silo.Generators or new generator project) that emits standard
	saga status reducers for projections annotated as saga-status projections (new attribute).
- Generated reducers should mirror existing Spring reducers (phase, step index, error fields, timestamps).
- Keep reducers internal and in the projection namespace’s Reducers sub-namespace to match conventions.

### 4. Sample migrations
- Update MoneyTransferStatusProjection to use `SagaPhase` enum (instead of string) so generated DTOs/mappers
	can produce `SagaPhaseDto`.
- Remove manual `SagaPhaseDto` and `SagaPhaseDtoMapper` from Spring.Server and Spring.Client if generated.
- Remove manual saga status reducers if generator provides equivalents.

### 5. Tests and validation
- Update/extend generator tests to cover:
	- top-level enum DTO generation (server + client)
	- generated enum mappers and registrations
	- saga status reducer generation and namespace placement
	- generic attribute discovery (if adopted)
- Run sample L0 tests for Spring domain and generator L0 tests.

## Files/modules likely to change
- src/Inlet.Server.Generators/ProjectionEndpointsGenerator.cs
- src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs
- src/Inlet.Generators.Abstractions/GenerateSagaEndpointsAttribute.cs (if generic attribute added)
- src/Inlet.Silo.Generators/(new generator or extend existing)
- src/Inlet.Generators.Core/Analysis/TypeAnalyzer.cs (enum handling if needed)
- samples/Spring/Spring.Domain/Projections/MoneyTransferStatus/...
- samples/Spring/Spring.Server/Controllers/Projections/...
- samples/Spring/Spring.Client/Features/MoneyTransferStatus/...
- tests/Inlet.*.Generators.L0Tests/...
- samples/Spring/Spring.Domain.L0Tests/...

## Test plan (commands)
- Generator tests:
	- pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Inlet.Server.Generators.L0Tests -SkipMutation
	- pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Inlet.Client.Generators.L0Tests -SkipMutation
	- pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Inlet.Silo.Generators.L0Tests -SkipMutation
- Samples domain tests:
	- pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Spring.Domain.L0Tests -SkipMutation
- Full sample unit tests (if needed):
	- pwsh ./eng/src/agent-scripts/unit-test-sample-solution.ps1

## Rollout plan
- Keep backward compatibility by retaining existing attribute usage when adding generic attributes.
- Remove manual saga DTO/mapper/reducers only after generated output passes tests.

## Risks + mitigations
- **Public API change risk**: introducing a generic attribute expands public surface.
	- Mitigation: keep existing attribute, document usage, add tests to ensure both paths work.
- **Generator output mismatch**: generated saga reducers might not match sample behavior.
	- Mitigation: align to existing Spring reducers and re-run L0 tests.

## Changes from initial draft
- Added explicit generator changes for top-level enum DTOs/mappers.
- Added new saga status reducer generation step and sample migration steps.
