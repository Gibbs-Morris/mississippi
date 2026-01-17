---
applyTo: '**'
---

# Shared Engineering Guardrails

Governing thought: Common rules for every instruction—zero warnings, centralized packages, consistent dependency injection, and LoggerExtensions logging—so other docs can stay concise.

> Drift check: When a rule references a repository script or config, open the referenced file under `eng/src/` or the repo root first; scripts/configs stay authoritative.

## Rules (RFC 2119)

- **Zero Warnings Everywhere** - All projects and tests **MUST** build with zero compiler/analyzer warnings; contributors **MUST NOT** relax rule severity, add project-wide `NoWarn`, or use `#pragma`/`[SuppressMessage]` without explicit approval. Why: Keeps quality gates deterministic.
- **Central Package Management** - Package versions **MUST** live in `Directory.Packages.props`; `PackageReference` items **MUST NOT** declare `Version` and package changes **MUST** use `dotnet add/remove package`. Why: Prevents drift and NU10xx noise.
- **DI Property Pattern** - Injected dependencies **MUST** use `private Type Name { get; }` (no underscored fields); constructors **SHOULD** be the only injection point. Why: Matches analyzers, logging, and testability patterns.
- **No Service Locator** - Classes **MUST NOT** inject `IServiceProvider` directly; use explicit dependencies or `Lazy<T>` to break circular dependencies. Factory patterns resolving services at runtime are the only acceptable exception. Why: Service locator hides dependencies, complicates testing, and breaks static analysis.
- **LoggerExtensions Entry Point** - Logging **MUST** go through LoggerExtensions using `[LoggerMessage]`; direct `ILogger.Log*` calls **MUST NOT** be introduced. Why: Enforces the high-performance logging standard across the repo.
- **No File-Level Copyright Banners** - Copyright or license headers/banners **MUST NOT** appear at the top of source, script, or markup files; repository-level licensing already applies. Why: Avoids noisy/stale headers and keeps diffs focused on behavior.
- **Canonical Solutions Are .slnx** - `.slnx` files are the source of truth; `.sln` files **MUST NOT** be hand-edited because CI/automation regenerates them via SlnGen for legacy tooling (ReSharper, Stryker). Why: Prevents drift between generated and canonical solutions.

## Scope and Audience

Applies to all contributors and all files in this repository; individual instruction files add domain-specific rules and should reference this file instead of duplicating these guardrails.

## At-a-Glance Quick-Start

- Build/test with zero warnings; never add `NoWarn` or file-wide pragmas.
- Add/remove packages with `dotnet add/remove package`; keep versions only in `Directory.Packages.props`.
- Keep injected services as get-only properties; avoid underscored fields.
- Never inject `IServiceProvider`; use explicit dependencies or `Lazy<T>` for deferred resolution.
- Use LoggerExtensions source-generator methods for every log statement.

## Core Principles and Rationale

- Shared guardrails reduce duplication across instructions.
- Centralized packages and DI/logging patterns keep the codebase analyzable and testable.
- Zero warnings keeps CI deterministic and aligns with build/test gates.

## References

- `Directory.Build.props`, `Directory.Packages.props` for repo-wide MSBuild settings.
- `.github/instructions/logging-rules.instructions.md` for logging specifics.
