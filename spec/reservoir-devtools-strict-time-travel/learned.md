# Learned

## Repository orientation (initial)

- DevTools integration lives in Reservoir.Blazor with ReservoirDevToolsStore and options.
- Time-travel is implemented via ReplaceStateFromJson -> ReplaceFeatureStates.

## To verify

- Current behavior when time-travel payload is missing or invalid.
- Best place to enforce strict validation and new options surface.
- Any tests that should be updated to cover strict behavior.
