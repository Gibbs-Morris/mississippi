---
applyTo: '**/*.cs'
---

# Domain Modeling with Mississippi Event Sourcing

Governing thought: Use consistent, attribute-driven domain modeling with immutable aggregates, typed handlers, and projection reducers following the Mississippi framework patterns.

> Drift check: Review `src/DomainModeling.Abstractions` and `src/Tributary.Abstractions` for base class signatures before implementing handlers or reducers.

## Rules (RFC 2119)

### Domain Record Visibility

- Aggregate, command, and projection records **MUST** be internal by default and **MUST** be public when their types occur in public generated API signatures or are discovered through exported-type scanning. Why: Public C# signatures require accessible types, and exported-type discovery selects public types.
- Contributors **MUST** verify visibility against the consuming generator and registration path. Why: Friend-assembly access permits internal access but does not relax public-signature accessibility requirements.

Inlet's [aggregate controller generator](../../src/Inlet.Gateway.Generators/AggregateControllerGenerator.cs) exposes the aggregate type in its public base class and command types in public mapper constructor parameters. [Projection assembly scanning](../../src/Inlet.Runtime/InletSiloRegistrations.cs) uses `GetExportedTypes()`. Keep domain records public for these paths while retaining internal visibility for records without a public or discovery boundary.

### Aggregate Types

- Aggregates **MUST** be `sealed record` types with the visibility defined above and `[BrookName]`, `[SnapshotStorageName]`, `[GenerateSerializer]`, and `[Alias]` attributes. Why: Enables event sourcing, serialization, and stable storage identity.
- Aggregate properties **MUST** use `[Id(n)]` starting at 0 with unique sequential values; aggregate types **MUST** use `{ get; init; }` properties with sensible defaults. Why: Orleans serialization requires explicit member ordering.
- Aggregates **SHOULD** include a sentinel property (e.g., `IsCreated`, `IsInitialized`) to detect first-time creation. Why: Command handlers need to distinguish new vs existing aggregates.

### Command Types

- Commands **MUST** be `sealed record` types with the visibility defined above and `[GenerateSerializer]` and `[Alias]` attributes. Why: Enables Orleans serialization and stable identity.
- Serialized command properties **MUST** use `[Id(n)]` attributes. Commands **SHOULD** express caller-supplied inputs through constructor parameters or `required` init-only properties; intentionally optional inputs **MAY** use explicit defaults. Handlers **MUST** validate runtime input values. Command names **SHOULD** be verb phrases (e.g., `CreateChannel`, `UpdateDisplayName`). Why: Makes construction intent explicit while supporting the positional records and defaults used by current samples.

### Event Types

- Events **MUST** be `internal sealed record` types with `[EventStorageName]`, `[GenerateSerializer]`, and `[Alias]` attributes. Why: Enables stable event storage and serialization.
- Event names **MUST** use past tense (e.g., `ChannelCreated`, `MessageSent`); event properties **MUST** use `required` modifier and `[Id(n)]` attributes. Why: Events represent facts that have occurred.

### Command Handlers

- Handlers **MUST** inherit from `CommandHandlerBase<TCommand, TAggregate>` and be `internal sealed class`. Why: Provides type-safe dispatch and consistent base behavior.
- Handler classes **MUST** be named `{Command}Handler` (e.g., `CreateChannelHandler`); handlers **MUST** implement `HandleCore()` returning `OperationResult<IReadOnlyList<object>>`. Why: Naming convention enables discovery; return type supports multiple events.
- Handlers **MUST** validate command properties before checking aggregate state; invalid commands **MUST** return `AggregateErrorCodes.InvalidCommand`; state conflicts **MUST** return `AggregateErrorCodes.InvalidState`. Why: Clear error categorization aids debugging.

### Reducers (Aggregate State)

- Reducers **MUST** inherit from `EventReducerBase<TEvent, TAggregate>` and be `internal sealed class`. Why: Provides type-safe reduction and immutability enforcement.
- Reducer classes **MUST** be named `{Event}Reducer` (e.g., `ChannelCreatedReducer`); reducers **MUST** return a new instance using `with` expressions or record constructors. Why: Immutability is enforced at runtime.
- Reducers **MUST NOT** mutate the input state; the base class throws if the same instance is returned. Why: Event sourcing requires pure functions.

### Projection Types

- Projections **MUST** be `sealed record` types with the visibility defined above and `[BrookName]`, `[SnapshotStorageName]`, `[GenerateSerializer]`, and `[Alias]` attributes. Why: Same as aggregates but for read-optimized views.
- Projection types **SHOULD** be named `{Name}Projection` (e.g., `UserProfileProjection`, `ChannelMemberListProjection`). Why: Distinguishes from aggregate state.

### Projection Reducers

- Projection reducers **MUST** inherit from `EventReducerBase<TEvent, TProjection>` and be `internal sealed class`. Why: Same dispatch pattern as aggregate reducers.
- Projection reducer classes **MUST** be named `{Event}ProjectionReducer` to distinguish from aggregate reducers (e.g., `UserRegisteredProjectionReducer` vs `UserRegisteredReducer`). Why: Multiple reducers may consume the same event.
- Projection reducers **SHOULD** live under `Projections/{ProjectionName}/Reducers/`. Why: Clear organization by projection type.

### Attribute Values

- `[BrookName]` **MUST** use format `("APPNAME", "MODULENAME", "NAME")` with uppercase alphanumeric values only. Why: Attribute validates at runtime.
- `[EventStorageName]` and `[SnapshotStorageName]` **MUST** use format `("APPNAME", "MODULENAME", "NAME", version: n)` with version defaulting to 1; storage names **MUST NOT** change once persisted. Why: Enables safe refactoring while maintaining storage compatibility.
- `[Alias]` values **MUST** match the fully qualified type name (e.g., `[Alias("Contoso.Domain.Channel.Events.ChannelCreated")]`). Why: Provides stable Orleans serialization identity across refactoring.

### Registration

- Domain registration **MUST** follow the pattern `Add{Domain}Domain()` as public entry point calling private `Add{Aggregate}Aggregate()` and `Add{Projection}Projection()` methods. Why: Hierarchical registration keeps DI discoverable.
- Registration order **MUST** be: event types (`AddEventType<>`), command handlers (`AddCommandHandler<>`), reducers (`AddReducer<>`), snapshot converter (`AddSnapshotStateConverter<>`). Why: Dependencies must be registered before dependents.
- Registration classes **MUST** be named `{Domain}Registrations` as `public static class`. Why: Naming convention enables discovery.

## Scope and Audience

Developers implementing domain models using Mississippi event sourcing in samples or applications.

## At-a-Glance Quick-Start

### Folder Structure

```text
{Domain}/
├── {Aggregate}/
│   ├── {Aggregate}Aggregate.cs
│   ├── Commands/
│   │   └── {Action}.cs
│   ├── Events/
│   │   └── {ActionPastTense}.cs
│   ├── Handlers/
│   │   └── {Action}Handler.cs
│   └── Reducers/
│       └── {Event}Reducer.cs
├── {ProjectionName}Projection/
│   ├── {ProjectionName}Projection.cs
│   └── Reducers/
│       └── {Event}ProjectionReducer.cs
└── {Domain}Registrations.cs
```

### Required Attributes Checklist

| Type | Required Attributes |
|------|---------------------|
| Aggregate | `[BrookName]`, `[SnapshotStorageName]`, `[GenerateSerializer]`, `[Alias]` |
| Command | `[GenerateSerializer]`, `[Alias]` |
| Event | `[EventStorageName]`, `[GenerateSerializer]`, `[Alias]` |
| Projection | `[BrookName]`, `[SnapshotStorageName]`, `[GenerateSerializer]`, `[Alias]` |

### Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Aggregate | `{Name}Aggregate` | `ChannelAggregate` |
| Command | `{Verb}{Noun}` | `CreateChannel` |
| Event | `{Noun}{PastVerb}` | `ChannelCreated` |
| Handler | `{Command}Handler` | `CreateChannelHandler` |
| Reducer | `{Event}Reducer` | `ChannelCreatedReducer` |
| Projection | `{Name}Projection` | `UserProfileProjection` |
| Projection Reducer | `{Event}ProjectionReducer` | `UserRegisteredProjectionReducer` |

## Core Principles

- Immutable record types with explicit serialization ensure deterministic replay.
- Attribute-based storage names decouple code identity from persistence identity.
- Base classes enforce patterns (immutability, type dispatch) while providing extension points.
- Hierarchical registration mirrors domain structure and enables composability.

## References

- Coding discipline (samples): `.github/instructions/coding-discipline.instructions.md`
- Framework patterns (src): `.github/instructions/framework-patterns.instructions.md`
- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
- Orleans serialization: `.github/instructions/orleans-serialization.instructions.md`
- Storage naming: `.github/instructions/storage-type-naming.instructions.md`
- Service registration: `.github/instructions/service-registration.instructions.md`
