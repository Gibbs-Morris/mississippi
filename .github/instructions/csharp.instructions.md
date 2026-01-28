---
applyTo: '**/*.cs'
---

# C# Development Standards

Governing thought: Write SOLID, testable, cloud-ready C# with internal-by-default access, options/DI patterns, and Orleans-safe practices.

> Drift check: Check `Directory.Build.props`/`Directory.Packages.props` and referenced scripts before editing; they define analyzers, CPM, and defaults.

## Rules (RFC 2119)

- Code **MUST** follow SOLID and remain unit-testable via clear seams/DI; blocking calls and shared mutable state **MUST NOT** be introduced. Why: Keeps code maintainable and testable.
- .NET analyzers **MUST** stay enabled; warnings are errors. Suppressions or `#pragma` **MUST NOT** be added without approval. Why: Zero-warnings is mandatory.
- XML documentation **MUST NOT** include `<example>` blocks with code samples; examples drift over time and become stale/misleading. Readers can refer to real implementation code in the repository instead. Why: Prevents documentation drift and maintenance burden.
- Injected dependencies **MUST** use the get-only property pattern (`private Type Name { get; }`); field injection/underscored fields **MUST NOT** be used. Why: Aligns with logging and analyzers.
- Source files **MUST NOT** include copyright/license headers or banners at the top; repository-level licensing already applies. Why: Avoids noisy/stale headers and keeps diffs focused on behavior.
- Configuration **MUST** use `IOptions<T>`/`IOptionsSnapshot<T>`/`IOptionsMonitor<T>`; constructors **MUST NOT** take raw config primitives. Why: Centralizes config and validation.
- Nested classes **SHOULD NOT** be used except for truly private implementation details; test helpers and public/internal types **MUST** be top-level or in their own files. Why: Nested classes complicate mocking frameworks (NSubstitute/Castle.DynamicProxy) and reduce discoverability.
- Access control: types **MUST** default to `internal`; public/protected/unsealed types **MUST** document justification in XML comments; implementation types **MUST** remain internal unless part of public API. Why: Protects API surface.
- Grain implementations **MUST** implement `IGrainBase`, be `sealed`, and **MUST NOT** inherit from `Grain`; grain interfaces **MUST** be public only when external callers need them. Why: Follows Orleans 7+ POCO guidance.
- Orleans code **MUST NOT** use `Parallel.ForEach` or chatty inter-grain calls; prefer async + `Task.WhenAll`. Why: Preserves Orleans threading model.
- Options/ServiceRegistration types **MAY** be public when part of consumer surface; otherwise **SHOULD** stay internal. Why: Keeps public surface intentional.
- Public contracts **SHOULD** live in `.Abstractions` projects; implementations **MUST** stay in main projects. Why: Supports clean layering.
- Classes **SHOULD** be records/immutable where feasible and only inheritable with clear need; interfaces **SHOULD** be public only when part of deliberate API; members **SHOULD** expose least privilege. Why: Reduces coupling and breaks.
- New third-party dependencies **MAY** be added only with explicit approval or when extending already-adopted tech. Why: Limits supply-chain risk and sprawl.
- All logging **SHOULD** use LoggerExtensions per logging rules. Why: Maintains performance and consistency.

## Scope and Audience

C# contributors across Mississippi and Samples, including Orleans code.

## At-a-Glance Quick-Start

- Default visibility to `internal`; justify any widening in XML docs.
- Use DI property pattern and options pattern; no raw config parameters.
- Avoid blocking/parallel loops in Orleans; use async + `Task.WhenAll`.
- Place public contracts in `.Abstractions`; keep implementations internal.
- Keep analyzers on; fix warnings instead of suppressing.

## Core Principles

- SOLID + DI seams enable testing and refactoring.
- Internal-by-default keeps APIs stable and reduces breaking changes.
- Orleans POCO pattern and async-first avoid threading issues.
- Immutable/value-object bias improves correctness and logging/serialization.

## References

- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
- Naming/docs: `.github/instructions/naming.instructions.md`
- Orleans specifics: `.github/instructions/orleans.instructions.md`
- Service registration: `.github/instructions/service-registration.instructions.md`
- Logging: `.github/instructions/logging-rules.instructions.md`
