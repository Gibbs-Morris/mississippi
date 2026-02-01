# RFC: Blazor Native UI Design System and Component Library

## Problem

The repository lacks a cohesive, enterprise-grade Blazor-native design system and component library aligned to the authoritative hologram design language brief.

## Goals

- Deliver a document-first design system with tokens, theming, and atomic-design-based component inventory.
- Implement a Blazor-native component library with strong DX and enterprise data-app capabilities.
- Integrate Chart.js behind Blazor-first components without leaking JS types.
- Provide built-in themes that express the hologram aesthetic.

## Non-goals

- Replacing existing applications or rewriting unrelated UI code.
- Introducing JS-first or consumer-authored JS dependencies.

## Current state

- UNVERIFIED: No existing design system under docs/Docusaurus/docs/refraction.
- UNVERIFIED: Blazor UI components exist in src/Refraction or other projects but are not standardized.

## Proposed design (initial)

- Author docs first under docs/Docusaurus/docs/refraction.
- Define tokens (Opacity, Stroke, Glow, Depth, Motion) and theme variants (Neon Blue, Water Blue).
- Implement core components and signature hologram components aligned to the design brief.
- Encapsulate Chart.js via .NET configuration models.

## Alternatives

- Adopt a third-party UI library: rejected due to hologram aesthetic and Blazor-native constraints.

## Security

- No new auth or secrets handling in this scope.

## Observability

- Ensure components provide accessible states and deterministic behaviors; add structured logging only if required by existing patterns.

## Compatibility and migrations

- Avoid breaking public APIs where possible; introduce new components in a parallel namespace if required.

## Risks

- Scope creep in component inventory.
- Incomplete mobile adaptations for data-dense controls.
