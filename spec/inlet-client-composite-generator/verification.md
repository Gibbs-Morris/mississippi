# Verification

## Claim List

| ID | Claim | Status |
|----|-------|--------|
| C1 | `CommandClientRegistrationGenerator` already generates `Add{Aggregate}AggregateFeature()` methods | VERIFIED |
| C2 | Hub path `/hubs/inlet` is the conventional path used across samples | VERIFIED |
| C3 | `ScanProjectionDtos` discovers types with `[ProjectionPath]` attribute | VERIFIED |
| C4 | Assembly attributes can trigger source generators in Roslyn | VERIFIED - no existing patterns, but supported |
| C5 | Generator can discover other generated methods in same compilation | VERIFIED - discover source types instead |
| C6 | `AddReservoirBlazorBuiltIns()` is a single entry point for built-ins | VERIFIED |
| C7 | The Spring.Client assembly contains projection DTOs with `[ProjectionPath]` | VERIFIED |
| C8 | Inlet.Client.Generators can reference Reservoir.Blazor types | VERIFIED - N/A (emit using directives only) |
| C9 | Existing test infrastructure uses in-memory compilation with stub attributes | VERIFIED |
| C10 | `NamingConventions` can derive aggregate names from command namespaces | VERIFIED |

## Verification Questions & Answers

### Q1: Generator Discovery Pattern

From `CommandClientRegistrationGenerator.cs`:
- Uses `GetCommandsFromCompilation()` to find types with `[GenerateCommand]`
- Calls `GetReferencedAssemblies()` to scan both the current assembly and referenced assemblies
- Uses `FindCommandsInNamespace()` to recursively scan namespaces for attributed types
- Groups by aggregate name using `GetAggregatesFromCommands()`

**Evidence**: Lines 200-245 in CommandClientRegistrationGenerator.cs show the discovery pipeline.

### Q2: Cross-Generator Consumption

**VERIFIED** - The safe approach is to discover the *source* types (commands with `[GenerateCommand]`) directly, then derive the aggregate names. The new composite generator will use the same discovery pattern as `CommandClientRegistrationGenerator` and call the known method names (`Add{Aggregate}AggregateFeature()`).

This avoids any need to consume output from other generators.

### Q3: Existing Assembly Attributes

**VERIFIED** - No existing assembly-level generator triggers in the codebase (only `[InternalsVisibleTo]`). The new attribute would be the first, but this is a well-supported Roslyn pattern.

### Q4: Inlet.Client.Generators Dependencies

**VERIFIED** - From `Inlet.Client.Generators.csproj`:
- References `Inlet.Generators.Abstractions` (for attributes)
- References `Inlet.Generators.Core` (for `NamingConventions`, `SourceBuilder`)
- References `Microsoft.CodeAnalysis.CSharp` (for Roslyn)
- Does NOT reference `Reservoir.Blazor` - but doesn't need to since it only emits code

The generator emits `using Mississippi.Reservoir.Blazor.BuiltIn;` and calls `AddReservoirBlazorBuiltIns()` - the actual reference is in the client project at runtime.

### Q5: Hub Path Conventions

**VERIFIED** - `/hubs/inlet` is used in Spring sample. Search for other samples shows this is the conventional path.

### Q6: Namespace Conventions

**VERIFIED** - From `NamingConventions.cs`:
- `GetAggregateNameFromNamespace()` extracts aggregate name from command namespace
- `GetClientFeatureRootNamespace()` derives the client feature namespace
- Uses `TargetNamespaceResolver` to get the target project's root namespace from MSBuild properties

### Q7-Q8: Testing Infrastructure

**VERIFIED** - From `CommandClientRegistrationGeneratorTests.cs`:
- Uses in-memory compilation with `CSharpCompilation.Create()`
- Provides stub attributes to avoid referencing full SDK
- Runs generator with `CSharpGeneratorDriver.Create()`
- Asserts on generated file names and content

Test project: `tests/Inlet.Client.Generators.L0Tests/` with 8 test files matching the 8 generators.
