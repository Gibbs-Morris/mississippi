---
applyTo: '**/Refraction/**/*.css,**/Refraction/**/*.razor.css'
---

# Refraction CSS Tokenization

Governing thought: Tokens are semantic attributes (colors, spacing, radii), never control-specific. Every theme supports light, dark, and high-contrast modes for accessibility.

## Rules (RFC 2119)

### Token Usage

- Components **MUST** use existing tokens from the active theme token contract file (for example `refraction.tokens.luminous.css` or `refraction.tokens.enterprise.css`) before considering new ones. Why: Keeps token set minimal and coherent.
- New tokens **MUST** represent semantic attributes (`--mis-bg-brand`, `--mis-radius-md`), **MUST NOT** be control-specific (`--mis-button-bg`). Why: Tokens enable theming across all components; control-specific values belong in component CSS.
- Token additions **MUST** be justified by reuse across 2+ components or clear semantic meaning (error, warning, success states). Why: Prevents token bloat.
- All `var()` calls **MUST** include a fallback value: `var(--mis-bg-brand, #0066cc)`. Why: Components work even without a theme loaded.
- Token names **MUST** follow `--mis-{category}-{semantic}[-{variant}]` convention. Why: Predictable naming aids discoverability.
- Component CSS **SHOULD** use hardcoded values for one-off styling not intended for theming. Why: Not everything needs a token.

### Mode Support (Accessibility)

- All themes **MUST** support three modes: light, dark, and high-contrast. Why: Accessibility is non-negotiable.
- Mode switching **MUST** use `data-theme-mode` attribute on `<html>`: `"light"`, `"dark"`, or `"high-contrast"`. Why: Consistent switching mechanism.
- Themes **MUST** respect OS preferences via `prefers-color-scheme` and `prefers-contrast` as fallback when no explicit mode is set. Why: Honors user system settings.
- High-contrast mode **MUST** use maximum contrast ratios and **SHOULD** disable decorative effects (glows, translucency, soft shadows). Why: Accessibility.
- Mode-specific styles **MUST** be scoped to `[data-theme-mode="..."]` selectors. Why: Enables explicit user control.
- Theme token files **MUST** include media query fallbacks for OS preference detection. Why: Works before user makes explicit choice.

## Mode Switching Architecture

### Data Attribute Pattern

The `data-theme-mode` attribute on `<html>` controls the active mode:

```html
<!-- Explicit user choice (overrides OS preference) -->
<html data-theme-mode="dark">

<!-- No attribute = respect OS preference via media queries -->
<html>
```

### CSS Structure for Theme Token Files

```css
/* ═══════════════════════════════════════════════════════════════
   LIGHT MODE (default)
   ═══════════════════════════════════════════════════════════════ */
:root {
    --mis-bg-default: #ffffff;
    /* ... light palette ... */
}

/* ═══════════════════════════════════════════════════════════════
   DARK MODE
   ═══════════════════════════════════════════════════════════════ */

/* Explicit user preference */
[data-theme-mode="dark"] {
    --mis-bg-default: #0a0a0f;
    /* ... dark palette ... */
}

/* OS preference fallback (only when no explicit choice) */
@media (prefers-color-scheme: dark) {
    :root:not([data-theme-mode]) {
        --mis-bg-default: #0a0a0f;
        /* ... dark palette ... */
    }
}

/* ═══════════════════════════════════════════════════════════════
   HIGH CONTRAST MODE
   ═══════════════════════════════════════════════════════════════ */

/* Explicit user preference */
[data-theme-mode="high-contrast"] {
    --mis-bg-default: #000000;
    --mis-text-default: #ffffff;
    --mis-shadow-sm: none;        /* Disable decorative shadows */
    --mis-shadow-md: none;
    /* ... high contrast palette ... */
}

/* OS preference fallback */
@media (prefers-contrast: more) {
    :root:not([data-theme-mode]) {
        --mis-bg-default: #000000;
        --mis-text-default: #ffffff;
        /* ... high contrast palette ... */
    }
}
```

### Mode-Aware Theme Styles (Optional)

Theme style files may include mode-specific overrides when effects differ per mode:

```css
/* Glow effect in normal modes */
.button:focus-visible {
    box-shadow: var(--mis-shadow-glow, 0 0 20px var(--mis-bg-brand));
}

/* No glow in high contrast — use visible outline instead */
[data-theme-mode="high-contrast"] .button:focus-visible {
    box-shadow: none;
    outline: 3px solid var(--mis-border-focus, #4d9fff);
    outline-offset: 2px;
}
```

## Token Naming Convention

**Structure:** `--mis-{category}-{semantic}[-{variant}]`

### Categories (what the token applies to)

| Category | Purpose | Pattern |
|----------|---------|---------|
| `bg-*` | Background colors | `--mis-bg-{semantic}` |
| `text-*` | Text/foreground colors | `--mis-text-{semantic}` |
| `border-*` | Border colors | `--mis-border-{semantic}` |
| `radius-*` | Corner radii (scale) | `--mis-radius-{size}` |
| `space-*` | Spacing (padding/margin/gap) | `--mis-space-{size}` |
| `size-*` | Control dimensions | `--mis-size-{size}` |
| `font-*` | Typography | `--mis-font-{property}-{value}` |
| `shadow-*` | Elevation/depth | `--mis-shadow-{size}` |
| `duration-*` | Animation timing | `--mis-duration-{speed}` |
| `ease-*` | Animation easing | `--mis-ease-{type}` |
| `z-*` | Z-index layers | `--mis-z-{layer}` |

### Semantic Values (what the token means)

**For colors (`bg-*`, `text-*`, `border-*`):**

| Semantic | Meaning |
|----------|---------|
| `default` | Standard/neutral state |
| `muted` | De-emphasized, secondary |
| `inverse` | Inverted for contrast |
| `brand` | Primary brand color |
| `accent` | Secondary brand highlight |
| `success` | Positive/confirmation |
| `warning` | Caution/attention |
| `error` | Negative/destructive |
| `info` | Informational |
| `focus` | Focus indicator (borders only) |

**For scales (`radius-*`, `space-*`, `size-*`, `shadow-*`):**

| Size | Typical Use |
|------|-------------|
| `none` | Zero/disabled |
| `xs` | Compact UI |
| `sm` | Small controls |
| `md` | Default controls |
| `lg` | Large controls |
| `xl` | Hero sections |
| `full` | Maximum (e.g., pill radius) |

**For animation (`duration-*`, `ease-*`):**

| Duration | Use | Easing | Use |
|----------|-----|--------|-----|
| `instant` | Immediate | `linear` | Constant speed |
| `fast` | Micro-interactions | `in` | Accelerate |
| `normal` | Standard transitions | `out` | Decelerate |
| `slow` | Emphasis | `in-out` | Smooth both |

**For layers (`z-*`):**

| Layer | Use |
|-------|-----|
| `base` | Default content |
| `raised` | Cards, dropdowns |
| `overlay` | Modals, dialogs |
| `toast` | Notifications |

## Complete Token Examples

```css
/* Backgrounds */
--mis-bg-default: #ffffff;
--mis-bg-brand: #0066cc;
--mis-bg-error: #dc2626;

/* Text */
--mis-text-default: #1f2937;
--mis-text-muted: #6b7280;
--mis-text-inverse: #ffffff;
--mis-text-error: #dc2626;

/* Borders */
--mis-border-default: #d1d5db;
--mis-border-focus: #3b82f6;
--mis-border-error: #dc2626;

/* Scales */
--mis-radius-sm: 0.25rem;
--mis-radius-md: 0.375rem;
--mis-size-md: 2.5rem;
--mis-space-md: 1rem;

/* Typography */
--mis-font-size-sm: 0.875rem;
--mis-font-weight-medium: 500;
--mis-font-family-body: system-ui, sans-serif;

/* Animation */
--mis-duration-fast: 150ms;
--mis-ease-out: cubic-bezier(0, 0, 0.2, 1);

/* Elevation */
--mis-shadow-md: 0 4px 6px -1px rgb(0 0 0 / 0.1);
--mis-z-overlay: 100;
```

## When to Add a Token

**Add when:**

- Value represents a **semantic concept** (warning, error, success, disabled)
- Value will be **reused across 2+ components**
- Value should be **theme-overridable** by third parties

**Do NOT add when:**

- Value is specific to one component's internal layout
- Value is a one-off adjustment
- An existing token already serves the purpose

## Example: Correct vs Incorrect

```css
/* ✅ CORRECT: Semantic token with fallback */
.mis-button {
  background: var(--mis-bg-brand, #0066cc);
  border-radius: var(--mis-radius-md, 0.375rem);
  color: var(--mis-text-inverse, #fff);
}

/* ✅ CORRECT: State variant */
.mis-button--error {
  background: var(--mis-bg-error, #dc2626);
}

/* ❌ INCORRECT: Control-specific token */
.mis-button {
  background: var(--mis-button-bg, #0066cc);
}

/* ❌ INCORRECT: Missing fallback */
.mis-button {
  background: var(--mis-bg-brand);
}
```

## References

- Base tokens: `src/Refraction/wwwroot/refraction.tokens.css`
- Luminous theme: `src/Refraction.Theme.Luminous/wwwroot/refraction.tokens.luminous.css`
- Enterprise theme: `src/Refraction.Theme.Enterprise/wwwroot/refraction.tokens.enterprise.css`
- Theme briefs: `docs/Docusaurus/docs/refraction-themes.md`
- Luminous rules: `.github/instructions/refraction-theme-luminous.instructions.md`
- Enterprise rules: `.github/instructions/refraction-theme-enterprise.instructions.md`
