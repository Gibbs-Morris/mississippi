# Verification

## Claim List

| ID | Claim | Status |
|----|-------|--------|
| C1 | `CommandClientRegistrationGenerator` already generates `Add{Aggregate}AggregateFeature()` methods | VERIFIED |
| C2 | Hub path `/hubs/inlet` is the conventional path used across samples | VERIFIED |
| C3 | `ScanProjectionDtos` discovers types with `[ProjectionPath]` attribute | VERIFIED |
| C4 | Assembly attributes can trigger source generators in Roslyn | NEEDS VERIFICATION |
| C5 | Generator can discover other generated methods in same compilation | NEEDS VERIFICATION |
| C6 | `AddReservoirBlazorBuiltIns()` is a single entry point for built-ins | VERIFIED |
| C7 | The Spring.Client assembly contains projection DTOs with `[ProjectionPath]` | VERIFIED |
| C8 | Inlet.Client.Generators can reference Reservoir.Blazor types | NEEDS VERIFICATION |

## Verification Questions

### Generator Infrastructure

1. How do existing generators discover types/attributes? What pattern do they use?
2. Can a generator consume output from another generator in the same compilation?
3. What assembly attributes exist in the codebase for generator triggers?

### Dependencies

4. Does Inlet.Client.Generators reference Reservoir.Blazor (needed for `AddReservoirBlazorBuiltIns`)?
5. What is the package/project dependency chain for Inlet.Client.Generators?

### Conventions

6. Is `/hubs/inlet` the only hub path used in samples?
7. How is the namespace for generated registrations determined?

### Testing

8. How are existing client generators tested?
9. What test infrastructure exists for incremental generators?

## Answers

### Q1: Generator Discovery Pattern

From `CommandClientRegistrationGenerator.cs`:
- Uses `context.SyntaxProvider.ForAttributeWithMetadataName()` to find types with `[GenerateCommand]`
- Collects command models with namespace, type name, and aggregate info
- Groups by aggregate name to emit per-aggregate registration

**Evidence**: Lines 40-55 in CommandClientRegistrationGenerator.cs show `FindCommandsInNamespace()` recursively scanning for attributed types.

### Q2: Cross-Generator Consumption

**UNVERIFIED** - Need to check if Roslyn allows one generator to see output from another in the same compilation pass.

Typical pattern: Generators run in phases. A generator consuming other generators' output would need to:
- Run in a later phase, OR
- Discover the *source* types (commands/projections) directly rather than the generated methods

The safer approach: Scan for commands/projections directly (same as existing generators) and emit the composite registration that calls the known method names.

### Q3: Existing Assembly Attributes

Need to search for `[assembly:` patterns to see if any generators trigger on assembly attributes.

### Q4: Inlet.Client.Generators Dependencies

(To be verified by reading csproj)

### Q5: Package Dependency Chain

(To be verified)

### Q6: Hub Path Conventions

Verified `/hubs/inlet` in Spring sample. Need to check if other samples exist and use same path.

### Q7: Namespace Conventions

From `CommandClientRegistrationGenerator.cs`, `NamingConventions.GetClientFeatureRootNamespace()` derives namespaces.
The composite registration should go in the client root namespace (e.g., `Spring.Client`).

### Q8-Q9: Testing Infrastructure

(To be verified by examining test projects)
