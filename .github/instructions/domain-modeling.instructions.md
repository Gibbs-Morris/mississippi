---
applyTo: '**/*.cs'
---

# Domain Modeling with Mississippi Event Sourcing

Governing thought: Production aggregate and projection features use named immutable records, typed handlers, pure event reducers, separate effects, and wiring-only registration across framework libraries, applications, and samples.

> Drift check: Review `src/DomainModeling.Abstractions` and `src/Tributary.Abstractions` for base class signatures before implementing handlers or reducers.

## Rules (RFC 2119)

### Production Feature Pattern

- New or modified production aggregate and projection features **MUST** follow the named component pattern below, including reusable framework features under `src/`. Why: Framework ownership does not make domain behavior infrastructure.
- Aggregate features **MUST** define named immutable state, command, and event records with named typed command handlers and event reducers. Why: Domain decisions and replay remain discoverable and independently testable.
- Projection features **MUST** define named immutable read-model state and named event reducers. Why: Read models derive their state from accepted events.
- Projections **MUST NOT** accept domain commands or author business events. Why: Aggregates own command decisions; projections serve reads.
- Side effects **MUST** remain separate from command decisions and event reduction in named effect implementations. Why: Replay stays independent of external work.
- Low-level dispatch, storage engines, delegate adapters, ordinary DI factories, and focused test fixtures **MAY** retain their existing infrastructure or fixture shapes. Why: These mechanisms enable or test features without becoming domain features themselves.
- An exceptional production-pattern departure **MUST** have documented evidence of an actual limitation and direct user agreement. Why: Convenience, framework ownership, and PR line targets do not justify hiding feature behavior.

### Domain Record Visibility

- Aggregate, command, and projection records **MUST** be internal by default. They **MUST** be public when their types occur in public generated API signatures or are discovered through exported-type scanning. Why: Public C# signatures require accessible types, and exported-type discovery selects public types.
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

- Events **MUST** be immutable `sealed record` types with `[EventStorageName]`, `[GenerateSerializer]`, and `[Alias]` attributes. Why: Enables stable event storage and serialization.
- Events **MUST** remain internal unless they are intentional public cross-assembly extension contracts under the [abstractions policy](abstractions-projects.instructions.md). Why: Stored facts do not automatically require a public API.
- Event names **MUST** use past tense (e.g., `ChannelCreated`, `MessageSent`); event properties **MUST** use `required` modifier and `[Id(n)]` attributes. Why: Events represent facts that have occurred.

### Command Handlers

- Handlers **MUST** inherit from `CommandHandlerBase<TCommand, TAggregate>` and be `internal sealed class`. Why: Provides type-safe dispatch and consistent base behavior.
- Handler classes **MUST** be named `{Command}Handler` (e.g., `CreateChannelHandler`); handlers **MUST** implement `HandleCore()` returning `OperationResult<IReadOnlyList<object>>`. Why: Naming convention enables discovery; return type supports multiple events.
- Handlers **MUST** validate command properties before checking aggregate state; invalid commands **MUST** return `AggregateErrorCodes.InvalidCommand`; state conflicts **MUST** return `AggregateErrorCodes.InvalidState`. Why: Clear error categorization aids debugging.
- Handlers **MUST** express accepted decisions as events or return an operation failure without mutating aggregate state. Why: The event stream records state changes; a successful no-op can return an empty event list.

### Reducers (Aggregate State)

- Reducers **MUST** inherit from `EventReducerBase<TEvent, TAggregate>` and be `internal sealed class`. Why: Provides type-safe reduction and immutability enforcement.
- Reducer classes **MUST** be named `{Event}Reducer` (e.g., `ChannelCreatedReducer`); reducers **MUST** return a new instance using `with` expressions or record constructors. Why: Immutability is enforced at runtime.
- Aggregate and projection reducers **MUST NOT** mutate input state or its reachable collections. Why: A new outer record alone does not make mutable nested data safe.
- Aggregate and projection reducers **MUST** deterministically derive new state solely from the supplied state and event. Why: Replay cannot depend on I/O, clocks, randomness, host policy, or other external state.

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

### Registration and Placement

- Domain registration **MUST** follow the composition boundary and registration conventions in [service registration](service-registration.instructions.md#rules-rfc-2119). Why: Registration wires named components rather than owning decisions or replay.
- Feature files **MUST** follow the `Aggregates/<Name>/...` and `Projections/<Name>/...` archetypes in [namespace and folder placement](namespace-folder-placement.instructions.md#archetype-segment-naming-canonical). Why: The same components remain easy to find across projects.
- Public feature contracts **MUST** follow the minimal contract boundary in [abstractions projects](abstractions-projects.instructions.md#rules-rfc-2119). Why: Consumer-facing state and requests do not expose implementation behavior.

## Scope and Audience

Contributors implementing new or modified production aggregate and projection features using Mississippi event sourcing in framework libraries, applications, or samples. Infrastructure adapters and focused test fixtures keep the bounded allowance above; they do not exempt production feature behavior hosted through them. This policy does not require a blanket migration of untouched features.

## At-a-Glance Quick-Start

### Component Responsibilities

| Component | Aggregate | Projection |
|-----------|-----------|------------|
| State | Named immutable `{Name}Aggregate` record | Named immutable `{Name}Projection` read model |
| Commands | Named immutable records expressing caller intent | None; commands target aggregates |
| Handlers | `{Command}Handler : CommandHandlerBase<TCommand, TAggregate>` validates input/state and returns events or failure | None |
| Events | Named past-tense records describing accepted facts | Consumes existing events; does not author business events |
| Reducers | `{Event}Reducer : EventReducerBase<TEvent, TAggregate>` implements `ReduceCore()` for replay | `{Event}ProjectionReducer : EventReducerBase<TEvent, TProjection>` implements `ReduceCore()` for read-model derivation |
| Effects | Named implementations for side effects when needed | Supported aggregate effects or host services for external work, separate from projection replay |
| Registration | Composes types, options, descriptors, and factories | Composes reducers and snapshot support; ensures consumed event types are available in the host |

Current projection replay has no native event-effect dispatch. Keep external work in supported aggregate effects or host services; registering `AddEventEffect<TEffect, TProjection>()` does not make a projection execute it. The [projection cache](../../src/DomainModeling.Runtime/UxProjectionVersionedCacheGrain.cs) loads reduced state, while [aggregate execution](../../src/DomainModeling.Runtime/GenericAggregateGrain.cs) dispatches event effects.

### Folder Structure

The [placement archetypes](namespace-folder-placement.instructions.md#archetype-segment-naming-canonical) govern optional child folders. This hand-authored outline uses state records at the feature root, as in current source; create folders only for components the feature has. Generated wiring retains the entry points described by [service registration](service-registration.instructions.md#domain-registration-patterns-event-sourcing).

```text
{Domain}/
├── Aggregates/{Name}/
│   ├── {Aggregate}Aggregate.cs
│   ├── Commands/
│   │   └── {Action}.cs
│   ├── Events/
│   │   └── {ActionPastTense}.cs
│   ├── Handlers/
│   │   └── {Action}Handler.cs
│   ├── Reducers/
│   │   └── {Event}Reducer.cs
│   └── Effects/                         # Only when needed
├── Projections/{Name}/
│   ├── {ProjectionName}Projection.cs
│   └── Reducers/
│       └── {Event}ProjectionReducer.cs
└── {Domain}DomainRegistrations.cs
```

For a reusable framework feature, the public state/commands can occupy matching archetype paths in its Abstractions project. Internal events, handlers, reducers, effects, and concrete wiring remain in its implementation project. [Undercurrent registration](../../src/Undercurrent.Domain/UndercurrentDomainRegistrations.cs) composes that split; [Saga registration](../../src/DomainModeling.Runtime/SagaRegistrations.cs) also wires named handlers, reducers, and an effect. [Spring's BankAccount aggregate](../../samples/Spring/Spring.Domain/Aggregates/BankAccount) and [FlaggedTransactions projection](../../samples/Spring/Spring.Domain/Projections/FlaggedTransactions) show application components. These sources demonstrate responsibilities; the archetype policy governs new placement.

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
