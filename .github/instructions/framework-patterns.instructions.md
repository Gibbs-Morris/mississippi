---
applyTo: 'src/**'
---

# Framework Patterns (Mississippi Core)

Governing thought: Framework code prioritizes developer experience and minimizes cognitive overload by exposing consistent primitives—aggregates, projections, commands, events, actions, reducers, effects—throughout. Internal flexibility serves external consistency.

> Drift check: Review existing framework implementations in `src/Reservoir/`, `src/EventSourcing.Aggregates/`, and `src/Inlet.*/` before adding new patterns.

## Rules (RFC 2119)

### Developer Experience First

- Framework **MUST** minimize cognitive overload by reusing the same primitives everywhere. Why: Developers learn once, apply everywhere.
- New framework features **MUST** express themselves using existing primitives (aggregates, projections, commands, events, actions, reducers, effects) when possible. Why: Consistency reduces learning curve.
- When new primitives are necessary, they **SHOULD** follow the same shape (immutable records, handlers/reducers, DI registration patterns). Why: Familiarity accelerates adoption.
- Framework APIs **MUST** be designed from the developer's perspective first, then implemented. Why: Developer experience drives design.

### Testability by Design

- **Developer-authored primitives** (aggregates, projections, command handlers, reducers, effects)—the code developers write on top of the framework—**MUST** be testable without infrastructure dependencies. Why: This is the core value proposition; business logic stays pure.
- Command handlers, reducers, and effects **MUST** be pure functions or simple classes with injected dependencies—no static state, no hidden coupling. Why: Enables isolation.
- The Domain project pattern (see `samples/Spring/Spring.Domain/`) demonstrates the goal: almost all business logic testable without Orleans, Cosmos, HTTP, or SignalR. Why: Reference implementation proves the pattern.
- Framework **SHOULD** provide test harnesses (`AggregateTestHarness`, `StoreScenario`) that let developers test with Given/When/Then semantics. Why: Reduces test boilerplate.
- New framework primitives that developers extend **MUST** include testability as a first-class design constraint; if developer code is hard to test, redesign the primitive. Why: Testability of developer code is non-negotiable.
- **Framework infrastructure** (developer tools, diagnostics, configuration toggles) has different constraints—users enable/disable features rather than write code on top of them. Standard testing practices apply but the "testable without infra" rule is less critical. Why: These are framework internals, not developer extension points.

### Pattern Consistency

- Framework APIs **MUST** expose the same patterns developers use: actions/reducers/state/effects for client; commands/handlers/events/reducers for server. Why: Developers learn one model.
- Internal framework code **SHOULD** follow these patterns when practical. Why: Consistency aids maintenance.
- Framework code **MAY** deviate when infrastructure requirements demand it (e.g., low-level Orleans integration, source generators, storage providers). Why: Framework builds the patterns; it can't always use them.
- Deviations **MUST** be documented with justification in code comments. Why: Future maintainers need context.

### Extension Points

- Framework **MUST** provide base classes and interfaces that guide developers toward correct patterns (`CommandHandlerBase`, `EventReducerBase`, `ActionEffectBase`, `StoreComponent`). Why: Pit of success.
- Base classes **SHOULD** enforce invariants at runtime (e.g., reducer immutability check, handler return type validation). Why: Catches pattern violations early.
- Framework **SHOULD** expose hooks for customization without requiring pattern abandonment. Why: Edge cases shouldn't force developers out of the architecture.

### Source Generators

- Generators **MUST** produce code that follows the patterns developers would write manually. Why: Generated code is reference implementation.
- Generator output **SHOULD** be readable and match hand-written style. Why: Developers debug generated code.
- `[PendingSourceGenerator]` marks hand-written code awaiting generation; these are reference patterns for generator validation. Why: Tracks generation backlog.

### Abstractions

- Public framework APIs **MUST** live in `*.Abstractions` projects when they define contracts. Why: Lightweight consumer packages.
- Implementation details **MUST** stay in main projects; abstractions **MUST NOT** depend on implementations. Why: Clean layering.
- Framework **SHOULD** use the same DI patterns it prescribes (`private Type Name { get; }`, no service locator). Why: Dogfooding.

### Client State (Reservoir)

- `IStore`, `IAction`, `IFeatureState`, `IActionReducer`, `IActionEffect` define the client pattern. Why: Core contracts.
- Framework **MAY** use internal state management for its own concerns (e.g., Inlet connection state) but **SHOULD** expose it through the pattern when beneficial. Why: Balance practicality with consistency.
- Built-in features (Navigation, Lifecycle) **MUST** follow the actions/reducers pattern. Why: Reference implementations.

### Server State (Event Sourcing)

- `ICommandHandler`, `IEventReducer`, `IEventEffect` define the server pattern. Why: Core contracts.
- `IRootCommandHandler`, `IRootReducer` compose handlers/reducers for dispatch. Why: Single entry point.
- Framework grains **SHOULD** follow aggregate patterns where applicable. Why: Consistency.
- Infrastructure grains (worker grains, coordination grains) **MAY** have custom patterns when orchestrating aggregates. Why: Infrastructure enables patterns.

### Flexibility for Edge Cases

- When a pattern cannot support a requirement, framework code **MAY** implement custom solutions. Why: Framework must be complete.
- Custom solutions **SHOULD** still expose familiar interfaces to developers. Why: Developer-facing API consistency matters more than internal consistency.
- New patterns added to handle edge cases **SHOULD** be generalized if reusable. Why: One-offs become patterns if needed twice.

## Scope and Audience

Contributors building or extending the Mississippi framework under `src/`. This is about building the infrastructure that enables the strict patterns enforced in samples.

## At-a-Glance Quick-Start

### Pattern Hierarchy

```text
Framework exposes patterns → Samples follow patterns strictly
Framework MAY deviate internally → But MUST NOT leak deviations to developers
```

### When to Follow vs Deviate

| Scenario | Guidance |
|----------|----------|
| New feature for developers | MUST follow patterns |
| Internal plumbing | SHOULD follow, MAY deviate with justification |
| Source generators | MUST produce pattern-compliant code |
| Orleans integration | MAY use Orleans patterns directly |
| Storage providers | MAY use provider-specific patterns |

### Framework Contracts

```text
Client: IStore, IAction, IFeatureState, IActionReducer, IActionEffect
Server: ICommandHandler, IEventReducer, IEventEffect, IAggregate, IProjection
Generators: [GenerateCommand], [GenerateAggregateEndpoints], [GenerateProjectionEndpoints], etc.
```

Review `src/Inlet.Generators.Abstractions/` for the latest generation attributes.

## Core Principles

- **Developer experience first**: minimize cognitive overload
- **Same primitives everywhere**: aggregates, projections, commands, events, actions, reducers, effects
- **Testability by design**: business logic testable without infrastructure (see `Spring.Domain`)
- Base classes enforce correct usage
- Generated code matches hand-written style
- Internal flexibility serves external consistency; document deviations

## References

- Coding discipline (samples): `.github/instructions/coding-discipline.instructions.md`
- Abstractions: `.github/instructions/abstractions-projects.instructions.md`
- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
