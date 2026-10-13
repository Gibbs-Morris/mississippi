---
applyTo: '**/*.cs'
---

# Service Registration Pattern

Governing thought: Use hierarchical `ServiceRegistration` extension methods with options-based overloads for features with bindable options and synchronous registration; defer async work to hosted services or Orleans lifecycle participants.

> Drift check: Review DI settings in `Directory.Build.props` and any referenced scripts/config before editing registration code.

## Rules (RFC 2119)

- Feature registration extensions **MUST** use `{Feature}Registrations` classes and hierarchical `Add{Feature}()` methods at supported feature boundaries. Why: Keeps DI discoverable alongside the shipped RuntimeBuilder and ClientBuilder facades.
- Parent registrations **MUST** call child registrations rather than copy their service lists. Why: Preserves hierarchy.
- Sub-feature registrations **MUST** remain `internal` unless an independently supported consumer boundary or generated host-callable helper requires public access. Why: Generated conveniences still need an explicit caller.
- Public registration boundaries **MUST** include XML documentation. Why: Makes the supported composition surface explicit.
- Registration methods **MUST** be synchronous; async calls (DB, HTTP, etc.) **MUST NOT** occur during registration. Why: DI building is synchronous.
- Any async initialization **MUST** be deferred to `IHostedService` or Orleans lifecycle participants; async factories **MUST** be registered for deferred work rather than blocking registration. Why: Avoids startup deadlocks.
- Classes **MUST NOT** inject `IServiceProvider` directly; use explicit dependencies or `Lazy<T>` to break circular dependencies. Factory patterns resolving services at runtime are the only acceptable exception. Why: Service locator hides dependencies, complicates testing, and breaks static analysis.
- Configuration forms, required identity arguments, options validation, and attribute ownership **MUST** follow [metadata and host composition](metadata-and-composition.instructions.md) within its activation scope. Why: Adopted contracts use real configurable options while repository-wide adoption awaits ADR acceptance.
- Connection strings and external clients **MUST** be accepted/configured via factories; package version entries **MUST NOT** be added in project files. Why: Keeps registration testable and CPM-compliant.
- Registered services **MUST** use the DI get-only property pattern. Why: Aligns with shared guardrails and logging patterns.
- Registration classes **SHOULD** be static and minimal. Why: Keeps the extension-method surface focused.

## Scope and Audience

Developers adding or modifying DI registration in Mississippi/Samples, including Orleans integrations.

## At-a-Glance Quick-Start

- Shape registrations as `services.Add{Feature}()` in `{Feature}Registrations.cs` under the feature namespace (e.g., `AggregateRegistrations.cs`, `InletSiloRegistrations.cs`).
- Keep registration sync-only; move setup to `IHostedService` or Orleans lifecycle participants.
- For work that adopts the proposed composition guide, expose configuration forms for real options, keep required identity arguments explicit, and validate options on start.
- Call child registrations instead of duplicating service lists.

## Core Principles

- Hierarchical DI keeps features composable.
- Options + validation catch misconfig early.
- Async work belongs in hosted services/lifecycle hooks, not registration.
- Internal-by-default access reduces public API churn.

## Domain Registration Patterns (Event Sourcing)

For domain models using Mississippi event sourcing:

| Method Pattern | Purpose | Example |
|----------------|---------|---------|
| `Add{Domain}Domain()` | Public entry point | `AddContosoDomain()` |
| `Add{Aggregate}Aggregate()` | Private per-aggregate | `AddChannelAggregate()` |
| `Add{Projection}Projection()` | Private per-projection | `AddUserProfileProjection()` |

Registration order within aggregates/projections:

1. `AddEventType<TEvent>()` - Register event types
2. `AddCommandHandler<TCommand, TAggregate, THandler>()` - Register handlers
3. `AddReducer<TEvent, TState, TReducer>()` - Register reducers
4. `AddSnapshotStateConverter<TState>()` - Register snapshot converter

## References

- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
- Orleans lifecycle: `.github/instructions/orleans.instructions.md`
- Domain modeling: `.github/instructions/domain-modeling.instructions.md`
- Keyed services for storage: `.github/instructions/keyed-services.instructions.md`
