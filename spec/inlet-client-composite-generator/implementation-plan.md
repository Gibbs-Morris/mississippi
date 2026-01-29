# Implementation Plan

## Status: DRAFT - pending verification answers

## Phase 1: Attribute Definition

1. **Add `GenerateInletClientCompositeAttribute`** to `src/Inlet.Generators.Abstractions/`
   - Assembly-level attribute with `AppName` (required) and `HubPath` (optional, default `/hubs/inlet`)

## Phase 2: Generator Implementation

2. **Create `InletClientCompositeGenerator`** in `src/Inlet.Client.Generators/`
   - Trigger on assembly attribute `[GenerateInletClientComposite]`
   - Discover commands with `[GenerateCommand]` to derive aggregate names
   - Emit composite registration class

3. **Emit Registration Class**
   - Class name: `{AppName}InletRegistrations`
   - Method name: `Add{AppName}Inlet()`
   - Calls: All `Add{Aggregate}AggregateFeature()`, `AddReservoirBlazorBuiltIns()`, `AddInletClient()`, `AddInletBlazorSignalR(...)`

## Phase 3: Sample Update

4. **Update Spring.Client**
   - Add assembly attribute: `[assembly: GenerateInletClientComposite(AppName = "Spring")]`
   - Replace 4 lines in Program.cs with single `AddSpringInlet()` call
   - Keep `AddEntitySelectionFeature()` as manual call (app-specific)

## Phase 4: Testing

5. **Add Unit Tests** in `tests/Inlet.Client.Generators.L0Tests/` (or create if missing)
   - Test attribute discovery
   - Test composite output generation
   - Test with zero, one, and multiple aggregates

## Phase 5: Validation

6. **Build/Test/Cleanup**
   - `pwsh ./eng/src/agent-scripts/build-sample-solution.ps1`
   - `pwsh ./eng/src/agent-scripts/clean-up-sample-solution.ps1`
   - `pwsh ./eng/src/agent-scripts/unit-test-sample-solution.ps1`
   - Run Spring sample: `pwsh ./run-spring.ps1`

## Rollout

- Additive change: existing registrations continue to work
- Samples opt in via attribute
- No migration required

## Files to Create/Modify

| File | Action |
|------|--------|
| `src/Inlet.Generators.Abstractions/GenerateInletClientCompositeAttribute.cs` | CREATE |
| `src/Inlet.Client.Generators/InletClientCompositeGenerator.cs` | CREATE |
| `samples/Spring/Spring.Client/InletAssemblyAttributes.cs` | CREATE |
| `samples/Spring/Spring.Client/Program.cs` | MODIFY |
| `tests/Inlet.Client.Generators.L0Tests/...` | CREATE/MODIFY |
