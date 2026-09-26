---
applyTo: 'samples/**'
---

# Mississippi Framework Usage

Governing thought: Build applications using the Mississippi framework with source generation as the default, a clean four-project solution structure, Redux-style state management via Reservoir, and small componentized projections over brooks.

> Drift check: Review the framework source under `src/`, the Spring sample under `samples/Spring/`, and especially `Spring.Domain/` for domain patterns before implementing new features; established patterns are authoritative.

## Rules (RFC 2119)

### Instruction Maintenance

- When the Spring sample project is updated with new patterns (e.g., effects, new attributes, new source generation options), this instruction file **MUST** be updated in the same PR or immediately following PR. Why: Spring is the reference implementation and these instructions need to remain in sync.
- New framework capabilities added to `src/` that affect how samples are built **MUST** be documented in this file before being used in samples. Why: Ensures contributors understand new patterns before applying them.

### Source Generation

- Source generation **SHOULD** be used for all supported concerns (DTOs, actions, action effects, endpoints, mappers) when a generator exists; manual implementations **MAY** be used only when no generator supports the scenario. Why: Reduces boilerplate and ensures consistency while preserving escape hatches.
- Generators consume Domain project types (aggregates, commands, events, projections) and emit Client artifacts (actions, action effects, feature registrations, DTOs); see the Generator Inputs table below and Inlet and Client-Server Integration. Why: Centralizes the source of truth in Domain while producing artifacts for all targets.
- Types marked with `[PendingSourceGenerator]` (defined in `src/Inlet.Generators.Abstractions/`) **MUST** be treated as reference implementations for generator validation only; they exist to enable test comparisons between generated and expected code and **MUST NOT** be used as patterns for new development. Why: Scoped to generator testing infrastructure.
- Contributors **SHOULD** review `src/Inlet.Client.Generators/` and `src/Inlet.Gateway.Generators/` for generator implementations, `src/Inlet.Generators.Abstractions/` for attribute definitions, and `src/Reservoir/` for state management. Why: Understanding the framework internals aids correct usage; abstractions define the attribute surface while generator projects contain the logic.

#### Generator Inputs by Project

| Input Project | Generator Input | Output Artifacts |
|---------------|-----------------|------------------|
| Domain | Aggregates with `[GenerateAggregateEndpoints]` | Runtime registration, Gateway controller, Client feature/state/reducers, feature registration (`Add{Aggregate}Feature()`) |
| Domain | Commands with `[GenerateCommand]` | DTOs, mappers, HTTP endpoints, client actions, action effects, command state |
| Domain | Projections with `[GenerateProjectionEndpoints]` | Gateway controller, Client subscription, DTOs |
| Domain | Event effects extending `EventEffectBase` or `SimpleEventEffectBase` | Runtime registration (`AddEventEffect<TEffect, TAggregate>()`) |

### Solution Structure

- Orleans runtime hosts **MUST** compose Brooks through `silo.UseMississippi(runtime => runtime.AddEventSourcing(...))` using `RuntimeBuilder` from `Mississippi.Hosting.Runtime`. Why: The runtime builder unites Brooks service and option registration and validates terminal attachment.
- New sample applications in this repository **MUST** follow the four-project structure: Runtime host (running in an Orleans silo), ASP.NET Gateway, Blazor WebAssembly Client, and Domain (see Scope and Audience). Why: Separates concerns and enables source generation.
- An Aspire AppHost project **SHOULD** be included for local development orchestration. Why: Simplifies emulator setup for Cosmos, Azure Storage, and Orleans.
- Runtime and Gateway host projects **MUST** contain only configuration, options, dependency wiring, and framework registration—not domain logic. Why: Keeps host projects thin.
- The Domain project **MUST** contain all server-side domain state (aggregates, projections, commands, events, handlers, reducers). Why: Centralizes domain logic for source generation.
- The WebAssembly Client project **MUST** contain all front-end code including UX, UI, and local state management. Why: Separates client concerns from server domain.

### State Management (Reservoir)

- Full Mississippi WebAssembly clients **MUST** compose features inside one `builder.UseMississippi(client => ...)` callback using `ClientBuilder` from `Mississippi.Hosting.Client`. Why: Terminal attachment validates the composition before committing its registrations; the client and nested builders cannot be configured after attachment.
- All client-side domain and business state **MUST** be managed via the Reservoir store using actions and reducers; ephemeral UI state (e.g., hover, focus, temporary form input) **MAY** remain component-local. Why: Enforces predictable Redux/Flux-style state management for state that matters while allowing practical UI patterns. See `.github/instructions/blazor-ux-guidelines.instructions.md`.
- Contributors **SHOULD** review how Reservoir is implemented in `src/Reservoir/` before building features. Why: Understanding the store pattern ensures correct usage.
- Dispatching actions and obtaining feature state **MUST** go through the store; ad-hoc or component-local state management **MUST NOT** be used for domain state. Why: Prevents scattered state that cannot be inspected or replayed.
- Third-party component libraries **MUST** integrate with Reservoir's state model; libraries that insist on managing internal state incompatibly **SHOULD NOT** be used as general-purpose UI components. Why: Maintains state consistency.
- Manual actions **SHOULD** follow the `{Command}Action`, `{Command}ExecutingAction`, `{Command}SucceededAction`, `{Command}FailedAction` naming pattern; generated actions follow generator naming conventions (see Inlet and Client-Server Integration). Why: Provides clear lifecycle visibility while respecting generator output.

### UX Component Guidelines

- Components **MUST** follow atomic design principles (Atoms, Molecules, Organisms, Templates, Pages). Why: Enables composition and reuse.
- State flows down the component tree via parameters (including cascading parameters for shared context); domain events flow up via `EventCallback`. Why: Creates predictable unidirectional data flow for domain interactions. See `.github/instructions/blazor-ux-guidelines.instructions.md`.
- Presentational components (Atoms, Molecules) **MUST NOT** call APIs or dispatch actions directly; they **MUST** emit events that container components (Organisms, Pages) handle. Container components dispatch actions to the store; action effects respond to those dispatched actions (see Action Effects and Reservoir State Management). Why: Keeps presentational components pure and testable while allowing containers to coordinate.
- See `.github/instructions/blazor-ux-guidelines.instructions.md` for detailed component patterns. Why: UX guidelines contain comprehensive rules.

### Inlet and Client-Server Integration

- Inlet spans both the domain logic and the front end, providing source generation for both sides. Why: Unified framework for client-server communication.
- Inlet **MUST** be used to generate client-side actions from aggregate commands marked with `[GenerateCommand]`. Why: Automates the HTTP request pipeline.
- Generated actions map directly to commands defined on aggregates; the mapping is handled by the framework. Why: Reduces manual wiring.
- When a generated action is dispatched, the source-generated infrastructure handles the following flow:

  **Success path:**
  1. Construct and issue an HTTP request
  2. Handle the request on the server
  3. Activate or retrieve the aggregate grain instance within the silo (see Orleans Grain Considerations)
  4. Validate the command and produce events
  5. Framework appends events to the brook and updates the snapshot (if snapshot storage is configured)
  6. Projection grains consume events and update their state
  7. SignalR notifies subscribed clients only, asynchronously (near-real-time, not instant; see Projection Subscriptions)

  **Failure path:** Command validation failures return an error code; no events are produced and projections are unchanged.

  See Consistency Model Separation for the async/eventual nature of projection updates.
- Client features **MUST** be registered via generated `Add{Aggregate}Feature()` extension methods; runtime and gateway projects use `Add{Aggregate}()` (following `.github/instructions/service-registration.instructions.md` conventions). Why: Enables clean, scalable feature registration consistent with repo patterns while distinguishing client from host registrations.

### Projection Subscriptions

- UX screens **SHOULD** subscribe to many small projections rather than one large monolithic projection. Why: Minimizes unnecessary updates when data changes.
- Projections **MUST** be subscribed and unsubscribed using the Inlet subscription APIs. Why: Manages SignalR connections consistently.
- Design projections so each UI surface subscribes only to what it needs, minimizing updates; a single event may legitimately update multiple projections. Why: Improves performance and reduces re-renders while allowing the 1-to-many brook-to-projection pattern.
- Client-side projection DTOs **MUST** use `[ProjectionPath]` matching the server projection's path. Why: Enables Inlet to route subscription requests correctly.

### Domain Modeling (Aggregates)

- Contributors **SHOULD** review `samples/Spring/Spring.Domain/` to understand the domain modeling approach in detail. Why: Spring serves as the reference implementation.
- Aggregates, commands, and events **MUST** be `internal sealed record` types with `[GenerateSerializer]` and `[Id(n)]` on each property; see `.github/instructions/domain-modeling.instructions.md` and Framework Attributes Reference for Orleans serialization requirements. Why: Ensures correct Orleans serialization and visibility.
- Aggregates **MUST** define commands, and command handlers **MUST** validate business logic before raising events. Why: Enforces invariants.
- Aggregates **MUST** use `[BrookName]`, `[SnapshotStorageName]`, `[GenerateSerializer]`, and `[Alias]` attributes. Aggregates exposed via API **MUST** also use `[GenerateAggregateEndpoints]` (see Framework Attributes Reference). Why: Enables event sourcing and stable serialization; endpoint generation is conditional on API exposure.
- Commands exposed to the UX **MUST** be annotated with `[GenerateCommand(Route = "...")]`. Why: Triggers endpoint and action generation.
- Command handlers **MUST** return `OperationResult<IReadOnlyList<object>>` containing events on success or an error code on failure. Use `AggregateErrorCodes.InvalidCommand` for command validation failures and `AggregateErrorCodes.InvalidState` for state-based rejections. Why: Enables consistent, typed error handling.
- Command handlers **MUST** validate the command against current state and return events; the framework handles persistence and snapshotting (see Inlet and Client-Server Integration for the full pipeline). Why: Separates business logic from infrastructure concerns.
- **Pre-1.0 event evolution**: While the repository is pre-1.0 (see `.github/instructions/backwards-compatibility.instructions.md`), event shapes **MAY** be changed freely; V2 event types and compatibility shims **MUST NOT** be introduced for patterns that only exist on the current branch. Why: Pre-release iteration speed outweighs ceremony; only contracts on `main` define the compatibility baseline.
- **Post-1.0 event immutability**: Once the repository reaches 1.0+ or events are persisted in a real (non-test) store, events **MUST NOT** be modified once written; property names/types **MUST NOT** change on existing events—events are immutable facts forming an append-only log. Adding properties to existing events **MAY** be done but is not always advisable; a new event type (e.g., `{Event}V2`) **SHOULD** be introduced alongside the original for significant schema changes. Why: Post-release, backwards compatibility supports rolling updates and gradual migration.

### Domain Modeling (Projections)

- Multiple projections **MAY** be defined over the same brook (event stream). Why: Enables different read-optimized views of the same data.
- Projections **SHOULD** be small and highly componentized. Why: Enables reuse across different UX contexts.
- Projections **MUST** use `[BrookName]`, `[SnapshotStorageName]`, `[GenerateSerializer]`, and `[Alias]` attributes. Projections exposed to clients **MUST** also use `[ProjectionPath]` and `[GenerateProjectionEndpoints]` (see Projection Subscriptions and Framework Attributes Reference). Why: Core attributes enable event sourcing; client-facing attributes are conditional on exposure.
- Projection reducers **MUST** inherit from `EventReducerBase<TEvent, TProjection>` and return new instances (using `with` expressions or constructors). Why: Enforces immutability.

### Brooks (Event Streams)

- Brooks **MUST** be identified via the `[BrookName("APPNAME", "MODULENAME", "NAME")]` attribute. Why: Provides stable string-based stream identity.
- Developers **MUST NOT** work with brooks directly; the framework aligns aggregates and projections by matching `[BrookName]` values. Why: Simplifies event sourcing configuration.
- Aggregates and brooks have a 1-to-1 relationship; each aggregate **MUST** have exactly one brook. Why: Orleans grains are single-threaded, so a single aggregate per brook ensures update consistency—the aggregate's internal state is always correct.
- Brooks and UX projections have a 1-to-many relationship; multiple projections **MAY** subscribe to the same brook. Why: Projections are eventually consistent read models, enabling different optimized views of the same event stream (CQRS pattern).

### Orleans Grain Considerations

- Aggregate grains are single-threaded; contributors **MUST** design to avoid bottlenecks from "master" grains that do too much. Why: Prevents throughput issues.
- When scalability requires it, a family of grains/aggregates sharing the same business identifier but storing different aspects **MAY** be created; each aggregate still has its own brook (the 1:1 invariant applies per aggregate type, not per identifier). Why: Distributes load across grain activations while preserving event stream isolation.
- Grain operations **SHOULD** be designed to be fast and avoid long-running operations. Why: Prevents grain throughput degradation.

### Action Effects (Client-Side Side Effects)

- Action effects **MAY** be used to trigger additional client-side behavior in response to actions (e.g., showing notifications, triggering navigation, dispatching follow-up actions). Why: Enables reactive client workflows.
- Action effects run after reducers complete and can emit multiple actions over time (e.g., async operations returning success/failure). Why: Allows side effects to drive further state changes.
- Action effects **SHOULD** dispatch follow-up actions or call client-side services rather than performing complex inline logic. Why: Keeps action effects lightweight and predictable.
- Action effects run on the client; they **MUST NOT** be confused with server-side event effects (which respond to domain events within grains). Why: Clarifies the distinction between client and server effect patterns.

### Event Effects (Server-Side Side Effects)

- Event effects **MAY** be used to trigger server-side behavior in response to persisted domain events (e.g., cross-aggregate commands, external notifications, audit logging). Why: Enables reactive server-side workflows without coupling aggregates.
- Event effects run synchronously within the grain context after events are persisted; they block the grain until complete. Why: Ensures effects finish before the next command is processed.
- Event effects **MUST** inherit from `EventEffectBase<TEvent, TAggregate>` (if yielding additional events) or `SimpleEventEffectBase<TEvent, TAggregate>` (if performing side operations only). Why: Provides strongly-typed event handling with proper async enumerable support.
- Event effects **SHOULD** be placed in an `Effects` sub-namespace under the aggregate (e.g., `Aggregates/BankAccount/Effects/`). Why: Source generators discover effects by namespace convention.
- Event effects can yield additional events via `IAsyncEnumerable<object>`, which are persisted immediately; this enables streaming scenarios (e.g., LLM token streaming, progressive data fetch). Why: Allows effects to produce follow-up events that update projections in real-time.
- Event effects **SHOULD** complete quickly (sub-second typical); a warning is logged if an effect takes longer than 1 second. Why: Long-running effects block grain throughput.
- For long-running background work triggered by events, event effects **SHOULD** dispatch commands to other grains or use Orleans reminders/timers rather than performing inline processing. Why: Avoids blocking the originating grain.
- Event effects **MUST** be stateless and registered as transient services; the framework auto-registers them via `AddEventEffect<TEffect, TAggregate>()`. Why: Ensures effects are instantiated per invocation with correct DI scope.
- Event effects can inject Orleans services (e.g., `IAggregateGrainFactory`, `IGrainContext`) to dispatch commands to other aggregates. Why: Enables cross-aggregate workflows like the Spring sample's `HighValueTransactionEffect`.

### Fire-and-Forget Event Effects

- Fire-and-forget effects **MAY** be used for async side effects that should not block the aggregate grain (e.g., external API calls, notifications, analytics). Why: Enables background processing without impacting command latency.
- Fire-and-forget effects **MUST** inherit from `FireAndForgetEventEffectBase<TEvent, TAggregate>`. Why: Provides strongly-typed event handling with dedicated worker grain execution.
- Fire-and-forget effects run in a separate worker grain, not the aggregate grain; they provide Orleans single-threaded guarantees but otherwise are infrastructure. Why: Keeps aggregate grains fast while effects can take longer.
- Fire-and-forget effects **MUST NOT** yield additional events; if further state changes are needed, the effect **MUST** dispatch commands through the normal aggregate command API. Why: Maintains event stream integrity and aggregate ownership of state transitions.
- Fire-and-forget effects **SHOULD** be placed in an `Effects` sub-namespace under the aggregate (same as regular event effects). Why: Source generators discover effects by namespace convention.
- Fire-and-forget effects are registered automatically via `AddFireAndForgetEventEffect<TEffect, TEvent, TAggregate>()` when using source generators. Why: Follows the same discovery pattern as regular event effects.
- Use fire-and-forget effects when the effect may take significant time (external HTTP calls, third-party integrations) and blocking the aggregate is unacceptable. Why: p99 latency improves when slow side effects are offloaded.

### Storage Providers

- Cosmos DB **SHOULD** be used as the default storage provider for brooks (events) and snapshots; it lends itself well to event sourcing's append-only writes and Aspire integration. Why: Provides scalable, globally distributed storage with excellent developer experience.
- Custom storage providers **MAY** be implemented when Cosmos is not suitable; the framework's storage abstractions allow pluggable backends. Why: Preserves flexibility for different deployment scenarios.
- New projects **SHOULD** use Aspire to set up local development with emulators. Why: Enables consistent local development experience.
- The Spring sample demonstrates this setup using Cosmos for event sourcing and Azure Storage for Orleans clustering/grain state. Why: Provides reference implementation for storage configuration.
- Storage client registrations **MUST** use keyed services following the patterns in `Spring.Runtime/Program.cs`. Why: Enables multiple storage accounts for different purposes.

### Framework Attributes Reference

Contributors **SHOULD** review all custom attributes under `src/` (particularly in `Inlet.Generators.Abstractions/` and `Brooks.Abstractions/Attributes/`) to understand their behavior.

| Attribute | Purpose | When to Use | Relates To |
|-----------|---------|-------------|------------|
| `[BrookName]` | Identifies the event stream via hierarchical name `(APP, MODULE, NAME)` | Required on all aggregates and projections that share an event stream | Event stream alignment; projections and aggregates with matching brook names share events; names are immutable once deployed—use uppercase alphanumeric segments (see `.github/instructions/storage-type-naming.instructions.md`) |
| `[SnapshotStorageName]` | Stable snapshot storage identity with versioning `(APP, MODULE, NAME, version)` | Required on aggregates and projections to persist state | Snapshot naming and storage; version enables schema evolution; names are immutable once deployed |
| `[SnapshotRetention]` | Sets the checkpoint retention modulus for a state type | Optional on snapshot-enabled aggregates and projections; omit it to use a configured override or fallback | Stable storage-name and CLR type-name overrides take precedence, then this attribute, then the global default (50 unless configured); the modulus must be positive |
| `[EventStorageName]` | Stable event storage identity with versioning `(APP, MODULE, NAME, version)` | Required on all event types | Event versioning; enables safe refactoring without breaking stored events; names are immutable once deployed |
| `[GenerateAggregateEndpoints]` | Generates runtime registration, gateway controller, and client feature code | Required on aggregate records exposed via API | Endpoint generation; creates `Add{Aggregate}()` extension methods |
| `[GenerateProjectionEndpoints]` | Generates read-only GET endpoint and SignalR subscription code | Required on projections exposed to clients | Endpoint generation; creates projection controller and client subscription |
| `[GenerateCommand]` | Exposes command as HTTP POST endpoint with generated client action | Required on commands that should be callable from UX | Command exposure; `Route` property controls endpoint path |
| `[ProjectionPath]` | Defines subscription and API path for projections | Required on server projections and matching client DTOs | Subscription routing; path must match between server and client |
| `[GenerateSerializer]` | Orleans serialization support | Required on all types that cross grain boundaries | Orleans serialization; requires pairing with `[Id(n)]` on properties—IDs start at 0 and must be unique per inheritance level |
| `[Alias]` | Stable Orleans type identity that survives refactoring | Required on all serialized types | Type versioning; use fully qualified name format |

## Scope and Audience

Applies to all contributors building sample applications or new features using the Mississippi framework. These rules ensure samples remain consistent, idiomatic, and serve as reference implementations for framework consumers.

## Feature workflow

For implementing or assessing an event-sourced application feature, use
[implement-event-sourced-feature](../../.agents/skills/implement-event-sourced-feature/SKILL.md)
with the [local source bindings](../agent-guidance/event-sourced-feature-bindings.md).
Read these files directly if skill discovery is unavailable. The Rules above
apply independently of skill activation.

## Consistency Model Separation

Read the [write/read consistency binding](../agent-guidance/event-sourced-feature-bindings.md#consistency-model-separation)
for the authoritative aggregate and asynchronous projection boundaries.

## References

- Coding discipline (samples): `.github/instructions/coding-discipline.instructions.md`
- Framework patterns (src): `.github/instructions/framework-patterns.instructions.md`
- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
- Orleans conventions: `.github/instructions/orleans.instructions.md`
- Domain modeling: `.github/instructions/domain-modeling.instructions.md`
- Blazor UX guidelines: `.github/instructions/blazor-ux-guidelines.instructions.md`
- Keyed services: `.github/instructions/keyed-services.instructions.md`
- Testing: `.github/instructions/testing.instructions.md`
