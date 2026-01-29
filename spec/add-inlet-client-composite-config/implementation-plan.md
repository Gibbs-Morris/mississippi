# Implementation plan

## Changes from initial draft
- Confirmed no docs references to update; keep docs plan only.
- Confirmed behavior via generator output; configuration invoked after defaults.

## Steps
1. Update composite generator to emit an overload with optional SignalR configuration.
2. Ensure default `Add{App}Inlet()` forwards to the overload with `null`.
3. Apply configuration after defaults (hub path + projection DTO registration).
4. Add generator L0 test asserting overload and invocation.
5. Run `test-project-quality.ps1 -TestProject Inlet.Client.Generators.L0Tests -SkipMutation`.

## Files to touch
- `src/Inlet.Client.Generators/InletClientCompositeGenerator.cs`
- `tests/Inlet.Client.Generators.L0Tests/InletClientCompositeGeneratorTests.cs`

## Docs plan (no changes in this task)
- Add a section under Docusaurus docs explaining the generated `Add{App}Inlet()` and the optional configuration overload.
- Include manual configuration path with `AddInletClient` + `AddInletBlazorSignalR` for advanced scenarios.
- Reference `RegisterProjectionDtos` and `ScanProjectionDtos` as the two registration approaches.

## API/contract changes
- Adds a new overload to the generated `Add{App}Inlet` extension method (backwards compatible).

## Observability
- None.

## Test plan
- `pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Inlet.Client.Generators.L0Tests -SkipMutation`

## Rollout
- No runtime behavior change; merge-only.

## Validation/monitoring checklist
- Ensure generated code compiles in sample app (verified by L0 generator test).
- Ensure tests remain green.

## Risks and mitigations
- Risk: configuration overwritten by defaults. Mitigation: invoke configuration after defaults.
