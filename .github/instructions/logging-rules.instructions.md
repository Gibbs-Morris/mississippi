---
applyTo: '**/*.cs'
---

# Logging (LoggerExtensions) Standard

Governing thought: All logging goes through `[LoggerMessage]` LoggerExtensions for zero-allocation performance and consistent observability.

> Drift check: Open referenced LoggerExtensions files or logging configs before editing; scripts/configs remain authoritative for levels and providers.

## Rules (RFC 2119)

- All logs **MUST** use LoggerExtensions classes suffixed `LoggerExtensions` with `[LoggerMessage]` partial methods; direct `ILogger.Log*` calls **MUST NOT** be introduced. Why: Ensures high-performance, consistent logging.
- Injected `ILogger<T>` **MUST** follow the get-only property pattern. Why: Aligns with DI guardrails and analyzers.
- Public service methods **MUST** log entry and successful completion; every `catch` block **MUST** log exceptions with context. Why: Provides traceability.
- Data mutations, external service calls, and batch operations **MUST** be logged with identifiers/counts; operations exceeding ~1s or significant allocations (>10MB) **MUST** capture timing/size. Why: Supports audit and performance analysis.
- Orleans grains **MUST** log activation/deactivation and public grain method calls with timing; business rule violations and event append/read operations **MUST** be logged. Why: Observability for Orleans and event sourcing.
- Log messages **MUST** be structured and descriptive enough for AI debugging; sensitive data (PII/secrets) **MUST NOT** be logged. Why: Keeps logs useful and safe.
- Logs **SHOULD** include correlation IDs and relevant method parameters (masked when sensitive). Why: Enables end-to-end tracing.
- When direct `ILogger` usage is discovered, agents **MUST** create a `.scratchpad/tasks` item to convert it to LoggerExtensions. Why: Tracks remediation work.

## Scope and Audience

All C# contributors emitting logs (services, grains, libraries).

## At-a-Glance Quick-Start

- Create `public static partial class {Component}LoggerExtensions` with `[LoggerMessage]` methods.
- Inject `ILogger<T>` as `private ILogger<T> Logger { get; }`; call `Logger.{Extension}(...);`.
- Log: entry/exit, exceptions, CRUD, external calls, batches, long/expensive ops, Orleans lifecycle/methods, business rule failures, event sourcing operations.
- Add correlation IDs; never log secrets; keep messages short and structured.

## Core Principles

- LoggerExtensions + source generators are the only logging entry point.
- Logs capture intent, identifiers, and timing without leaking sensitive data.
- Orleans/event sourcing require lifecycle + operation logs for diagnostics.

## References

- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
- Orleans context: `.github/instructions/orleans.instructions.md`
