---
applyTo: '**/*.razor*'
---

# Blazor UX Guidelines

Governing thought: Build atomic, testable Blazor components with split markup/logic, Redux-style state, accessibility by default, and WebAssembly-friendly dependencies.

> Drift check: Check DI/logging/test settings and design system assets before editing components; repository scripts/configs stay authoritative.

## Rules (RFC 2119)

- Agents **MUST** follow this guide when authoring/reviewing Razor components. Why: Keeps UX consistent.
- Components **MUST** mirror atomic layers (Atoms/Molecules/Organisms/Templates/Pages) with one component per folder containing `.razor`, `.razor.cs`, styles, and tests. Why: Predictable structure.
- Markup and logic **MUST** be split (`.razor` + `partial` `.razor.cs`, `sealed` unless extensibility is required). Why: Focused diffs and testability.
- View-only components **MUST** stay presentational, exposing `[Parameter]` + `EventCallback`; child components **MUST NOT** call APIs or manage side effects; domain logic **MUST** live outside the UI. Why: Separation of concerns.
- Redux-style state (actions/reducers/selectors/effects) **SHOULD** be used; selectors **MUST** feed components instead of raw store state; effects **MUST** call interfaces for IO. Why: Predictable updates and testability.
- Templates **MUST NOT** fetch data; injection inside Razor markup **MUST NOT** be used (inject in partial class); server-only dependencies **MUST NOT** appear in shared components to keep WASM compatibility. Why: Portability and clarity.
- `[Parameter]` members **MUST** be PascalCase; atoms **MUST NOT** rely on global styles; organisms **MUST NOT** access data stores directly. Why: Consistent APIs and layering.
- Interactive atoms **MUST** be keyboard accessible with required ARIA metadata; components **MUST** include L0 tests for state transitions/callbacks. Why: Accessibility and regression safety.
- Atoms **SHOULD** forward `AdditionalAttributes`; duplicated markup **SHOULD** be refactored into slots/parameters; pages **SHOULD** implement `IAsyncDisposable` when holding resources. Why: Reuse and cleanup.
- Global overrides **SHOULD NOT** be required for theming; missing accessibility audits **SHOULD** be tracked. Why: Portable styling and visibility of gaps.

## Scope and Audience

Developers authoring or reviewing Blazor components/pages.

## At-a-Glance Quick-Start

- Place components under a single root with atomic folders; one component per folder.
- Keep logic in `.razor.cs` partial class; inject services there, not in markup.
- Use Redux-style state + selectors; send intent via callbacks, not direct API calls.
- Ensure accessibility (keyboard/ARIA), isolated styles, and L0 tests.

## Core Principles

- Atomic design supports reuse and consistent composition.
- Separation of markup/logic/state keeps components portable and testable.
- Accessibility and WASM readiness are defaults, not afterthoughts.

## References

- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
- Testing: `.github/instructions/testing.instructions.md`
