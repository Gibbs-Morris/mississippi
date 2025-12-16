---
applyTo: '**/*.razor*'
---

# Blazor UX Design System Guidelines

Governing thought: Build atomic, accessible, WASM-ready components with clear separation of markup, logic, and state.

## Rules (RFC 2119)

- Agents **MUST** follow this guide for Razor work; components **MUST** live under a single atomic root with one component per folder (razor + partial class + styles/tests). Backing partial classes **MUST** be `partial sealed` (unless composition requires) and follow the C# namespace model; `[Parameter]` members **MUST** be PascalCase.
- Markup and logic **MUST** be split (`.razor` + `.razor.cs`); view components **MUST** stay presentational, exposing parameters and `EventCallback`s. Child components **MUST NOT** call APIs or manage side effects; domain logic **MUST** live outside the UI.
- State/effects **SHOULD** use Redux-style stores with selectors feeding components; effects **MUST** call abstractions; components **MUST** render state and raise intent without mutating domain rules. Organisms **MUST NOT** access data directly; server-only dependencies **MUST NOT** appear in shared components; templates **MUST NOT** fetch data; avoid injection inside markup.
- Styling: atoms **MUST NOT** rely on global styles; prefer CSS isolation/BEM and theming via CSS variables. Component duplication **SHOULD** be avoided via parameters/slots. Accessibility: interactive atoms **MUST** be keyboard accessible with ARIA where required; track missing a11y audits for high-traffic components.
- Testing: component logic **MUST** include L0 tests for state transitions and callbacks; log design debt or refactor tasks as needed.

## Quick Start

- Organize folders by atoms/molecules/organisms/templates/pages; use `RenderFragment` slots instead of hardcoded children.
- Keep components WASM-friendly (async APIs, no server-only dependencies); dispose long-lived resources via `IAsyncDisposable` on pages when needed.
- Use selectors and event callbacks; forward unrecognized attributes via `AdditionalAttributes` for atoms.

## Review Checklist

- [ ] Atomic structure with one-folder-per-component; partial sealed backing classes and PascalCase parameters.
- [ ] Markup/logic split; components remain presentational; no API calls/side effects in children or templates.
- [ ] Redux-style state/selectors applied; no server-only dependencies; organisms avoid direct data access.
- [ ] Styles scoped (CSS isolation/BEM/variables); accessibility hooks present.
- [ ] L0 tests cover component logic; design debt tracked when needed.
