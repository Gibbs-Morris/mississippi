# Verification

## Claim list

1. Current time-travel replaces per-feature slices and ignores invalid/missing slices.
2. Strict mode can be implemented via an option without changing IStore.
3. Strict mode should gate JUMP/IMPORT/RESET/ROLLBACK handling.

## Verification questions (initial)

1. Where exactly does JSON rehydration occur in ReservoirDevToolsStore?
2. Which DevTools message types trigger ReplaceStateFromJson?
3. Is there any existing option surface that can host strict-mode configuration?
4. Do current tests cover time-travel behavior or only registration/snapshot hooks?
5. Will strict validation require full snapshot comparison or only JSON validity per slice?

## Answers (evidence-based)

1. JSON rehydration occurs in ReplaceStateFromJson -> ReplaceStateFromJsonDocument in ReservoirDevToolsStore. (src/Reservoir.Blazor/ReservoirDevToolsStore.cs)
2. JUMP_TO_STATE, JUMP_TO_ACTION, and IMPORT_STATE trigger ReplaceStateFromJson. (src/Reservoir.Blazor/ReservoirDevToolsStore.cs)
3. ReservoirDevToolsOptions is the existing public options surface for DevTools configuration. (src/Reservoir.Blazor/ReservoirDevToolsOptions.cs)
4. Existing tests cover snapshot/replace hooks and registrations only; no tests cover time-travel rehydration logic. (tests/Reservoir.L0Tests/StoreTests.cs, tests/Reservoir.Blazor.L0Tests/ReservoirDevToolsRegistrationsTests.cs)
5. Strict validation can be implemented by requiring JSON to include all current feature keys and successful deserialization for each slice.
