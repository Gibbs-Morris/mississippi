# Neon-Aqua Theme Documentation

This document describes the dark, retro-futuristic "digital ocean" theme applied to the Mississippi documentation site.

## Overview

The theme features:

- **Deep navy/near-black backgrounds** for a dark, immersive reading experience
- **Cyan/aqua primary accents** for a neon, high-tech aesthetic
- **Subtle glow effects** on links, buttons, and code blocks
- **Glass-like surfaces** with backdrop blur effects
- **Optional grid and shimmer overlays** for a "digital ocean" atmosphere
- **Fira Sans** for UI text and **Fira Mono** for code

## Files Changed

| File | Changes |
|------|---------|
| `docusaurus.config.ts` | Added Google Fonts (headTags, stylesheets), set dark mode as default |
| `src/css/custom.css` | Complete theme with Infima variable overrides and visual effects |
| `THEME_NOTES.md` | This documentation file |

## Key Tokens to Customize

### Primary Color Palette

Located in `src/css/custom.css` under `:root` and `html[data-theme='dark']`:

```css
--ifm-color-primary: #00e5ff;          /* Main accent color */
--ifm-color-primary-dark: #00cfe6;     /* Slightly darker */
--ifm-color-primary-darker: #00c3d9;   /* More dark */
--ifm-color-primary-darkest: #00a0b3;  /* Darkest variant */
--ifm-color-primary-light: #1aebff;    /* Lighter variant */
--ifm-color-primary-lighter: #33eeff;  /* More light */
--ifm-color-primary-lightest: #66f3ff; /* Lightest variant */
```

### Background Colors

```css
--ifm-background-color: #050a14;         /* Main page background */
--ifm-background-surface-color: #071b2b; /* Cards, panels, surfaces */
```

### Text Colors

```css
--ifm-font-color-base: #e0f7fa;      /* Primary text */
--ifm-font-color-secondary: #b2ebf2; /* Secondary/muted text */
```

## Adjusting Effects

### Grid Overlay Opacity

The grid overlay is controlled by `html[data-theme='dark'] body::before`. To adjust intensity:

```css
html[data-theme='dark'] body::before {
  opacity: 0.03; /* Increase for more visible grid, decrease for subtler effect */
}
```

### Shimmer Overlay Opacity & Speed

The shimmer effect is controlled by `html[data-theme='dark'] body::after`:

```css
html[data-theme='dark'] body::after {
  opacity: 0.02; /* Increase for more visible shimmer */
  animation: shimmerDrift 30s ease-in-out infinite; /* Change 30s for faster/slower drift */
}
```

### Glow Intensity

Link glow (text-shadow):

```css
html[data-theme='dark'] a:not(.button)... {
  text-shadow: 0 0 8px rgba(0, 229, 255, 0.3); /* Adjust last value (0.3) for intensity */
}
```

Button glow (box-shadow):

```css
html[data-theme='dark'] .button--primary {
  box-shadow: 0 0 10px rgba(0, 229, 255, 0.2); /* Adjust spread and alpha */
}
```

## Disabling Effects

### To Disable the Grid Overlay

Comment out or remove the `html[data-theme='dark'] body::before` block in `src/css/custom.css`:

```css
/* html[data-theme='dark'] body::before {
  content: '';
  ...
} */
```

### To Disable the Shimmer Overlay

Comment out or remove the `html[data-theme='dark'] body::after` block in `src/css/custom.css`:

```css
/* html[data-theme='dark'] body::after {
  content: '';
  ...
} */
```

Also remove the `@keyframes shimmerDrift` if you remove the shimmer.

### To Disable All Glow Effects

Remove or comment out the following sections:

- Links: `html[data-theme='dark'] a:not(.button)...` blocks
- Buttons: `html[data-theme='dark'] .button--primary` and `.button--secondary` blocks
- Code blocks: Remove `box-shadow` from `html[data-theme='dark'] pre`

## Accessibility

### Reduced Motion

The shimmer animation automatically disables for users who prefer reduced motion:

```css
@media (prefers-reduced-motion: reduce) {
  html[data-theme='dark'] body::after {
    animation: none;
  }
}
```

### Color Contrast

The theme maintains WCAG AA contrast ratios:

- Primary text (#e0f7fa) on background (#050a14): ~15:1
- Accent color (#00e5ff) on background: ~10:1

### Pointer Events

All overlays have `pointer-events: none` to ensure they don't interfere with user interactions.

## Fonts

Fonts are loaded via Google Fonts in `docusaurus.config.ts`:

- **Fira Sans** (weights: 300, 400, 600, 700) - UI font
- **Fira Mono** (weights: 400, 500) - Code font

To change fonts, update:

1. The `stylesheets` array in `docusaurus.config.ts`
2. The `--ifm-font-family-base` and `--ifm-font-family-monospace` variables in `custom.css`

## Browser Support

- Modern browsers (Chrome, Firefox, Safari, Edge)
- Backdrop blur may not work in older browsers (graceful degradation)
- Custom scrollbar styling only works in WebKit browsers
