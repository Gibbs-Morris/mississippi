---
applyTo: 'src/Refraction.Theme.Enterprise/**'
---

# Refraction Theme: Enterprise

Governing thought: Enterprise is a neutral, trustworthy foundation — predictable patterns, clear signifiers, strong accessibility, and safe customization points that preserve coherence.

## Theme Statement

A neutral, trustworthy foundation theme optimized for predictable enterprise workflows—clean, conventional, and highly adaptable to brand guidelines without fragmenting the product.

## Rules (RFC 2119)

- Interaction patterns **MUST** prioritize convention and consistency. Why: Conformity with user expectations.
- Controls **MUST** communicate what they are and what will happen — no hidden affordances. Why: Self-descriptive interfaces.
- Simplification **MUST NOT** remove signifiers; clickable vs non-clickable **MUST** be obvious. Why: Minimal, not ambiguous.
- Contrast and non-color cues **MUST** be foundational, not optional. Why: Accessibility by design.
- Feedback **MUST** be specific and constructive; mistakes **MUST** be recoverable. Why: Error robustness.
- Motion **MUST** be subtle and utilitarian — preserve context, confirm outcomes. Why: No competition with data work.
- Motion **MUST** respect `prefers-reduced-motion`. Why: Accessibility requirement.
- Surfaces **SHOULD** be solid/opaque for consistent legibility; translucency (if any) **MUST** be protected by contrast rules. Why: Reading reliability.

## Experience Promise

- **Predictability**: users rely on established conventions and consistent behaviors
- **Clarity**: hierarchy via typography, spacing, contrast — not special effects
- **Brand-fit**: identity through controlled knobs (color, type, shape, density), not bespoke rework
- **Operational confidence**: states, validation, feedback are explicit, actionable, calm

## Behavioral Feel

- **Motion**: subtle, utilitarian; preserves context; confirms outcomes
- **Surfaces**: primarily solid/opaque; rare translucency always contrast-protected
- **Density**: comfortable readability default; compact variants for expert/data-heavy work

## Scaling (Phone → 8K)

| Size | Approach |
|------|----------|
| Small (touch) | Single primary action per view; clear hierarchy; frictionless navigation |
| Medium (desktop) | Data-heavy workflows; stable scanning patterns; consistent spacing rhythm |
| Large (8K) | Max line lengths; multi-column composition; deliberate density scaling |

## Non-Goals

- Not a fashion statement — avoid stylings that age quickly or reduce affordance clarity
- Not one-off brand skins — customization stays within guardrails

## Success Criteria

- Fits enterprise brand constraints with minimal friction
- Maintains predictability, consistency, recoverability across all components
- Meets baseline accessibility for contrast and non-color communication
- Works from mobile to 8K without diverging into separate products

## References

- Theme brief: `docs/Docusaurus/docs/refraction-themes.md`
- Token contract: `src/Refraction/wwwroot/refraction.tokens.css`
- Theming architecture: `.github/instructions/refraction-theming.instructions.md`
