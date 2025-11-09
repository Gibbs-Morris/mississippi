---
applyTo: "**/*.razor*"
---

# Blazor UX Standards

## Scope
Razor components, atomic design, accessibility. WASM-ready patterns.

## Quick-Start
Atoms (button, input) → Molecules (labeled input) → Organisms (feature) → Templates (layout) → Pages (routed). Split `.razor` (markup) and `.razor.cs` (logic).

## Core Principles
Atomic layers: presentational only, no business logic. Redux-style state (Fluxor). Properties for DI. EventCallback for events. WCAG 2.2 AA required. BEM CSS with isolation.

## Anti-Patterns
❌ Business logic in components. ❌ Direct API calls. ❌ Server-only dependencies. ❌ Missing ARIA attributes.

## Enforcement
Code reviews: atomic layering, state patterns, accessibility verified, no server-only deps.
