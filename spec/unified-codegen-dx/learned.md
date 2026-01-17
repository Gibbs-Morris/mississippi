# Learned Facts

## Repository Structure

### Source Generators

| Generator | Location | Triggers On | Generates |
| --------- | -------- | ----------- | --------- |
| `AggregateServiceGenerator` | `src/EventSourcing.Aggregates.Generators/` | `[AggregateService]` | Interface, Service, Controller |
| `ProjectionApiGenerator` | `src/EventSourcing.UxProjections.Api.Generators/` | `[UxProjection]` | DTO, Mapper, Controller |

### Key Attributes

| Attribute | Namespace | Purpose |
| --------- | --------- | ------- |
| `[AggregateService(route)]` | `Mississippi.EventSourcing.Aggregates.Abstractions` | Marks aggregate for service generation |
| `[UxProjection]` | `Mississippi.EventSourcing.UxProjections.Abstractions.Attributes` | Marks projection for API generation |
| `[ProjectionPath(path)]` | `Mississippi.Inlet.Projection.Abstractions` | Defines HTTP/SignalR path for projection |
| `[BrookName(...)]` | `Mississippi.EventSourcing.Brooks.Abstractions.Attributes` | Defines event stream identity |
| `[SnapshotStorageName(...)]` | `Mississippi.EventSourcing.Brooks.Abstractions.Attributes` | Defines snapshot storage key |

### Cascade Sample Project Structure

```text
samples/Cascade/
├── Cascade.AppHost/           # Aspire host
├── Cascade.Client/            # Blazor WASM - references Contracts + Inlet
├── Cascade.Contracts/         # WASM-safe DTOs with [ProjectionPath]
│   ├── Api/                   # Request/response DTOs
│   ├── Projections/           # Manual DTO copies of domain projections
│   └── Storage/               # Storage DTOs
├── Cascade.Domain/            # Orleans domain with [GenerateSerializer]
│   ├── Channel/               # Aggregate, Commands, Events, Handlers
│   ├── Conversation/          # Aggregate, Commands, Events, Handlers
│   ├── Projections/           # UX projections with [UxProjection]
│   └── User/                  # Aggregate, Commands, Events, Handlers
├── Cascade.Domain.L0Tests/    # Domain tests
├── Cascade.Grains.Abstractions/ # Grain interfaces
├── Cascade.Server/            # API host - references Domain + Contracts
└── Cascade.Silo/              # Orleans silo
```

### Dependency Graph (verified from csproj files)

```text
Cascade.Client.csproj:
  └── Cascade.Contracts
  └── Inlet, Inlet.Blazor.WebAssembly, Reservoir, Reservoir.Blazor

Cascade.Contracts.csproj:
  └── Inlet.Projection.Abstractions (contains [ProjectionPath])
  └── Newtonsoft.Json

Cascade.Domain.csproj:
  └── EventSourcing.Aggregates.Generators (Analyzer)
  └── EventSourcing.Aggregates, .Api, Reducers, Snapshots, UxProjections
  └── Inlet.Projection.Abstractions

Cascade.Server.csproj:
  └── Cascade.Domain
  └── Cascade.Client
  └── Cascade.Contracts
  └── EventSourcing.UxProjections.Api
```

### Generated Code Pattern (ProjectionApiGenerator)

For `ChannelMessagesProjection` with `[UxProjection][ProjectionPath("cascade/channels")]`:

1. **DTO** (`ChannelMessagesProjectionDto.g.cs`):
   - Record without `[Id]`/`[GenerateSerializer]`
   - Properties match source with `IReadOnlyList<>` for collections

2. **Mapper** (`ChannelMessagesProjectionMappingExtensions.g.cs`):
   - `ToDto()` extension method on projection type
   - Handles nested types and collections

3. **Controller** (`ChannelMessagesProjectionController.g.cs`):
   - Routes at `api/projections/{path}/{entityId}`
   - Returns DTO type, not projection type

### Client-Side Projection Discovery

`InletBlazorSignalRBuilder.ScanProjectionDtos()`:

- Scans assemblies for `[ProjectionPath]` attribute
- Registers types in `IProjectionDtoRegistry`
- `AutoProjectionFetcher` uses registry to fetch projections by path

### Manual Duplication Example

**Domain** (`Cascade.Domain/Projections/ChannelMessages/ChannelMessagesProjection.cs`):

```csharp
[ProjectionPath("cascade/channels")]
[UxProjection]
[BrookName("CASCADE", "CHAT", "CONVERSATION")]
[GenerateSerializer]
public sealed record ChannelMessagesProjection
{
    [Id(0)] public string ChannelId { get; init; }
    [Id(1)] public ImmutableList<MessageItem> Messages { get; init; }
    [Id(2)] public int MessageCount { get; init; }
}
```

**Contracts** (`Cascade.Contracts/Projections/ChannelMessagesDto.cs`):

```csharp
[ProjectionPath("cascade/channels")]
public sealed record ChannelMessagesDto
{
    public required string ChannelId { get; init; }
    public required IReadOnlyList<ChannelMessageItem> Messages { get; init; }
    public required int MessageCount { get; init; }
}
```

Both have `[ProjectionPath]` with the same path, but:

- Domain has Orleans attributes
- Contracts has `required` modifiers and JSON serialization concerns

### Current API Endpoint Registration

In `Cascade.Server/Program.cs`:

```csharp
app.MapUxProjections(typeof(ChannelMessagesProjection).Assembly);
```

This scans for `[UxProjection]` + `[ProjectionPath]` and creates minimal API endpoints.

## Verified Status

### AggregateServiceGenerator Usage in Cascade

**VERIFIED**: Only `UserAggregate` has `[AggregateService("users")]` attribute
(line 17 of `UserAggregate.cs`). `ChannelAggregate` and `ConversationAggregate`
do NOT have this attribute.

**VERIFIED**: Generated services exist but are unused.
`Cascade.Server/Program.cs` uses manual endpoints with
`IAggregateGrainFactory.GetGenericAggregate<T>()` instead of generated
`IUserService`.

### ProjectionApiGenerator Usage

**VERIFIED**: Generator produces DTOs, mappers, and controllers for each
`[UxProjection]` type in Domain.

**VERIFIED**: Generated DTOs are NOT used by Client. Client references
`Cascade.Contracts` which contains manually maintained DTOs.

### Internal Aggregate Handling

**VERIFIED**: Generator respects `internal` accessibility. Generated services
inherit the same visibility as the aggregate (see `AggregateServiceGenerator.cs`
lines 98-105, 347).
