---
applyTo: '**'
---

# Abstractions Projects

Governing thought: Split stable public contracts into `{Vendor}.{Area}[.{Feature}].Abstractions` projects so consumers take lightweight interfaces without implementations.

> Drift check: Open any referenced scripts/templates under `eng/src/` before use; scripts remain authoritative.

## Rules (RFC 2119)

- `*.Abstractions` projects **MUST** contain only public contracts (interfaces, abstract bases with documented justification, DTOs, domain exceptions, CQRS requests); no infrastructure/persistence/hosting code, and DI **MUST NOT** embed concrete dependencies. Generic DI helpers that only register the abstraction to a caller-supplied implementation type and add no new package dependencies **MAY** live in abstractions to keep consumers lightweight. Why: Keeps packages slim while enabling opt-in registration.
- Main projects **MUST** own all implementations/infrastructure and reference their abstractions; abstractions **MUST NOT** depend on implementations; downstream consumers **SHOULD** reference abstractions unless implementation is required. Why: Preserves clean layering.
- When all mandatory triggers apply (cross-assembly/service contracts, multiple implementations exist/expected, stable public API), contributors **MUST** create an abstractions project before adding/modifying contracts. Why: Enforces required separation early.
- When any optional trigger applies (dependency minimization, testing/mocking, cross-team reuse, versioning flexibility), contributors **SHOULD** create an abstractions project unless deliberately documented otherwise. Why: Encourages reuse when valuable.
- Types that describe *what* to do **SHOULD** live in abstractions; types describing *how* **MUST** stay in main project. Why: Keeps public programming model stable while implementations evolve.
- Abstract base classes intended for external inheritance **MUST** end with `Base` and document justification; naming **SHOULD** follow `{Vendor}.{Area}[.{Feature}].Abstractions`. Why: Clarifies intent and discoverability.

## Scope and Audience

Applies whenever creating or updating libraries that expose contracts across assemblies/services.

## At-a-Glance Quick-Start

- If contracts cross assemblies/services and multiple implementations/stable API exist, create `*.Abstractions` first.
- Keep implementations/DI/storage in main project; reference abstractions from main.
- Ensure abstractions have minimal dependencies and stay contracts-only.

## Core Principles

- Contracts stay lightweight and reusable.
- Implementations remain flexible and internal.
- Dependency direction mirrors `Microsoft.Extensions.*` and Orleans packages.

## References

- Naming: `.github/instructions/naming.instructions.md`
