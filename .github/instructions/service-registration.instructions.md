---
applyTo: '**/*.cs'
---

# Service Registration and Configuration Pattern

Governing thought: Register features through hierarchical extension methods that stay synchronous, option-driven, and minimal.

## Rules (RFC 2119)

- Structure: each feature **MUST** expose `public static class ServiceRegistration` with `Add{Feature}()` entry points; parent Add methods **MUST** call child registrations to build the hierarchy. Sub-feature registrations **MUST** be `internal`; public overloads only at product/feature boundaries. Methods **MUST** follow the Add{Feature} naming convention and include XML docs.
- Options: constructors **MUST NOT** take raw configuration; use `IOptions<T>`/`IOptionsSnapshot<T>`/`IOptionsMonitor<T>`. Provide overloads for `Action<TOptions>`, `IConfiguration`, and explicit parameters when needed. Options classes **MUST** be named `{Feature}Options`, include sensible defaults, and be validated (e.g., `ValidateOnStart` or `IValidateOptions`). Connection strings **MUST** be accepted and clients created via factory patterns.
- Implementation: shared core registration **MUST** be a private helper; registration **MUST** remain synchronous (no async work). Async setup **MUST** run in `IHostedService` or Orleans lifecycle participants/factories. Injected services **MUST** use get-only properties.
- Behavior: registration **SHOULD** be sealed and minimal, align with feature namespaces, and externalize configuration per cloud-native practices. External initialization for databases/infra **MUST** avoid blocking container build.

## Scope and Audience

Applies to all C# service registration code (ASP.NET, Orleans, background services, libraries).

## Quick Start

- Place `ServiceRegistration` beside the feature root and call child Add* methods to compose the stack.
- Keep registration synchronous; move async initialization to hosted services or lifecycle participants.
- Provide options defaults, validation, and overloads for configuration binding or delegates.
- Register clients via factories using provided connection strings.

## Review Checklist

- [ ] Feature/root exposes Add{Feature} public API; sub-features are internal.
- [ ] No raw configuration in constructors; options defaults and validation are present.
- [ ] Core registration is factored; no async work occurs during registration.
- [ ] DI uses the property pattern; options/clients are registered via factories.
- [ ] Public Add* methods carry XML docs and align with feature namespaces.
