---
applyTo: '**/*.cs'
---

# Service Registration Pattern

Governing thought: Use hierarchical `ServiceRegistration` extension methods with options-based overloads and synchronous registration; defer async work to hosted services or Orleans lifecycle participants.

> Drift check: Review DI settings in `Directory.Build.props` and any referenced scripts/config before editing registration code.

## Rules (RFC 2119)

- Each feature **MUST** expose `public static class {Feature}Registrations` with `Add{Feature}()` extension methods that follow the feature namespace structure (e.g., `AggregateRegistrations`, `ReducerRegistrations`, `InletSiloRegistrations`). Why: Keeps DI discoverable and consistent.
- Parent registrations **MUST** call child registrations; sub-feature registrations **MUST** remain `internal`; public registration **MUST** exist only at product/feature boundaries and include XML docs. Why: Preserves hierarchy and minimizes public surface.
- Registration methods **MUST** be synchronous; async calls (DB, HTTP, etc.) **MUST NOT** occur during registration. Why: DI building is synchronous.
- Any async initialization **MUST** be deferred to `IHostedService` or Orleans lifecycle participants; async factories **MUST** be registered for deferred work rather than blocking registration. Why: Avoids startup deadlocks.
- Classes **MUST NOT** inject `IServiceProvider` directly; use explicit dependencies or `Lazy<T>` to break circular dependencies. Factory patterns resolving services at runtime are the only acceptable exception. Why: Service locator hides dependencies, complicates testing, and breaks static analysis.
- Options pattern: constructors **MUST NOT** take raw config primitives; registration **MUST** offer overloads for `Action<TOptions>`, `IConfiguration`, and explicit parameters, and options classes **MUST** be named `{Feature}Options` with sensible defaults and validation (`ValidateOnStart`/`IValidateOptions`). Why: Supports predictable configuration.
- Connection strings and external clients **MUST** be accepted/configured via factories; package version entries **MUST NOT** be added in project files. Why: Keeps registration testable and CPM-compliant.
- Registered services **MUST** use the DI get-only property pattern. Why: Aligns with shared guardrails and logging patterns.
- Registration classes **SHOULD** be sealed/minimal and configuration **SHOULD** be externalized per cloud-native principles. Why: Keeps DI surface tight and environment-friendly.

## Scope and Audience

Developers adding or modifying DI registration in Mississippi/Samples, including Orleans integrations.

## Registration workflow

Use [register-dotnet-services](../../.agents/skills/register-dotnet-services/SKILL.md) with the [local source bindings](../agent-guidance/service-registration-bindings.md).
If discovery is unavailable or applicability is unclear, read both files directly; the Rules above remain effective independently of skill activation.

## References

- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
- Orleans lifecycle: `.github/instructions/orleans.instructions.md`
- Domain modeling: `.github/instructions/domain-modeling.instructions.md`
- Keyed services for storage: `.github/instructions/keyed-services.instructions.md`
