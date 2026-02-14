---
applyTo: 'src/Refraction.Theme.Luminous/**'
---

# Refraction Theme: Luminous

Governing thought: Luminous is a cinematic, opinionated theme — blue light defines identity and focus; depth through layering; warm accent reserved for warnings only. Supports light, dark, and high-contrast modes.

## Theme Statement

A luminous, spatial interface built from translucent "window" surfaces and controlled light—designed to feel like operating a high-precision system, with full accessibility support across all modes.

## Rules (RFC 2119)

### Mode Support

- Luminous **MUST** provide light, dark, and high-contrast mode palettes. Why: Accessibility is non-negotiable.
- Light mode **SHOULD** feel bright and airy while maintaining the futuristic aesthetic. Why: Same design language, different palette.
- Dark mode **SHOULD** be the signature Luminous experience (deep, immersive). Why: Original design intent.
- High-contrast mode **MUST** use maximum contrast ratios and **MUST NOT** use glow effects or translucency. Why: Accessibility.

### Visual Language

- Visual effects **MUST** increase clarity; dramatic aesthetics **MUST NOT** reduce legibility. Why: Instrument first, spectacle second.
- Text **MUST** land on surfaces that protect contrast (blur/scrim, simplified backdrops, solidification). Why: Legibility over atmosphere.
- State/interactivity/severity **MUST** use shape/weight/iconography in addition to color. Why: Multiple signifiers for accessibility.
- Warm accent **MUST** be reserved for warning/risk/urgent states only — never decoration. Why: High semantic value.
- Glow effects **SHOULD** be signifiers, not constant aesthetic blankets. Why: Attention is managed deliberately.
- Motion **MUST** preserve continuity and clarify cause→effect; **MUST NOT** exist to entertain. Why: Motion as explanation.
- Non-essential motion **MUST** respect `prefers-reduced-motion`. Why: Adaptive comfort.
- Translucency intensity **SHOULD** adapt to device capability while preserving theme identity. Why: Performance posture.

## Experience Promise

- **Dark as space, not absence**: layered translucency creates depth, operating "within" the UI
- **Blue as operating wavelength**: structure, focus, system energy through coherent blue spectrum
- **Warm accent as meaning**: scarce, serious, unmissable — warning/risk/exception only
- **Motion as explanation**: movement clarifies relationships and outcomes

## Behavioral Feel

- **Motion**: smooth, restrained; transitions preserve orientation (where it came from, where it went)
- **Surfaces**: translucent layers intensify on focus/interaction; reading-heavy content uses protected surfaces
- **Status**: warnings unmistakable, time-bounded, accompanied by non-color cues

## Scaling (Phone → 8K)

| Size | Approach |
|------|----------|
| Small (touch) | Clarity + hit-targets; reduce layers; strengthen text backplates |
| Medium (desktop) | Restore depth cues; keep quiet by default for dense work |
| Large (8K) | Structured spacing; max content widths; hierarchical grouping — instrument panel, not billboard |

## Non-Goals

- Not for reading-heavy, office-bright workflows
- Not a "full-time glow show"

## References

- Theme brief: `docs/Docusaurus/docs/refraction-themes.md`
- Token contract: `src/Refraction/wwwroot/refraction.tokens.css`
- Theming architecture: `.github/instructions/refraction-theming.instructions.md`
