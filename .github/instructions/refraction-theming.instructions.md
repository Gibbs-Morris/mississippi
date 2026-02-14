---
applyTo: '**/Refraction/**/*.css,**/Refraction/**/*.razor.css'
---

# Refraction CSS Tokenization

Governing thought: Tokens are semantic attributes (colors, spacing, radii), never control-specific. Reuse existing tokens; add new ones only for genuinely reusable semantic values.

## Rules (RFC 2119)

- Components **MUST** use existing tokens from `refraction.default-theme.css` before considering new ones. Why: Keeps token set minimal and coherent.
- New tokens **MUST** represent semantic attributes (`--mis-bg-brand`, `--mis-radius-md`), **MUST NOT** be control-specific (`--mis-button-bg`). Why: Tokens enable theming across all components; control-specific values belong in component CSS.
- Token additions **MUST** be justified by reuse across 2+ components or clear semantic meaning (error, warning, success states). Why: Prevents token bloat.
- All `var()` calls **MUST** include a fallback value: `var(--mis-bg-brand, #0066cc)`. Why: Components work even without a theme loaded.
- Token names **MUST** follow `--mis-{category}-{semantic}[-{variant}]` convention. Why: Predictable naming aids discoverability.
- Component CSS **SHOULD** use hardcoded values for one-off styling not intended for theming. Why: Not everything needs a token.

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

- Default theme: `src/Refraction/wwwroot/refraction.default-theme.css`
- Sample override: `samples/LightSpeed/LightSpeed.Client/wwwroot/refraction.sample-theme.css`
- Theming guide: `samples/LightSpeed/README.md` (Refraction Theming section)
