---
applyTo: 'src/**'
---

# Framework development

Governing thought: Developer-facing consistency permits justified framework-infrastructure flexibility.

> Drift check: Inspect src/Reservoir.*/, src/DomainModeling.Runtime/, src/Inlet.*/, and current src/Inlet.Generators.Abstractions/ attributes before changing patterns. Root guardrails and the common src/AGENTS.md contracts remain effective; this mandatory reference retains the original src/** and framework-infrastructure scope.

## Rules (RFC 2119)

### Framework development

Scope: src/** framework infrastructure work. These allowances do not authorize strict sample deviations.

- F1: Framework **MUST** minimize cognitive load through common aggregates/projections/commands/events/actions/reducers/effects.
- F1.2: Framework **MUST** reuse its common primitives for new features when possible.
- F1.3: Framework **MUST** design APIs from developers' perspective first.
- F1.4: Necessary new primitives **SHOULD** use familiar immutable records, handlers/reducers, and DI registration.
- F2: Developer-authored aggregates/projections/handlers/reducers/effects **MUST** be testable without infrastructure.
- F2.2: Handlers/reducers/effects **MUST** be pure functions or simple injected classes without static state/hidden coupling.
- F2.3: New extensible primitives **MUST** make testability first-class and be redesigned if difficult to test.
- F2.4: Framework **SHOULD** provide Given/When/Then harnesses (`AggregateTestHarness`, `StoreScenario`). Spring.Domain is the reference; developer-tool/diagnostic/configuration infrastructure has different constraints and standard testing, with infrastructure-free testing less critical.
- F3: APIs **MUST** expose client actions/reducers/state/effects and server commands/handlers/events/reducers.
- F3.2: Internals **SHOULD** follow the exposed patterns when practical.
- F3.3: Internals **MAY** deviate only when infrastructure requirements demand it (Orleans integration, generators, provider-specific storage).
- F3.4: Internal deviations **MUST** have justified code comments.
- F3.5: Internal deviations **MUST NOT** leak to developers.
- F4: Framework **MUST** provide guiding bases/interfaces (`CommandHandlerBase`, `EventReducerBase`, `ActionEffectBase`, `StoreComponent`).
- F4.2: Bases **SHOULD** enforce invariants at runtime.
- F4.3: Hooks **SHOULD** support customization without abandoning patterns.
- F5: Generators **MUST** emit code following the patterns developers would write manually.
- F5.2: Generator output **SHOULD** be readable.
- F5.3: Generator output **SHOULD** match hand-written style. `[PendingSourceGenerator]` marks handwritten validation references awaiting generation. Review current `src/Inlet.Generators.Abstractions/` attributes.
- F6: Public contract APIs **MUST** live in *.Abstractions.
- F6.4: Framework **SHOULD** use its prescribed get-only DI/no-locator patterns; root's existing stronger global guards remain effective.
- F7: Client contracts: `IStore`, `IAction`, `IFeatureState`, `IActionReducer`, `IActionEffect`. Server: `ICommandHandler`, `IEventReducer`, `IEventEffect`, `IAggregate`, `IProjection`; `IRootCommandHandler`/`IRootReducer` compose dispatch. Generator attributes include GenerateCommand/GenerateAggregateEndpoints/GenerateProjectionEndpoints.
- F7.2: Built-in Navigation/Lifecycle **MUST** use actions/reducers.
- F7.3: Framework **MAY** use internal state management for its own concerns (for example Inlet connection state).
- F7.4: Internal state **SHOULD** surface through patterns when useful.
- F7.5: Framework grains **SHOULD** follow aggregate patterns where applicable.
- F7.6: Worker/coordination grains **MAY** have custom patterns when orchestrating aggregates.
- F8: Unsupported requirements **MAY** use custom framework solutions.
- F8.2: Custom framework solutions **SHOULD** expose familiar interfaces.
- F8.3: Reusable edge-case patterns **SHOULD** be generalized.

## References

- Coding discipline (samples): `.github/instructions/coding-discipline.instructions.md`
- Abstractions: `.github/instructions/abstractions-projects.instructions.md`
- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
