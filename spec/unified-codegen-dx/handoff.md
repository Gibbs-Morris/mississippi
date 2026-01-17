# Implementation Handoff Brief

## Summary

Improve Mississippi's developer experience by enabling existing source generators
and extending them to eliminate manual DTO duplication and DI boilerplate.

## Decisions (Confirmed)

| Decision | Choice | Rationale |
| -------- | ------ | --------- |
| Attribute model | **Separate** (agg vs proj) | SRP; framework should be pluggable |
| DI registration | **Compile-time** (source gen) | No runtime reflection; AOT support |
| Client action generation | **Opt-in** `[GenerateClientAction]` | Explicit control; future RBAC support |
| Client DTO generation | **Opt-in** `[GenerateClientDto]` | Same reasoning; RBAC extensibility |
| Future extensibility | **RBAC properties reserved** | Attributes will carry auth metadata |

## Critical Constraint: Orleans Isolation

**User's Stated Concern:** "The biggest concern is the Orleans attributes leaking
into client code and HTTP code, as WASM/ASP.NET/Orleans are 3 different things
running in different pods/envs."

**Solution:** Generated project approach with strict boundaries:

```text
┌─ Orleans Zone ─────────────────────────────────────────────────────────┐
│  Cascade.Domain          (Orleans.Serialization, [Id], [GenerateSerializer])
│  Cascade.Silo            (Orleans.Server, grain implementations)
└────────────────────────────────────────────────────────────────────────┘
                                    │
                              Orleans RPC
                                    │
┌─ ASP.NET Zone ─────────────────────────────────────────────────────────┐
│  Cascade.Server          (Orleans.Client only, no grain activation)
└────────────────────────────────────────────────────────────────────────┘
                                    │
                              HTTP / SignalR
                                    │
┌─ Orleans-Free Zone ────────────────────────────────────────────────────┐
│  Cascade.Contracts.Generated     (⚠️ NO Orleans packages)
│  Cascade.Client.Generated        (⚠️ NO Orleans packages)
│  Cascade.Client                  (Blazor WASM)
└────────────────────────────────────────────────────────────────────────┘
```

**Key Pattern:** SignalR sends notifications (path, entityId, version) only.
HTTP fetches actual projection data. This separation means client DTOs only
need JSON serialization—no Orleans.

## POC Validation: Dual Generator Coexistence

**Concern:** Will Orleans generator conflict with Mississippi generator?

**Answer:** ✅ **No.** POC in `.scratchpad/poc-orleans-coexist/` proves both work:

| Generator | Project | Output |
|-----------|---------|--------|
| Orleans `OrleansCodeGen` | `Source.Domain` | `Codec_ChannelProjection` |
| Mississippi `ClientDtoGenerator` | `Target.Contracts` | `ChannelProjectionDto` |

**How it works:**

1. Domain project has `[GenerateSerializer]` + `[GenerateClientDto]` on types
2. Orleans generator runs in Domain → produces serialization code
3. Contracts project references Domain with `PrivateAssets="all"`
4. Mississippi generator runs in Contracts → scans `compilation.References`
5. Mississippi generator finds `[GenerateClientDto]` types → emits Orleans-free DTOs
6. Client references Contracts → gets DTOs without Orleans packages

**Project reference pattern (validated):**

```xml
<ProjectReference Include="..\Cascade.Domain\Cascade.Domain.csproj"
                  PrivateAssets="all" />
<ProjectReference Include="...\EventSourcing.Generators.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

**Client output (verified):** Only `Target.Client.dll` + `Target.Contracts.dll`
— **zero Orleans DLLs**.

## Current State

- **Two generators exist:** `AggregateServiceGenerator`, `ProjectionApiGenerator`
- **Generators are underutilized:** Cascade uses manual endpoints and manual DTOs
- **Duplication:** 9 DTOs in `Cascade.Contracts` manually mirror Domain projections
- **Boilerplate:** 332 lines of manual DI registrations in `CascadeRegistrations.cs`

## Target State

- Aggregates with `[AggregateService]` → Generated services used in endpoints
- Projections with `[UxProjection]` + `[GenerateClientDto]` → Generated DTOs
- Commands with `[GenerateClientAction]` → Generated Reservoir actions/effects
- DI registrations → Generated via compile-time source generator
- Manual Contracts DTOs → Deleted (replaced by generated)

---

## Naming and Taxonomy

See [naming-taxonomy.md](naming-taxonomy.md) for full details. Key decisions:

| Pattern | Examples | Purpose |
|---------|----------|---------|
| `Generate*` | `[GenerateAggregateService]`, `[GenerateClientDto]` | Triggers source generation |
| `Define*` | `[DefineProjectionPath]`, `[DefineBrookName]` | Assigns identity/metadata |

Legacy attribute names remain as `[Obsolete]` shims during transition.

---

## Execution Checklist

### Phase 0: Attribute Naming Alignment (Start Here)

- [ ] **0.1** Rename `AggregateServiceAttribute` → `GenerateAggregateServiceAttribute`
- [ ] **0.2** Rename `UxProjectionAttribute` → `GenerateProjectionApiAttribute`
- [ ] **0.3** Rename `ProjectionPathAttribute` → `DefineProjectionPathAttribute`
- [ ] **0.4** Update generators to use new attribute names
- [ ] **0.5** Update all usages in Mississippi source and samples
- [ ] **0.6** Build and run tests to verify rename is complete

### Phase 1: Enable Existing Generators

- [ ] **1.1** Add `[GenerateAggregateService("channels")]` to `ChannelAggregate.cs`
- [ ] **1.2** Add `[GenerateAggregateService("conversations")]` to `ConversationAggregate.cs`
- [ ] **1.3** Build and verify generated files exist in `obj/`
- [ ] **1.4** Register generated services in Silo DI
- [ ] **1.5** Replace one manual endpoint to use generated service (UserAggregate)
- [ ] **1.6** Run integration test to verify functionality
- [ ] **1.7** Repeat 1.5-1.6 for remaining aggregates

### Phase 2: Projection DTOs (Verification)

- [ ] **2.1** List generated projection DTOs from Domain build
- [ ] **2.2** Compare generated DTOs with manual Contracts DTOs
- [ ] **2.3** Document differences (naming, property handling)
- [ ] **2.4** Make go/no-go decision on unified approach

### Phase 3: DI Generator (Compile-Time)

- [ ] **3.1** Create `DomainRegistrationsGenerator` in `EventSourcing.Generators`
- [ ] **3.2** Implement discovery of handlers, reducers, event types
- [ ] **3.3** Generate `AddGenerated{Namespace}Domain()` method
- [ ] **3.4** Add tests for generator
- [ ] **3.5** Replace `CascadeRegistrations.cs` with generated call
- [ ] **3.6** Verify build and runtime

### Phase 4: Client DTO Generator (Opt-In)

- [ ] **4.1** Create `[GenerateClientDto]` attribute in abstractions
- [ ] **4.2** Add `[GenerateClientDto]` to client-visible projections:

    ```csharp
    [GenerateProjectionApi]
    [GenerateClientDto]  // Opt-in for client generation
    public sealed record ChannelMessagesProjection { ... }
    ```

- [ ] **4.3** Create `Cascade.Contracts.Generated.csproj`:

    ```xml
    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
      </PropertyGroup>
      <ItemGroup>
        <!-- Domain reference with PrivateAssets prevents Orleans from
             flowing transitively; generator reads types via compilation.References -->
        <ProjectReference Include="..\Cascade.Domain\Cascade.Domain.csproj"
                          PrivateAssets="all" />

        <!-- Generator as analyzer -->
        <ProjectReference Include="..\..\..\..\src\EventSourcing.Generators\EventSourcing.Generators.csproj"
                          OutputItemType="Analyzer"
                          ReferenceOutputAssembly="false" />
      </ItemGroup>
      <!-- ⚠️ NO Orleans packages allowed - validated via POC -->
    </Project>
    ```

    **Pattern validated via POC** (`.scratchpad/poc-cross-project-gen/`).

- [ ] **4.2** Create `ClientDtoGenerator` in `EventSourcing.Generators`:
  - Strip `[Id]`, `[GenerateSerializer]`, `[Immutable]`, `[Alias]`
  - Emit to `*.Contracts.Generated` namespace
- [ ] **4.3** Update `Cascade.Client.csproj` to reference `Contracts.Generated`
- [ ] **4.4** Delete manual DTOs in `Cascade.Contracts/Projections/`
- [ ] **4.5** Verify client builds without Orleans package errors
- [ ] **4.6** Run full integration test

### Phase 5: Client Action Generator

- [ ] **5.1** Create `[GenerateClientAction]` attribute in Abstractions
- [ ] **5.2** Create `ClientActionGenerator` in `EventSourcing.Generators`
- [ ] **5.3** Create `Cascade.Client.Generated` project (no Orleans)
- [ ] **5.4** Add `[GenerateClientAction]` to select commands
- [ ] **5.5** Replace manual HTTP calls in `ChatApp.razor.cs` with dispatches
- [ ] **5.6** Verify actions dispatch correctly

### Phase 6: Cleanup

- [ ] **6.1** Remove unused manual endpoints
- [ ] **6.2** Simplify `CascadeRegistrations.cs`
- [ ] **6.3** Update documentation
- [ ] **6.4** Run full test suite (`pwsh ./go.ps1`)

---

## Commands to Run

### Build and Verify Generators

```powershell
# Build Domain to trigger generators
dotnet build samples/Cascade/Cascade.Domain/Cascade.Domain.csproj -c Release

# Check generated files
Get-ChildItem -Path samples/Cascade/Cascade.Domain/obj -Recurse -Filter "*.g.cs" |
    Select-Object FullName, Length

# Build full solution
dotnet build samples.slnx -c Release -warnaserror
```

### Run Sample

```powershell
# Start Aspire host
dotnet run --project samples/Cascade/Cascade.AppHost/Cascade.AppHost.csproj

# In another terminal, test endpoints
Invoke-WebRequest -Uri "https://localhost:7001/api/health" -SkipCertificateCheck
```

### Run Tests

```powershell
# Generator tests
pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1

# Full gate
pwsh ./go.ps1
```

---

## Expected Outputs

### After Phase 1.2 (Add AggregateService attributes)

Generated files in `samples/Cascade/Cascade.Domain/obj/.../generated/`:

- `IChannelService.g.cs`
- `ChannelService.g.cs`
- `ChannelController.g.cs`
- `IConversationService.g.cs`
- `ConversationService.g.cs`
- `ConversationController.g.cs`

### After Phase 3 (DI Generator)

Generated file:

- `CascadeDomainRegistrations.g.cs`

Contains:

```csharp
public static IServiceCollection AddGeneratedCascadeDomain(this IServiceCollection services)
{
    services.AddAggregateSupport();
    services.AddEventType<UserRegistered>();
    // ... all event types
    services.AddCommandHandler<RegisterUser, UserAggregate, RegisterUserHandler>();
    // ... all command handlers
    services.AddReducer<UserRegistered, UserProfileProjection, UserRegisteredReducer>();
    // ... all reducers
    return services;
}
```

### After Phase 4 (Client DTO Generator)

New project `Cascade.Contracts.Generated` with:

- `ChannelMessagesDto.g.cs`
- `ChannelSummaryDto.g.cs`
- `AllChannelsDto.g.cs`
- etc.

Client references this project instead of manual `Cascade.Contracts`.

---

## Rollback Plan

### Phase 1 Rollback

```powershell
# Remove attributes from ChannelAggregate.cs and ConversationAggregate.cs
git checkout -- samples/Cascade/Cascade.Domain/Channel/ChannelAggregate.cs
git checkout -- samples/Cascade/Cascade.Domain/Conversation/ConversationAggregate.cs

# Restore manual endpoints if changed
git checkout -- samples/Cascade/Cascade.Server/Program.cs
```

### Phase 3 Rollback

```powershell
# Restore manual registrations
git checkout -- samples/Cascade/Cascade.Domain/CascadeRegistrations.cs
```

### Phase 4 Rollback

```powershell
# Restore Contracts reference in Client
git checkout -- samples/Cascade/Cascade.Client/Cascade.Client.csproj

# Delete generated project
Remove-Item -Recurse samples/Cascade/Cascade.Contracts.Generated
```

---

## Open Questions (Decision Required)

### 1. Generated Controllers vs Minimal APIs

**Question:** Should Cascade use the generated `{Name}Controller` classes or
keep minimal APIs?

**Options:**

- **A) Use generated controllers** - Standard MVC pattern, OpenAPI annotations
- **B) Keep minimal APIs** - Current pattern, more flexible

**Recommendation:** Option B for now. Minimal APIs are more common in modern
.NET and offer flexibility.

### 2. Client DTO Project Location

**Question:** Where should generated client DTOs live?

**Options:**

- **A) In Contracts project** - Keep existing project, add generated files
- **B) New Contracts.Generated project** - Clean separation
- **C) Shared source links** - Complex MSBuild, harder to understand

**Recommendation:** Option B for clean separation and explicit dependencies.

### 3. Naming Convention

**Question:** Generated DTOs are named `{Type}Dto` (e.g.,
`ChannelMessagesProjectionDto`). Manual DTOs drop "Projection"
(e.g., `ChannelMessagesDto`).

**Options:**

- **A) Keep generator naming** - `ChannelMessagesProjectionDto`
- **B) Update generator** - Strip "Projection" suffix → `ChannelMessagesDto`

**Recommendation:** Option B for cleaner client API.

---

## Risks

| Risk | Likelihood | Impact | Mitigation |
| ---- | ---------- | ------ | ---------- |
| Generated services break internal visibility | Medium | Medium | Test first |
| DI generator misses types | Low | High | Test coverage |
| Client DTO generator has Orleans deps | Low | High | Verify output |
| Breaking changes in Cascade | Medium | Low | Staged rollout |

---

## Success Criteria

1. **Phase 1 Complete:** All three aggregates use generated services
2. **Phase 3 Complete:** `CascadeRegistrations.cs` reduced to single call
3. **Phase 4 Complete:** `Cascade.Contracts/Projections/` deleted
4. **Final:** All tests pass, Cascade sample runs end-to-end
