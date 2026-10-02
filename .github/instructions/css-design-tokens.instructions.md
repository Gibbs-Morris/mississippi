---
applyTo: '**/*.css,**/*.scss,**/*.sass,**/*.less,**/*.razor*,src/Refraction.Client/**,src/Refraction.Abstractions/**/Theme/**,**/Themes/**/*.json,**/*.tokens.json'
---

# CSS and Design Token Authoring

Governing thought: Load only the styling contracts needed for the task.

> Drift check: The linked reference and Refraction source are authoritative.

## Rules (RFC 2119)

- Styling authors **MUST** use the applicable [styling rules](refraction-css-and-tokens.reference.md#rules-rfc-2119). Why: Preserves ownership and accessibility.
- Token and generator authors **MUST** use the [input and output contracts](refraction-css-and-tokens.reference.md#initial-dtcg-input-profile). Why: Keeps generation deterministic.

## Scope and Audience

CSS, Razor styling, themes, token catalogs, and new Refraction components.
