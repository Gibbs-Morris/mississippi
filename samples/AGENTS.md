---
applyTo: 'samples/**,**/*.razor*'
---

# Applications and samples

Governing thought: Preserve applicable contracts through explicit scopes and verified source authority.

> Drift check: Root guidance and selected C# contracts remain effective. Read current Spring, Spring.Domain, framework source, and generator attributes before introducing patterns. Section scopes separate sample-only discipline, consumer framework usage, and Razor content; generic domain/storage contracts remain in src/AGENTS.md. For components, inspect current DI/logging/test settings and design-system assets.

## Rules (RFC 2119)

### Sample discipline

Scope: samples/** and contributors building sample applications. These strict obligations do not apply merely because framework-engine code is read alongside them.

- SD1: Sample client state **MUST** use Redux: actions up, state down, reducers transform, effects perform side effects.
- SD1.2: Domain state outside Reservoir **MUST NOT** use ad hoc management.
- SD2: Sample components **MUST** be dumb/presentational.
- SD2.2: Sample components **MUST NOT** call APIs/dispatch actions.
- SD2.3: Sample components **MUST** emit EventCallback events.
- SD2.4: Sample pages **MUST** inherit StoreComponent (InletComponent for SignalR), dispatch actions, and read `GetState<T>()`. Retain both source distinctions; ambiguous component/container classification requires investigation rather than silently dropping either obligation.
- SD3: Feature folders **MUST** contain `*State.cs`, `*Action.cs`, `*Reducers.cs`, `*FeatureRegistration.cs`.
- SD3.2: Client effects **MAY** be *ActionEffect.cs under ActionEffects/ as in Spring.
- SD4: Server business logic **MUST** stay in Domain Aggregates/Projections.
- SD4.2: All server writes **MUST** be aggregates.
- SD4.3: Server read views **MUST** project brook events.
- SD4.4: Server **MUST** use commands(intent), validating handlers emitting events, reducers applying state, and event effects for side effects.
- SD4.5: New behavior **MUST** use event/effect extensions.
- SD4.6: Sagas/workflows **MUST** be aggregates with orchestration.
- SD4.7: Sagas/workflows **MUST NOT** bypass aggregates.
- SD4.8: Outside Domain **MUST NOT** contain business rules; Runtime/Gateway only DI/config.
- SD5: Features **MUST** start with attributed domain aggregate/command/event/projection types.
- SD5.2: Available GenerateAggregateEndpoints/GenerateCommand/GenerateProjectionEndpoints attributes **MUST** trigger generation.
- SD5.3: Actions/DTOs/registrations **MUST** be generated.
- SD5.4: Manual code **MAY** exist only when no generator covers it.
- SD6: Nonconforming code **MUST** be refused/refactored.
- SD6.2: PRs with nonaggregate writes, non-Redux client domain state, or business logic outside Domain **MUST** be rejected.
- SD6.3: Extensions **MUST** prefer event effects/new projections over imperative additions.

### Framework usage

Scope: samples/** and the stated source audience of sample applications/new application features using Mississippi. Requirements explicitly about new repository samples remain restricted to those samples; framework-engine allowances are separate.

- SF1: New Spring patterns **MUST** update this policy in the same or immediately following PR.
- SF1.2: New src capabilities affecting sample construction **MUST** be documented here before sample use, preserving reference-pattern synchrony.
- SF2: Supported DTO/action/action-effect/endpoint/mapper concerns **SHOULD** use generators.
- SF2.2: Manual implementations **MAY** be used only when unsupported.
- SF2.3: `[PendingSourceGenerator]` types **MUST** serve generator validation only.
- SF2.4: `[PendingSourceGenerator]` types **MUST NOT** guide new development.
- SF2.5: Contributors **SHOULD** review Inlet.Client.Generators, Inlet.Gateway.Generators, Inlet.Generators.Abstractions attributes, and Reservoir.
- SF3: Domain is the generator source: attributed aggregates emit Runtime registration, Gateway controller, Client feature/state/reducers and Add{Aggregate}Feature(); GenerateCommand emits DTOs/mappers/HTTP endpoints/client actions/effects/command state; GenerateProjectionEndpoints emits Gateway controller/client subscription/DTOs; EventEffectBase/SimpleEventEffectBase effects emit AddEventEffect<TEffect, TAggregate>().
- SF4: Orleans hosts **MUST** compose Brooks via `silo.UseMississippi(runtime => runtime.AddEventSourcing(...))` and Hosting.Runtime RuntimeBuilder, which validates terminal registration/options attachment.
- SF4.2: New repository samples **MUST** have Orleans Runtime, ASP.NET Gateway, WASM Client, Domain.
- SF4.3: Aspire AppHost **SHOULD** orchestrate local emulators.
- SF4.4: Runtime/Gateway **MUST** be configuration/options/wiring/registration only.
- SF4.5: Domain **MUST** own all server state/logic.
- SF4.6: Client **MUST** own UX/UI/local state.
- SF5: Full WASM clients **MUST** compose inside one `builder.UseMississippi(client => ...)` Hosting.Client ClientBuilder callback. Terminal attachment validates/commits; client/nested builders cannot configure afterward.
- SF6: Client domain/business state **MUST** use Reservoir store/actions/reducers.
- SF6.2: Ephemeral hover/focus/form state **MAY** remain component-local.
- SF6.3: State reads/dispatch **MUST** use store, never ad hoc domain state.
- SF6.4: Contributors **SHOULD** review Reservoir.
- SF6.5: Third-party libraries **MUST** integrate Reservoir's state model.
- SF6.6: Libraries with incompatible internal state **SHOULD NOT** be general UI.
- SF6.7: Manual actions **SHOULD** be {Command}Action/{Command}ExecutingAction/{Command}SucceededAction/{Command}FailedAction; generated conventions remain valid.
- SF7: Components **MUST** use atomic Atoms/Molecules/Organisms/Templates/Pages. Parameters (including cascading) carry state down and EventCallback domain events up.
- SF7.2: Atoms/Molecules **MUST NOT** call APIs/dispatch.
- SF7.3: Atoms/Molecules **MUST** emit events handled by containers (Organisms/Pages). Containers dispatch store actions; effects respond. Select the Blazor section for detailed component contracts.
- SF8: Inlet spans domain/frontend and **MUST** generate actions from aggregate GenerateCommand commands; generated action-command mapping is framework-owned.
- SF8.2: Client features **MUST** register with Add{Aggregate}Feature(); runtime/gateway use Add{Aggregate}() under registration contracts.
- SF9: Generated action success: HTTP request → server handling → aggregate activation/retrieval → command validation/events → framework brook append and configured snapshot update → projection event consumption/state update → asynchronous SignalR notification to subscribed clients. Near-real-time is not instant. Validation failure returns error code, produces no events, leaves projections unchanged. Read `.github/agent-guidance/event-sourced-feature-bindings.md#consistency-model-separation` for authoritative aggregate/asynchronous projection boundaries.
- SF10: Screens **SHOULD** subscribe to many small projections; each surface needs only its subset, while one event may update multiple projections.
- SF10.2: Subscribe/unsubscribe **MUST** use Inlet APIs.
- SF10.3: Client DTO ProjectionPath **MUST** match server path.
- SF11: Contributors **SHOULD** review Spring.Domain. Generic domain type/serialization/handler contracts, including visibility and serialized properties, in [src/AGENTS.md](../src/AGENTS.md#event-sourced-domain) remain binding.
- SF11.2: API aggregates additionally **MUST** use GenerateAggregateEndpoints.
- SF11.3: UX commands **MUST** use GenerateCommand(Route="...").
- SF11.4: Handlers **MUST** validate business logic/current state before events and return typed success/error results; framework owns persistence/snapshots.
- SF12: Pre-1.0 event shapes **MAY** change freely.
- SF12.2: Pre-1.0 branch-only patterns **MUST NOT** get V2 types/shims, with main the compatibility baseline.
- SF12.3: At 1.0+ OR after writing to a real non-test store, written event property names/types **MUST NOT** change: immutable facts form an append-only log.
- SF12.4: At 1.0+ or for written real-store events, adding properties **MAY** occur but is not always advisable.
- SF12.5: For significant event schema changes, a new event type ({Event}V2) **SHOULD** be added alongside the original.
- SF13: Multiple projections **MAY** share a brook.
- SF13.2: Projections **SHOULD** be small/componentized.
- SF13.3: Core domain projection attributes apply; client-facing projections additionally **MUST** use ProjectionPath/GenerateProjectionEndpoints.
- SF13.4: Projection reducers **MUST** inherit EventReducerBase<TEvent,TProjection> and return new instances.
- SF14: Brooks **MUST** use BrookName(APPNAME,MODULENAME,NAME).
- SF14.2: Developers **MUST NOT** work with brooks directly, since framework aligns aggregate/projection attributes.
- SF14.3: Each aggregate **MUST** have exactly one brook.
- SF14.4: Many UX projections **MAY** consume a brook.
- SF14.5: Grain design **MUST** avoid oversized master bottlenecks.
- SF14.6: When scalability requires it, a family of grains/aggregates sharing a business identifier but storing separate aspects **MAY** scale out, each retaining its own 1:1 brook.
- SF14.7: Grain operations **SHOULD** be fast, avoiding long work.
- SF15: Client action effects **MAY** react with notifications/navigation/follow-up actions; they run after reducers and may emit multiple async actions.
- SF15.2: Client action effects **SHOULD** dispatch/call services rather than complex inline logic.
- SF15.3: Client action effects **MUST NOT** be confused with server grain event effects.
- SF16: Server event effects **MAY** react to persisted events (cross-aggregate commands/notifications/audit). They run synchronously in the grain after persistence and block its next command until completion.
- SF16.2: Server event effects **MUST** inherit EventEffectBase<TEvent,TAggregate> for yielded events or SimpleEventEffectBase<TEvent,TAggregate> for side operations.
- SF16.3: Server event effects **SHOULD** be in aggregate Effects namespaces.
- SF16.4: Server event effects **SHOULD** complete quickly (typically subsecond; warning after 1s). `IAsyncEnumerable<object>` events persist immediately, enabling LLM-token/progressive streaming.
- SF16.5: Long-running event work **SHOULD** dispatch elsewhere/reminders/timers.
- SF16.6: Server event effects **MUST** be stateless/transient via AddEventEffect<TEffect,TAggregate>(); injected IAggregateGrainFactory/IGrainContext may coordinate, e.g. HighValueTransactionEffect.
- SF17: Fire-and-forget effects **MAY** offload external HTTP/third-party/analytics work.
- SF17.2: Fire-and-forget effects **MUST** inherit FireAndForgetEventEffectBase<TEvent,TAggregate>. Separate worker grains retain Orleans single-threaded guarantees but are infrastructure.
- SF17.3: Fire-and-forget effects **MUST NOT** yield events.
- SF17.4: Changes from fire-and-forget effects **MUST** dispatch normal aggregate commands.
- SF17.5: Fire-and-forget effects **SHOULD** use aggregate Effects namespaces; generation auto-registers AddFireAndForgetEventEffect<TEffect,TEvent,TAggregate>(). Use them when significant work would unacceptably block aggregates/p99.
- SF18: Cosmos **SHOULD** default for event brooks/snapshots.
- SF18.2: Alternative storage **MAY** use pluggable storage abstractions.
- SF18.3: New projects **SHOULD** use Aspire emulators; Spring uses Cosmos event sourcing and Azure Storage Orleans clustering/state.
- SF18.4: Storage clients **MUST** use keyed Spring.Runtime/Program.cs patterns.
- SF19: Contributors **SHOULD** review all src custom attributes, especially Inlet.Generators.Abstractions and Brooks.Abstractions/Attributes.
- SF19.2: BrookName identifies APP.MODULE.NAME alignment and **MUST** use uppercase-alphanumeric segments; event/snapshot storage identities are versioned, stable after deployment, and required on their respective types.
- SF19.3: Optional SnapshotRetention on snapshot-enabled types uses stable storage-name override → CLR-name override → attribute → global default 50 (unless configured); modulus must be positive. Omission uses configured override/fallback.
- SF20: GenerateAggregateEndpoints is required for API aggregates and emits Runtime registration/Gateway controller/client feature plus Add{Aggregate}().
- SF20.2: GenerateProjectionEndpoints is required for client projections and emits read-only GET/SignalR.
- SF20.3: GenerateCommand is required for UX commands with Route controlling POST endpoint/action path.
- SF20.4: ProjectionPath is required on server/client subscription types and must match.
- SF20.5: Cross-grain types require GenerateSerializer with Id(n), 0-based/unique per inheritance level.
- SF20.6: Alias provides stable type identity in fully qualified-name format.

### Blazor

Scope: All **/*.razor* paths/content and Blazor component/page authoring/review, including Razor outside samples. Sample-only stronger discipline remains scoped separately.

- B1: Razor authoring/review **MUST** follow atomic Atoms/Molecules/Organisms/Templates/Pages, one component folder with .razor/.razor.cs/styles/tests.
- B1.2: Markup/partial logic **MUST** split.
- B1.3: Component classes **MUST** be sealed unless extensibility is required.
- B1.4: View-only components **MUST** expose Parameter/EventCallback.
- B1.5: Children **MUST NOT** call APIs/side effects.
- B1.6: Domain logic **MUST** be outside UI.
- B2: Redux actions/reducers/selectors/effects **SHOULD** be used.
- B2.2: Selectors **MUST** feed components instead of raw state.
- B2.3: Effects **MUST** use interfaces for IO.
- B3: Templates **MUST NOT** fetch data.
- B3.2: Razor markup **MUST NOT** inject (use partial class).
- B3.3: Shared components **MUST NOT** depend on server-only code.
- B3.4: Parameters **MUST** be PascalCase.
- B3.5: Atoms **MUST NOT** rely on global styles.
- B3.6: Organisms **MUST NOT** directly access data stores.
- B4: Interactive atoms **MUST** support keyboard/required ARIA.
- B4.2: Components **MUST** have L0 state-transition/callback tests.
- B5: Atoms **SHOULD** forward AdditionalAttributes.
- B5.2: Duplicated markup **SHOULD** become slots/parameters.
- B5.3: Resource-owning pages **SHOULD** implement IAsyncDisposable.
- B5.4: Global theming overrides **SHOULD NOT** be required.
- B5.5: Missing accessibility audits **SHOULD** be tracked.

## References

- samples/Spring/ and Spring.Domain/ (authoritative reference patterns)
- src/Inlet.Client.Generators/; src/Inlet.Gateway.Generators/; src/Inlet.Generators.Abstractions/; src/Reservoir/; src/DomainModeling.Runtime/
- src/DomainModeling.Abstractions/ and src/Tributary.Abstractions/ for base signatures
- Root global issue/stack/testing/compatibility/UX policies; src/AGENTS.md C#/Orleans/registration/placement contracts
- Existing event-sourced-feature skill/source binding remain unchanged; read direct linked sources when discovery is unavailable
