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
