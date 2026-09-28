---
applyTo: 'samples/**,**/*.cs,**/*.cs,**/*.csproj,**/Program.cs,**/*.razor,src/**,**/*.{cs,razor,css},**/Aspire*/**/*.cs'
---

# Source contracts

Governing thought: Preserve applicable contracts through explicit scopes and verified source authority.

> Drift check: Root guidance remains effective. Sections retain their own path, content, and role triggers; this file's location does not make sample discipline apply to framework internals. Inspect current build props/packages, analyzers, scripts, and the referenced implementations before changing their contracts.

## Rules (RFC 2119)

### C\#

Scope: C# paths (**/*.cs), and C# authored/reviewed/explained as content, including examples outside src.

- C1: Code **MUST** follow SOLID and be unit-testable through clear seams/DI.
- C1.2: Blocking calls and shared mutable state **MUST NOT** be introduced.
- C2: .NET analyzers **MUST** remain enabled; warnings are errors. Root engineering guardrails retain approval requirements for suppressions and pragmas.
- C3: XML documentation **MUST NOT** contain `<example>` or `<code>` samples; refer to real implementations to avoid stale examples.
- C4: Configuration **MUST** use `IOptions<T>`, `IOptionsSnapshot<T>`, or `IOptionsMonitor<T>`.
- C4.2: Constructors **MUST NOT** take raw configuration primitives.
- C5: Nested classes **SHOULD NOT** be used except for truly private implementation details.
- C5.2: Test helpers and public/internal types **MUST** be top-level or separate files; nesting impedes mocking and discovery.
- C6: Types **MUST** default to internal.
- C6.2: Public/protected/unsealed types **MUST** justify access in XML comments.
- C6.3: Implementations **MUST** stay internal unless public API.
- C6.4: Consumer options/registration classes **MAY** be public.
- C6.5: Other options/registration classes **SHOULD** remain internal.
- C6.6: Classes **SHOULD** be records/immutable where feasible and inheritable only with clear need.
- C6.7: Interfaces **SHOULD** be public only for deliberate API.
- C6.8: Members **SHOULD** expose least privilege.
- C7: New/refactored APIs **MUST** follow BCL/runtime conventions (`Try*`/`Parse`, `Async`, `CancellationToken` for cancelable work).
- C7.2: API ambiguity **MUST** resolve to widely used .NET APIs.
- C7.3: Grain APIs **MUST** follow Orleans conventions, including `Task`/`Task<T>` and no synchronous blocking.
- C8: Orleans code **MUST NOT** use `Parallel.ForEach` or chatty inter-grain calls; prefer async and `Task.WhenAll` to preserve the threading model.
- C9: Public contracts **SHOULD** live in `.Abstractions`.
- C10: New third-party dependencies **MAY** be added only with explicit approval or when extending already-adopted technology.
- C11: Code needing current date/time **MUST** inject `TimeProvider`, not call `DateTime.Now`, `DateTime.UtcNow`, or `DateTimeOffset.UtcNow`.
- C11.2: Tests **SHOULD** use `Microsoft.Extensions.TimeProvider.Testing.FakeTimeProvider` for deterministic assertions.

### Naming

Scope: All C# paths/content; applicable exposed public/internal symbols and domain types.

- N1: SA13xx/SA16xx naming/documentation violations **MUST** break builds.
- N1.2: SA13xx/SA16xx naming/documentation violations **MUST** be fixed, not suppressed.
- N2: Namespaces **MUST** be feature-oriented, with at most ten PascalCase segments, no underscores or technical Services/Models silos.
- N2.2: Namespace abbreviations **MUST** be industry-standard.
- N3: Types **MUST** be PascalCase nouns.
- N3.2: Interfaces **MUST** prefix `I`.
- N3.3: Enums **MUST** be singular with PascalCase members.
- N4: Methods **MUST** be PascalCase verb phrases.
- N4.2: Properties **MUST** be PascalCase nouns.
- N4.3: Booleans **MUST** start `Is/Has/Can/Should`.
- N4.5: Private fields/locals **MUST** be camelCase without underscores.
- N4.6: Constants **MUST** be PascalCase.
- N5: Public and exposed internal symbols **MUST** have factual XML docs, no TODOs, imperative `<summary>`, and applicable `<param>`, `<typeparam>`, `<returns>`.
- N6: Abstract classes **SHOULD NOT** use `Base` unless intended for inheritance.
- N6.2: Orleans abstract grains **MAY** follow their Base convention.
- N6.3: Private/internal member docs **SHOULD** exist only for nontrivial behavior or `InternalsVisibleTo` exposure.
- N7: Retain illustrative event-sourcing suffixes in `.github/agent-guidance/event-sourced-feature-bindings.md#domain-type-suffix-examples`; general naming above remains authoritative.

### Orleans

Scope: All C# paths/content; clauses apply to Orleans grains/interfaces and serialized types when their triggers apply.

- O1: Grains **MUST** implement `IGrainBase` with `public IGrainContext GrainContext { get; }`.
- O1.2: Grains **MUST NOT** inherit `Grain`.
- O1.3: Concrete grains **MUST** be sealed.
- O1.4: All dependencies, including `IGrainContext`, **MUST** be constructor-injected and stored as get-only properties, never private readonly DI fields.
- O1.5: Interfaces **MUST** be public only for external callers.
- O2: `using Orleans.Runtime;` **MUST** be included.
- O2.2: Orleans extension methods **MUST** use `this.`, for example `this.GetPrimaryKeyString()` and `this.DeactivateOnIdle()`.
- O3: Legacy `Grain` implementations **SHOULD** migrate to POCO.
- O3.2: Deferred grain migrations **SHOULD** be tracked in `.scratchpad/tasks`.
- O3.3: Abstract `IGrainBase` classes **MUST** end `Base`.
- O3.4: Conversions from `Grain<TState>` **SHOULD** inject `IPersistentState<TState>`.
- O4: Serializable types **MUST** use explicit `[GenerateSerializer]`, `[Id(n)]` on every serialized member, and globally unique `[Alias]`.
- O4.2: Implicit serialization **MUST NOT** be used.
- O4.3: IDs **MUST** start at 0 per inheritance level and be unique there.
- O5: Pre-1.0, member IDs/type shapes **MAY** change freely.
- O5.2: Pre-1.0 branch-only serialization layouts **MUST NOT** acquire compatibility shims. Apply root compatibility policy; this does not erase separate real-store safeguards.
- O6: At 1.0+, existing IDs **MUST NOT** change.
- O6.2: At 1.0+, IDs **MUST** remain unique for their inheritance level.
- O6.3: At 1.0+, breaking changes such as record-to-class/signedness **MUST NOT** occur.
- O6.4: At 1.0+, new members **MUST** use unused IDs.
- O6.5: At 1.0+, versioning **MUST** be additive/compatible.
- O6.6: At 1.0+, removals **SHOULD** use `[NonSerialized]`/`[Obsolete]`.
- O6.7: At 1.0+, widening numerics/making properties nullable **SHOULD** be preferred to narrowing to preserve reads/rolling deployments.
- O7: Projects **MUST** include required Orleans packages and treat analyzers as errors.
- O7.2: Reviewers **SHOULD** verify serialization compatibility.
- O7.3: Unfixed analyzer violations/missing attributes **SHOULD** have focused `.scratchpad/tasks/pending` items.

### Registration and keyed services

Scope: All C# paths/content select this section, not only files under a hosting directory or tasks explicitly naming DI. Clauses apply to feature registration, options, library clients, and host wiring as stated.

- R1: Each feature **MUST** expose `public static class {Feature}Registrations` and namespace-hierarchical `Add{Feature}()` extensions (`AggregateRegistrations`, `ReducerRegistrations`, `InletSiloRegistrations`).
- R1.2: Parent registrations **MUST** call children.
- R1.3: Sub-feature registrations **MUST** stay internal.
- R1.4: Public registration boundaries **MUST** be product/features with XML docs.
- R2: Registration **MUST** be synchronous, with no async DB/HTTP work.
- R2.2: Async initialization **MUST** be deferred to `IHostedService`/Orleans lifecycle participants.
- R2.3: Async factories **MUST** be registered for deferred work, never block registration, preventing startup deadlocks.
- R3.2: Registration **MUST** offer `Action<TOptions>`, `IConfiguration`, and explicit-parameter overloads.
- R3.3: `{Feature}Options` **MUST** have sensible defaults and validation (`ValidateOnStart`/`IValidateOptions`).
- R4: Connection strings/external clients **MUST** be accepted/configured through factories. Root CPM forbids project-local package versions.
- R5: Registration classes **SHOULD** be sealed/minimal.
- R5.2: Configuration **SHOULD** be externalized for cloud-native operation.
- K1: Library cloud-client consumers (BlobServiceClient, CosmosClient, Container, etc.) **MUST** constructor-request `[FromKeyedServices(<ModuleDefaults>.XxxServiceKey)]`, not unkeyed registrations, allowing different locking/state/upload/archive accounts.
- K2: Keys **MUST** be owned by the storage-contract package (`BrookCosmosDefaults`, `SnapshotCosmosDefaults`).
- K2.2: Keys **MUST NOT** use a cross-module defaults hub.
- K2.3: Keys **MUST** follow `mississippi-{client-type}-{feature}` (`mississippi-cosmos-brooks`, `mississippi-blob-locking`).
- K3: Registration documentation **MUST** comment which keyed services callers supply.
- K4: Hosts **MUST** forward their keys (Aspire `cosmos`/`blobs`) to library keys with `AddKeyedSingleton`.
- K4.2: When a host needs keyed clients for the library and unkeyed clients for its own services, it **MUST** explicitly forward using `AddSingleton(sp => sp.GetRequiredKeyedService<T>("key"))`.
- R6: For registration work, use `.agents/skills/register-dotnet-services/SKILL.md` with `.github/agent-guidance/service-registration-bindings.md`. If discovery is unavailable or applicability unclear, read both directly; these Rules remain effective independently.

### Logging

Scope: All C# paths/content select this section; requirements apply when emitting or encountering logs.

- L1: All logs **MUST** use classes suffixed `LoggerExtensions` with `[LoggerMessage]` partial methods.
- L2: Public services **MUST** log entry/successful completion.
- L2.2: Every catch **MUST** log exceptions with context.
- L3: Mutations, external calls, and batches **MUST** log identifiers/counts.
- L3.2: Operations over ~1s or significant allocations (>10MB) **MUST** capture timing/size.
- L4: Grains **MUST** log activation/deactivation and public calls with timing.
- L4.2: Business-rule violations and event append/read operations **MUST** be logged.
- L5: Messages **MUST** be structured and descriptive for AI debugging.
- L5.2: PII/secrets **MUST NOT** be logged.
- L5.3: Correlation IDs/relevant parameters **SHOULD** be included, with sensitive values masked.
- L6: Encountered direct `ILogger` usage **MUST** create a `.scratchpad/tasks` conversion item.
- L7: For adding/converting logs, use `.agents/skills/add-dotnet-source-generated-logging/SKILL.md`; Rules apply whether selected or available. Inspect current LoggerExtensions/configuration for levels/providers.

### Placement

Scope: Matching .cs/.razor/.css paths and placement content; adding/moving source in src/tests/samples. Exclude generated/intermediate files (obj/**, bin/**, generated outputs).

- P1: Placement **MUST** be deterministic from project identity, namespace segments, and approved archetypes, never ad hoc.
- P1.2: Namespaces **MUST** mirror folders except explicitly approved roots (`GlobalUsings.cs`, assembly-info).
- P1.3: Roots **MUST** use the effective `RootNamespace`/nearest props, not assume repository props.
- P1.4: Samples **MUST** use nearest sample props.
- P1.5: Samples **MUST NOT** prepend Mississippi unless their props do.
- P2: Folders **MUST** enforce 20 files per extension (.cs/.css/.razor).
- P2.2: Mixed totals **MAY** exceed 20 if each type does not.
- P2.3: Earlier useful vertical splits **MAY** occur.
- P2.4: Per-type excess **MUST** split vertically.
- P3: Services/Models/Helpers/Utils/Common technical buckets **MUST NOT** be introduced unless established domain feature names with explicit justification.
- P4: Aggregate/Projection/Saga archetypes **MUST** stay consistent across src/tests/samples.
- P4.2: Tests **SHOULD** mirror production vertical structure.
- P4.3: Deviations **MUST** carry reason, approval metadata, and evidence, never convenience/preference.
- P4.4: Exceptions **SHOULD** stay <=1% of mapped files.
- P4.5: Exceeding 1% exceptions **MUST** stop for reassessment/recorded decision.
- P5: Conflict precedence **MUST** be valid bounded-context naming, archetype consistency, per-type cap split within the branch, then deterministic qualifiers.
- P5.2: Enforcement **MUST** emit machine-readable inventory/reconciliation and fail on unresolved collisions, caps, missing approvals/mappings.
- P6: Canonical segments: Aggregate=`Aggregates/<Name>/{Commands,Events,Reducers,Effects,State,Handlers,Registrations}`; Projection=`Projections/<Name>/{Reducers,Effects,State,Handlers,Contracts,Registrations}`; Saga=`Sagas/<Name>/{Commands,Events,Reducers,Effects,State,Compensation,Registrations}`.

### DDD analysis

Scope: Union `**/*.cs, **/*.csproj, **/Program.cs, **/*.razor` and domain/application/infrastructure/UI-shell content where DDD/SOLID choices matter.

- D1: Domain-sensitive work **MUST** start with written bounded-context, aggregate/value-object/service/event, pattern, and security/compliance analysis.
- D1.2: Before coding, agents **MUST** plan changed domain primitives and tests.
- D2: Domain logic **MUST** stay in aggregates/value objects/domain services.
- D2.2: Application services **MUST** orchestrate.
- D2.3: Infrastructure **MUST** remain isolated under registration guidance.
- D3: Test strategy **MUST** use PascalCase names, L0-first, >=80% overall/95–100% target where feasible,100% touched code, and proportionate mutation as an additional signal under root testing policy.
- D4: Financial rules **MUST** use decimal value objects, explicit rounding, and recorded domain events.
- D5: Before completion, agents **MUST** confirm SOLID, event publication, security boundaries, and documentation/tasks.

### Aspire integration

Scope: Matching **/Aspire*/**/*.cs paths and Aspire integration-test/emulator content; source rules remain effective regardless of skill selection.

- A1: Cosmos emulator **MUST** use `RunAsEmulator()` with `WithoutHttpsCertificate()` on Aspire 13.6 or later. Why: `RunAsEmulator()` selects the Linux vNext emulator with an HTTP `/ready` endpoint; `RunAsPreviewEmulator()` is obsolete. See [Aspire 13.6 breaking changes](https://aspire.dev/whats-new/aspire-13-6/#breaking-changes).
- A2: `CosmosClientOptions` **MUST** set `LimitToEndpoint = true` when connecting to any emulator. Why: Keeps requests on the configured emulator endpoint instead of discovering other regions. See [the SDK property reference](https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.cosmos.cosmosclientoptions.limittoendpoint).
- A2.2: `CosmosClientOptions` **SHOULD** use `ConnectionMode.Gateway` for emulator connections. Why: Gateway mode is more reliable than Direct TCP for local emulators.
- A3: Cosmos document models **MUST** use `[Newtonsoft.Json.JsonProperty("id")]` not `System.Text.Json` attributes. Why: Cosmos SDK v3 uses Newtonsoft.Json by default; STJ attributes are ignored.
- A4: Aspire test projects **SHOULD** use `IAsyncLifetime` fixture pattern to manage AppHost lifecycle. Why: Ensures proper startup/teardown and resource cleanup.
- A5: For authoring/extending an owned Cosmos emulator test fixture, use `.agents/skills/author-cosmos-integration-tests/SKILL.md` with `.github/agent-guidance/cosmos-integration-bindings.md`; read both directly when discovery/applicability is unclear. Before changing emulator configuration, check the referenced Aspire Cosmos/SDK issues for updates.

### Event-sourced domain

Scope: All matching C# paths/content select this section; domain clauses concern Mississippi event-sourced models in samples or applications, not arbitrary unrelated C# types.

#### Domain record visibility

- DM0: Aggregate, command, and projection records **MUST** be internal by default and **MUST** be public when their types occur in public generated API signatures or are discovered through exported-type scanning.
- DM0.2: Contributors **MUST** verify visibility against the consuming generator and registration path. Friend-assembly access does not relax public-signature accessibility requirements.

Inlet's [aggregate controller generator](Inlet.Gateway.Generators/AggregateControllerGenerator.cs) exposes the aggregate type in its public base class and command types in public mapper constructor parameters. [Projection assembly scanning](Inlet.Runtime/InletSiloRegistrations.cs) uses `GetExportedTypes()`. These paths require public domain records; records without a public or discovery boundary remain internal.

#### Domain types and behavior

- DM1: Aggregates **MUST** be sealed records with the visibility defined above and `[BrookName]`, `[SnapshotStorageName]`, `[GenerateSerializer]`, `[Alias]`, and get/init properties with sensible defaults.
- DM1.2: Aggregate properties **MUST** have unique sequential `[Id(n)]` starting at 0.
- DM1.3: A creation sentinel (`IsCreated`, `IsInitialized`) **SHOULD** distinguish initial state.
- DM2: Commands **MUST** be sealed records with the visibility defined above and `[GenerateSerializer]`/`[Alias]`.
- DM2.2: Serialized command properties **MUST** have `[Id(n)]`. Commands **SHOULD** express caller-supplied inputs through constructor parameters or required init-only properties; intentionally optional inputs **MAY** use explicit defaults. Handlers **MUST** validate runtime input values.
- DM2.3: Command names **SHOULD** be verb phrases (`CreateChannel`, `UpdateDisplayName`).
- DM3: Events **MUST** be internal sealed records with `[EventStorageName]`, `[GenerateSerializer]`, `[Alias]`.
- DM3.2: Event names **MUST** be past tense (`ChannelCreated`, `MessageSent`).
- DM3.3: Event properties **MUST** be required with `[Id(n)]`.
- DM4: Handlers **MUST** be internal sealed classes inheriting `CommandHandlerBase<TCommand, TAggregate>`, named `{Command}Handler` (`CreateChannelHandler`), and implement `HandleCore()` returning `OperationResult<IReadOnlyList<object>>`.
- DM4.2: Command properties **MUST** be validated before aggregate state.
- DM4.3: Invalid commands **MUST** return `AggregateErrorCodes.InvalidCommand`, state conflicts `AggregateErrorCodes.InvalidState`.
- DM5: Aggregate reducers **MUST** be internal sealed classes inheriting `EventReducerBase<TEvent, TAggregate>`, named `{Event}Reducer` (`ChannelCreatedReducer`), and return new state through with/constructors.
- DM5.2: Reducer input **MUST NOT** be mutated; returning the same instance throws at the base.
- DM6: Projections **MUST** be sealed records with the visibility defined above and `[BrookName]`, `[SnapshotStorageName]`, `[GenerateSerializer]`, `[Alias]`.
- DM6.2: Projection names **SHOULD** be `{Name}Projection` (`UserProfileProjection`, `ChannelMemberListProjection`).
- DM7: Projection reducers **MUST** be internal sealed `EventReducerBase<TEvent, TProjection>` classes named `{Event}ProjectionReducer` (`UserRegisteredProjectionReducer` vs `UserRegisteredReducer`).
- DM7.2: Projection reducers **SHOULD** live in `Projections/{ProjectionName}/Reducers/`.
- DM8: `[BrookName]` **MUST** use uppercase-alphanumeric `("APPNAME", "MODULENAME", "NAME")`.
- DM8.2: Event/snapshot storage attributes **MUST** use that format plus `version: n` (default 1).
- DM8.3: Storage names **MUST NOT** change after persistence.
- DM8.4: `[Alias]` **MUST** match the fully qualified type name, e.g. `Contoso.Domain.Channel.Events.ChannelCreated`.
- DM9: Domain registration **MUST** use public `Add{Domain}Domain()` calling private `Add{Aggregate}Aggregate()`/`Add{Projection}Projection()`, in `public static class {Domain}Registrations`.
- DM9.2: Domain registration order **MUST** be event types `AddEventType<>`, handlers `AddCommandHandler<>`, reducers `AddReducer<>`, snapshot converter `AddSnapshotStateConverter<>`.
- DM10: For implementing/assessing an event-sourced application feature, use `.agents/skills/implement-event-sourced-feature/SKILL.md` with `.github/agent-guidance/event-sourced-feature-bindings.md`; read both directly if discovery is unavailable. These Rules remain effective without skill activation.

### Persisted identity

Scope: All C# paths/content select this section; requirements concern creating/consuming persisted event-sourcing/storage types.

- ST1: Persisted events/snapshots/commands/etc. **MUST** have a naming attribute (EventStorageName/SnapshotStorageName) with explicit version.
- ST1.2: Computed storage names **MUST** be `APPNAME.MODULENAME.NAME.Vn`.
- ST1.3: App/module/name **MUST NOT** change once persisted.
- ST1.4: Persisted schema evolution **MUST** increment version.
- ST1.5: Storage naming attributes **MUST** be globally unique.
- ST1.6: Registries (IEventTypeRegistry) **MUST** resolve names↔types.
- ST1.7: Startup scanning **SHOULD** register all persisted types.
- ST1.8: CLR names **MAY** refactor with unchanged attribute identity.
- ST2: Removed members backed by a real, non-test store **SHOULD** retain prior versions/types for reads.
- ST2.2: Pre-1.0 without persisted production data **MAY** skip retaining prior versions/types; root compatibility policy does not waive real-store identity.

### Framework development

Scope: src/** framework infrastructure work. These allowances do not authorize strict sample deviations.

- FW-LOAD: For every matching src/** path or semantic framework-infrastructure task, agents **MUST** read .github/agent-guidance/framework-development.md in full before planning, reviewing, answering, or changing that work. Apply its original scope; reading the common C# profile for a snippet outside src does not alone activate this src-scoped reference.

## References

- Root AGENTS.md#engineering and root issue/stack/testing/compatibility policies
- Directory.Build.props; Directory.Packages.props; .editorconfig; eng/src/agent-scripts/
- samples/Spring/Spring.Domain/; src/Reservoir.*/; src/DomainModeling.Runtime/; src/Inlet.*/
- src/DomainModeling.Abstractions/; src/Tributary.Abstractions/; Microsoft.Orleans.Sdk / Microsoft.Orleans.CodeGenerator.MSBuild
- samples/{Spring,LightSpeed,Crescent}/Directory.Build.props
- <https://learn.microsoft.com/dotnet/orleans/serialization>; <https://github.com/dotnet/aspire/issues/7882>; <https://github.com/Azure/azure-cosmos-dotnet-v3/issues/5364>
- <https://github.com/dotnet/aspire/issues?q=cosmos+emulator>; <https://github.com/Azure/azure-cosmos-dotnet-v3/issues>
