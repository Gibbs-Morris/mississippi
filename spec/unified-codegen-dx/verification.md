# Verification Questions and Answers

## Claim List

| ID | Claim |
| -- | ----- |
| C1 | `AggregateServiceGenerator` exists and generates `I{Name}Service`, `{Name}Service`, `{Name}Controller` |
| C2 | Only `UserAggregate` has `[AggregateService]`; other aggregates lack it |
| C3 | Generated `IUserService` is not used in `Cascade.Server/Program.cs` |
| C4 | `ProjectionApiGenerator` generates `{Name}Dto` without Orleans attributes |
| C5 | Generated projection DTOs are not consumed by `Cascade.Client` |
| C6 | `Cascade.Contracts` contains manually maintained DTOs duplicating Domain projections |
| C7 | `CascadeRegistrations.cs` has 80+ manual DI calls |
| C8 | `Cascade.Client` does NOT reference `Orleans.Core` or `Orleans.Serialization` |
| C9 | `[ProjectionPath]` is used for client-side projection discovery |
| C10 | Generators respect `internal` accessibility of aggregates |

## Verification Questions

### Q1: Generator Existence

**Question:** Does `AggregateServiceGenerator.cs` exist and implement
`IIncrementalGenerator`?

**Evidence:**

- File: `src/EventSourcing.Aggregates.Generators/AggregateServiceGenerator.cs`
- Line 19: `public sealed class AggregateServiceGenerator : IIncrementalGenerator`

**Answer:** ✅ Verified. Generator exists and implements
`IIncrementalGenerator`.

### Q2: AggregateService Attribute Usage

**Question:** Which aggregates in Cascade have `[AggregateService]`?

**Evidence:**

```text
grep -r "AggregateService" samples/Cascade --include="*.cs"
```

- `UserAggregate.cs` line 17: `[AggregateService("users")]`
- No matches in `ChannelAggregate.cs` or `ConversationAggregate.cs`

**Answer:** ✅ Verified. Only `UserAggregate` has the attribute.

### Q3: Generated Service Usage

**Question:** Does `Cascade.Server/Program.cs` use `IUserService`?

**Evidence:**

- File: `samples/Cascade/Cascade.Server/Program.cs`
- Uses `IAggregateGrainFactory.GetGenericAggregate<UserAggregate>`
- No references to `IUserService`

**Answer:** ✅ Verified. Generated service exists but is unused.

### Q4: ProjectionApiGenerator DTO Output

**Question:** Does `ProjectionApiGenerator` strip Orleans attributes from
generated DTOs?

**Evidence:**

- File: `src/EventSourcing.UxProjections.Api.Generators/ProjectionApiGenerator.cs`
- Lines 237-245: Filters out `[Id]` and `[GenerateSerializer]` attributes
- Method `GenerateDtoRecord` builds new record without Orleans markers

**Answer:** ✅ Verified. DTOs are Orleans-free.

### Q5: Client DTO Consumption

**Question:** Does `Cascade.Client` reference generated DTOs from Domain?

**Evidence:**

- File: `samples/Cascade/Cascade.Client/Cascade.Client.csproj`
- References: `Cascade.Contracts`, NOT `Cascade.Domain`
- Client code imports from `Cascade.Contracts.Projections`

**Answer:** ✅ Verified. Client uses manual Contracts DTOs, not generated.

### Q6: Manual DTO Duplication Count

**Question:** How many DTOs in `Cascade.Contracts/Projections/` mirror Domain?

**Evidence:**

```text
ls samples/Cascade/Cascade.Contracts/Projections/
```

- 9 DTO files: `ChannelDto.cs`, `ChannelMessagesDto.cs`, `ChannelOverviewDto.cs`,
  `ConversationDto.cs`, `ConversationMessagesDto.cs`,
  `ConversationOverviewDto.cs`, `UserDto.cs`, `UserDirectMessagesDto.cs`,
  `UserOverviewDto.cs`

**Answer:** ✅ Verified. 9 manual DTOs duplicate Domain projections.

### Q7: CascadeRegistrations Line Count

**Question:** How many lines and Add* calls in `CascadeRegistrations.cs`?

**Evidence:**

- File: `samples/Cascade/Cascade.Domain/CascadeRegistrations.cs`
- Lines: 332
- Manual calls: `AddEventType<>` (18), `AddCommandHandler<>` (15),
  `AddReducer<>` (12), `AddBrookFactory<>` (6), `AddUxProjectionFactory<>` (9),
  `AddSnapshotFactory<>` (6), etc.

**Answer:** ✅ Verified. 332 lines, 80+ registration calls.

### Q8: Orleans Package Isolation

**Question:** Does `Cascade.Client` reference Orleans packages?

**Evidence:**

- File: `samples/Cascade/Cascade.Client/Cascade.Client.csproj`
- Package references: `Microsoft.AspNetCore.Components.WebAssembly`, Inlet,
  Reservoir — NO Orleans references
- File: `samples/Cascade/Cascade.Contracts/Cascade.Contracts.csproj`
- Package references: `Inlet.Projection.Abstractions`, `Newtonsoft.Json` —
  NO Orleans references

**Answer:** ✅ Verified. Client and Contracts are Orleans-free.

### Q9: ProjectionPath Discovery

**Question:** How does the client discover projections?

**Evidence:**

- File: `src/Inlet.Blazor.WebAssembly/InletBlazorSignalRBuilder.cs`
- Method `ScanProjectionDtos()` scans for `[ProjectionPath]` attribute
- Registers types in `IProjectionDtoRegistry`

**Answer:** ✅ Verified. `[ProjectionPath]` drives client discovery.

### Q10: Internal Aggregate Visibility

**Question:** Does the generator respect `internal` accessibility?

**Evidence:**

- File: `src/EventSourcing.Aggregates.Generators/AggregateServiceGenerator.cs`
- Lines 98-105: Checks `aggregateSymbol.DeclaredAccessibility`
- Lines 347-350: Uses same accessibility for generated types

**Answer:** ✅ Verified. Generated services match aggregate visibility.

## Summary

All 11 claims verified with repository evidence and POC validation.

### Cross-Project Generation (C11) — VALIDATED

**Claim:** Generators can read types from referenced assemblies and emit
Orleans-free DTOs into a separate project using `PrivateAssets="all"`.

**Verification:** ✅ **VALIDATED via POC** (2025-01-17)

**Evidence:**

A proof-of-concept validated the cross-project generation pattern:

1. Generator scans `compilation.References` to find Domain assembly
2. Generator finds types with `[GenerateClientDto]` marker attribute
3. Generator emits Orleans-free DTOs into `Target.Contracts` project
4. `PrivateAssets="all"` prevents Orleans from flowing transitively
5. Client output contains **zero Orleans DLLs**
6. **Orleans generator still produces serialization code** in Domain project

**Generated DTO (Orleans attributes stripped):**

```csharp
// <auto-generated/>
// Generated from: Source.Domain.MarkedProjection
// Orleans attributes stripped - WASM safe!
namespace Target.Contracts;
public sealed record MarkedProjectionDto
{
    public required string EntityId { get; init; }
    public required string Title { get; init; }
    public required System.DateTime CreatedAt { get; init; }
}
```

**Client Output DLLs:**

- `Mississippi.Target.Client.dll`
- `Mississippi.Target.Contracts.dll`
- ❌ No Orleans DLLs present

**Correct Project Reference Pattern:**

```xml
<!-- Target.Contracts.csproj -->
<ItemGroup>
  <!-- Domain for type metadata; PrivateAssets prevents transitive flow -->
  <ProjectReference Include="..\Domain\Domain.csproj" PrivateAssets="all" />

  <!-- Generator as analyzer -->
  <ProjectReference Include="..\..\Generators\Generators.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

**Impact:** Phases 4-5 are now unblocked. Proceed with implementation.
