# Learned

## Repository orientation (initial)

- DevTools integration lives in Reservoir.Blazor with ReservoirDevToolsStore and options.
- Time-travel is implemented via ReplaceStateFromJson -> ReplaceStateFromJsonDocument -> ReplaceFeatureStates.

## Verified facts

- JUMP_TO_STATE/JUMP_TO_ACTION call ReplaceStateFromJson; IMPORT_STATE uses TryExtractImportedStateJson then ReplaceStateFromJson. (src/Reservoir.Blazor/ReservoirDevToolsStore.cs)
- ReplaceStateFromJsonDocument iterates current snapshot keys and deserializes per-feature; missing/invalid slices are skipped. (src/Reservoir.Blazor/ReservoirDevToolsStore.cs)
- RESET/ROLLBACK/COMMIT use ReplaceStateFromSnapshot or InitDevToolsAsync without JSON parsing. (src/Reservoir.Blazor/ReservoirDevToolsStore.cs)
- ReservoirDevToolsOptions is public and the right place to add strict-mode configuration. (src/Reservoir.Blazor/ReservoirDevToolsOptions.cs)
