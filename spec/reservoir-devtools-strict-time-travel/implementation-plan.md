# Implementation Plan (Revised)

## Step-by-step checklist

1. Add strict rehydration option.
	- Add `StrictStateRehydration` boolean to ReservoirDevToolsOptions (default false).

2. Implement strict validation.
	- In ReplaceStateFromJsonDocument, when strict mode is enabled, require JSON to include all current feature keys and successful deserialization for each.
	- If validation fails, do not apply any state replacement.

3. Update tests.
	- Add Reservoir.Blazor L0 tests that validate strict mode rejects partial/invalid payloads and accepts full payloads.

4. Verification.
	- Run L0 tests for Reservoir.Blazor.

## Files to touch

- src/Reservoir.Blazor/ReservoirDevToolsOptions.cs
- src/Reservoir.Blazor/ReservoirDevToolsStore.cs
- tests/Reservoir.Blazor.L0Tests/

## Risks and mitigations

- Risk: stricter behavior could surprise users relying on partial payloads.
  - Mitigation: strict mode is opt-in and default remains best-effort.

## Test plan

- pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1 -TestLevels @('L0Tests')
