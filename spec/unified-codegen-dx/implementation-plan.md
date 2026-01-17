# Implementation Plan

## Overview

This plan enables unified source generation for aggregates and projections in
the Cascade sample, eliminating manual DTO duplication and DI boilerplate.

**User Preference:** Generated project approach (separate output projects).

## Size Classification

**Large** — Five phases touching generators, sample code, and build
configuration. No public API changes.

## Decision Checkpoint

**Yes** — Multiple viable designs exist (Options A, B, C in RFC). Recommend
Option C. User approval required before Phase 3-5 (new generators).

## Generated Project Architecture

To maintain strict Orleans isolation, generated code goes into dedicated
projects that have **no Orleans dependencies**:

```text
samples/Cascade/
├── Cascade.Domain/              # Source definitions (has Orleans)
│   ├── Channel/
│   │   ├── ChannelAggregate.cs     [AggregateService]
│   │   └── ChannelSummary.cs       [UxProjection][GenerateClientDto]
│   └── Commands/
│       └── CreateChannel.cs        [GenerateClientAction]
│
├── Cascade.Contracts.Generated/  # Generated DTOs (NO Orleans)
│   ├── ChannelSummaryDto.g.cs      # Orleans attributes stripped
│   └── UserSummaryDto.g.cs
│
├── Cascade.Client.Generated/     # Generated actions/effects (NO Orleans)
│   ├── CreateChannelAction.g.cs
│   ├── CreateChannelEffect.g.cs
│   └── ...
│
├── Cascade.Contracts/            # Keep: API DTOs, storage types
├── Cascade.Client/               # References .Generated projects
├── Cascade.Server/               # Generated controllers + DI
└── Cascade.Silo/                 # Full Orleans
```

### Project Reference Structure

```mermaid
flowchart TB
    subgraph "Orleans Zone (Silo)"
        Domain["Cascade.Domain<br/>Orleans.Serialization"]
        Silo["Cascade.Silo<br/>Orleans.Server"]
    end

    subgraph "ASP.NET Zone (Server)"
        Server["Cascade.Server<br/>Orleans.Client"]
    end

    subgraph "Orleans-Free Zone (WASM)"
        ContractsGen["Cascade.Contracts.Generated<br/>⚠️ NO ORLEANS"]
        ClientGen["Cascade.Client.Generated<br/>⚠️ NO ORLEANS"]
        Client["Cascade.Client<br/>Blazor WASM"]
    end

    Domain --> Server
    Domain -.->|"AnalyzerReference<br/>(compile only)"| ContractsGen
    ContractsGen --> Client
    ClientGen --> Client
    Silo --> Domain
```

### Key Isolation Mechanisms

1. **AnalyzerReference** — Contracts.Generated references Domain with
   `ReferenceOutputAssembly="false"`, so Orleans types are visible to
   the generator but NOT linked at runtime.

2. **Attribute Stripping** — Generator removes `[Id(n)]`, `[GenerateSerializer]`,
   `[Immutable]`, `[Alias]` when emitting DTOs.

3. **No Transitive Orleans** — .Generated projects have no Orleans package
   references; build will fail if accidentally introduced.

## Phases

### Phase 1: Enable Existing Generators in Cascade

**Goal:** Add `[AggregateService]` to all aggregates; wire generated services.

**Files to Modify:**

1. `samples/Cascade/Cascade.Domain/Channel/ChannelAggregate.cs`
   - Add `[AggregateService("channels")]` attribute

2. `samples/Cascade/Cascade.Domain/Conversation/ConversationAggregate.cs`
   - Add `[AggregateService("conversations")]` attribute

3. `samples/Cascade/Cascade.Server/Program.cs`
   - Replace manual `IAggregateGrainFactory.GetGenericAggregate<T>()` calls
     with injected `I{Name}Service`
   - Remove 6 manual endpoint registrations

**Validation:**

```powershell
dotnet build samples/Cascade/Cascade.Server/Cascade.Server.csproj -warnaserror
```

Expect: Clean build with generated `IChannelService`, `IConversationService`.

### Phase 2: Verify Projection DTO Generation

**Goal:** Confirm generated DTOs are usable by client.

**Investigation:**

1. Build Domain project and inspect `obj/Debug/net9.0/generated/` for
   `{Name}Dto.g.cs` files.

2. Verify generated DTOs have same properties as manual Contracts DTOs.

3. Test: Temporarily reference generated DTOs from Client; confirm
   serialization round-trip.

**Outcome Decision Point:**

- If generated DTOs are equivalent → proceed to Phase 4 (delete manual DTOs)
- If differences exist → document gaps and decide whether to extend generator

### Phase 3: Create DomainRegistrationsGenerator

**Goal:** Eliminate 80+ manual `Add*` calls in `CascadeRegistrations.cs`.

**New Files:**

1. `src/EventSourcing.Generators/DomainRegistrationsGenerator.cs`
   - Scan for `[Aggregate]`, `[AggregateService]`, `[UxProjection]`
   - Scan for `CommandHandler<,>`, `IReducer<,>`, `IEventType`
   - Emit `AddDomain(this IServiceCollection, ...options)` extension

2. `src/EventSourcing.Generators/DomainRegistrationsGenerator.Syntax.cs`
   - Roslyn incremental pipeline
   - Combine multiple syntax providers

**Attribute Dependencies:**

- Use existing `[AggregateService]`, `[UxProjection]` as markers
- Discover handlers/reducers by base type inspection

**Generated Output Example:**

```csharp
public static class DomainRegistrations
{
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        services.AddEventType<ChannelCreatedEvent>();
        services.AddEventType<MessageSentEvent>();
        // ... all event types
        services.AddCommandHandler<CreateChannelHandler>();
        // ... all handlers
        services.AddReducer<ChannelMessagesReducer>();
        // ... all reducers
        return services;
    }
}
```

**Validation:**

```powershell
dotnet build samples/Cascade/Cascade.Domain/Cascade.Domain.csproj -warnaserror
```

Expect: Generated `DomainRegistrations.AddDomain()` method.

### Phase 4: Create ClientDtoGenerator and Migrate Client

**Goal:** Generate WASM-safe DTOs via opt-in `[GenerateClientDto]` attribute.

**New Attribute:**

Create `[GenerateClientDto]` in `EventSourcing.UxProjections.Abstractions`:

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateClientDtoAttribute : Attribute
{
    /// <summary>Optional custom DTO name (default: {ProjectionName}Dto).</summary>
    public string? DtoName { get; set; }

    // Reserved for future RBAC (v2)
    // public string[]? RequiredRoles { get; set; }
    // public string[]? RequiredPermissions { get; set; }
    // public string? RequiredPolicy { get; set; }
}
```

**Why opt-in?**

- Not all projections are client-visible (some are internal).
- Future RBAC properties will control who can access which DTOs.
- Keeps generated output minimal and intentional.

**New Generator:**

1. `src/EventSourcing.Generators/ClientDtoGenerator.cs`
   - Scan `[UxProjection]` types that ALSO have `[GenerateClientDto]`
   - Emit `{Name}Dto` records without Orleans attributes
   - Target output: `Cascade.Contracts.Generated` project

**New Projects:**

1. `samples/Cascade/Cascade.Contracts.Generated/`
   - AnalyzerReference to `EventSourcing.Generators`
   - CompileReference to `Cascade.Domain` (for type info only; not runtime dep)

**Migration Steps:**

1. Add `[GenerateClientDto]` to client-visible projections in Domain:

   ```csharp
   [ProjectionPath("cascade/channels")]
   [UxProjection]
   [GenerateClientDto]  // <-- opt-in for client generation
   [GenerateSerializer]
   public sealed record ChannelMessagesProjection { ... }
   ```

2. Create `Cascade.Contracts.Generated.csproj`:

   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>net9.0</TargetFramework>
       <ImplicitUsings>enable</ImplicitUsings>
       <Nullable>enable</Nullable>
     </PropertyGroup>
     <ItemGroup>
       <ProjectReference Include="..\Cascade.Domain\Cascade.Domain.csproj"
                         OutputItemType="Analyzer"
                         ReferenceOutputAssembly="false" />
     </ItemGroup>
     <ItemGroup>
       <PackageReference Include="Inlet.Projection.Abstractions" />
     </ItemGroup>
     <!-- ⚠️ NO Orleans packages allowed -->
   </Project>
   ```

3. Update `Cascade.Client.csproj`:
   - Replace `Cascade.Contracts` reference with `Cascade.Contracts.Generated`

4. Delete manual DTOs:
   - `samples/Cascade/Cascade.Contracts/Projections/*.cs` (9 files)

5. Update `Cascade.Contracts` to contain only:
   - `Api/` request/response DTOs (not projection DTOs)
   - `Storage/` types if needed

**Validation:**

```powershell
dotnet build samples/Cascade/Cascade.Client/Cascade.Client.csproj -warnaserror
dotnet test samples/Cascade/Cascade.Domain.L0Tests/Cascade.Domain.L0Tests.csproj
```

Expect: Clean build and passing tests.

## Rollout Plan

| Step | Action | Validation |
| ---- | ------ | ---------- |
| 1 | Phase 1: Add attributes | Build Cascade.Server |
| 2 | Phase 1: Wire services | Run L0 tests |
| 3 | Phase 2: Verify DTOs | Manual inspection |
| 4 | Phase 3: DI generator | Build Cascade.Domain |
| 5 | Phase 3: Migrate DI | Delete CascadeRegistrations |
| 6 | Phase 4: Client DTOs | Build Cascade.Client |
| 7 | Phase 4: Delete manual | Full test suite |
| 8 | Phase 5: Client actions | Build Cascade.Client |
| 9 | Phase 5: Migrate commands | Full test suite |

## Backout Plan

Each phase is independently revertible:

- **Phase 1**: Remove `[AggregateService]` attributes; restore manual endpoints
- **Phase 2**: No changes to commit; investigation only
- **Phase 3**: Delete generator; restore `CascadeRegistrations.cs`
- **Phase 4**: Delete `Cascade.Contracts.Generated`; restore Contracts DTOs
- **Phase 5**: Delete `[GenerateClientAction]` attributes; restore manual HTTP calls

## Test Plan

### Unit Tests

- Existing `Cascade.Domain.L0Tests` must pass after each phase
- Add generator tests in `EventSourcing.Generators.L0Tests/`

### Integration Tests

- Build and run Cascade.AppHost
- Verify API endpoints return expected DTOs
- Verify Blazor WASM client can fetch projections

### Manual Verification

- Inspect generated files in `obj/` for correctness
- Confirm no Orleans package references in Client

## Files Touched Summary

| Phase | Files Modified | Files Created | Files Deleted |
| ----- | -------------- | ------------- | ------------- |
| 1 | 3 | 0 | 0 |
| 2 | 0 | 0 | 0 |
| 3 | 1 | 2-3 | 1 |
| 4 | 2 | 1 project | 9 DTOs |
| 5 | 5-10 commands | 3-4 generator files | Manual HTTP code |

## Risks and Mitigations

| Risk | Mitigation |
| ---- | ---------- |
| Generated DTOs differ from manual | Phase 2 investigation before delete |
| Build ordering with analyzer refs | Use `OutputItemType="Analyzer"` pattern |
| Generator perf regression | Incremental generators already in use |
| Breaking client serialization | Verify JSON round-trip in Phase 2 |
| Client action naming conflicts | Use namespace scoping and prefix options |

## Phase 5: Create ClientActionGenerator (New)

**Goal:** Generate Fluxor actions and effects for client-side command dispatch.

### New Attribute

Create `[GenerateClientAction]` in `EventSourcing.Aggregates.Abstractions`:

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateClientActionAttribute : Attribute
{
    public string? ActionName { get; set; }
}
```

### New Generator

Create `src/EventSourcing.Generators/ClientActionGenerator.cs`:

**Triggers on:** Commands with `[GenerateClientAction]` attribute

**Scans:**

1. Command type name and properties
2. Parent aggregate's `[AggregateService]` route for URL construction

**Generates:**

1. `{CommandName}Action.g.cs` — action record with command properties
2. `{CommandName}SuccessAction.g.cs` — success result action
3. `{CommandName}FailureAction.g.cs` — failure result action
4. `{CommandName}Effect.g.cs` — effect handling HTTP dispatch

### Migration Steps

1. Add `[GenerateClientAction]` to selected commands in `Cascade.Domain`:

   ```csharp
   [GenerateClientAction]
   [GenerateSerializer]
   public sealed record CreateChannel
   {
       [Id(0)] public required string ChannelId { get; init; }
       [Id(1)] public required string Name { get; init; }
       [Id(2)] public required string CreatedBy { get; init; }
   }
   ```

2. Update `Cascade.Client` to reference generated actions.

3. Replace manual HTTP calls in `ChatApp.razor.cs` with action dispatches:

   ```csharp
   // Before
   await Http.PostAsJsonAsync($"/api/channels/{channelId}/create?...", null);

   // After
   Dispatch(new CreateChannelAction { EntityId = channelId, Name = name, CreatedBy = user });
   ```

4. Delete manual command dispatch code.

### Validation

```powershell
dotnet build samples/Cascade/Cascade.Client/Cascade.Client.csproj -warnaserror
dotnet run --project samples/Cascade/Cascade.AppHost/Cascade.AppHost.csproj
```

Manual test: Create channel via UI, verify command executes successfully.

## Sequence Diagram: API Request with Generated Service

```mermaid
sequenceDiagram
    participant Client as Blazor WASM
    participant API as Cascade.Server
    participant Svc as IUserService (generated)
    participant Grain as IAggregateGrain
    participant Store as Event Store

    Client->>API: POST /api/users/{id}/commands
    API->>Svc: ExecuteAsync(command)
    Svc->>Grain: GetGenericAggregate<UserAggregate>()
    Grain->>Store: Load events
    Store-->>Grain: Event stream
    Grain->>Grain: Apply command
    Grain->>Store: Append event
    Grain-->>Svc: Result
    Svc-->>API: CommandResult
    API-->>Client: 200 OK
```

## As-Is vs To-Be Architecture

```mermaid
flowchart TB
    subgraph As-Is
        A1[Domain Types] --> A2[Manual DTOs in Contracts]
        A1 --> A3[Manual DI in CascadeRegistrations]
        A1 --> A4[Manual Endpoints in Program.cs]
        A2 --> A5[Client]
    end
    subgraph To-Be
        B1[Domain Types + Attributes] --> B2[Generated DTOs]
        B1 --> B3[Generated DI Extension]
        B1 --> B4[Generated Services + Controllers]
        B2 --> B5[Client]
    end
```
