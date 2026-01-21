# Holographic Console Design Note — Bank Account Operations Console

## Design Summary

This document describes the visual design, interaction patterns, and responsive behavior for the Spring Sample's Bank Account Operations Console, transformed into a **holographic console interface** (Holographic Console).

---

## Visual Hierarchy

The interface establishes a clear 3-tier visual hierarchy:

### 1. Console Shell (Background Layer)
- **Deep void canvas** (#000d1a → #001122 gradient) simulates empty space
- **Ambient radial gradients** at top and bottom edges suggest distant nebulae
- **Subtle grid overlay** (40px squares, near-invisible) implies sensor calibration marks
- **Vignette effect** draws focus toward center content

### 2. Holographic Panels (Content Layer)
- **Glass-effect containers** with frosted translucency (`backdrop-filter: blur`)
- **Thin cyan borders** with controlled glow auras
- **Corner bracket decorations** (top-left) add "engineered hardware" feel
- **Panel headers** with section icons, titles, and activity indicators
- **Segmented dividers** (dashed lines, tick marks) separate content sections

### 3. Interactive Controls (Foreground Layer)
- **High-contrast buttons** with layered glow states
- **Monospace input fields** with clear focus rings
- **Status readouts** in prominent typography with currency/telemetry styling
- **Alert messages** with icon + color-coded severity

---

## Guiding Aesthetic Principles (Holographic Console)

| Principle | Implementation |
|-----------|---------------|
| **Blue-forward holographic spectrum** | Primary colors: cyan (#00d4ff), azure (#0099cc), electric blue (#0066aa); amber/green reserved for status only |
| **Precision geometry** | 4px border radii, 1px strokes, exact spacing scale (4/8/12/16/24/32px) |
| **Translucent glass panels** | 8-12% opacity backgrounds with backdrop-filter blur |
| **Engineered framing** | Corner brackets, segmented dividers, calibration-tick aesthetics |
| **Controlled luminosity** | Soft glows on focus/hover; no uncontrolled bloom or noise |
| **Professional restraint** | No decoration for decoration's sake; every element serves UX |

---

## Typography System

| Role | Font | Weight | Size | Color |
|------|------|--------|------|-------|
| Page title | Rajdhani | 600 | 2–2.5rem | Cyan with glow |
| Panel headers | Rajdhani | 600 | 1–1.125rem | Cyan |
| Body copy | IBM Plex Sans | 400 | 0.875–1rem | White (#e8f4ff) |
| Form labels | IBM Plex Mono | 400 | 0.625–0.75rem | Azure (uppercase) |
| Telemetry values | IBM Plex Mono | 500 | 1–1.25rem | Cyan (currency) / White |
| Micro-labels | IBM Plex Mono | 400 | 0.625rem | Azure at 60–70% opacity |

All sizes use `clamp()` for fluid scaling across viewport widths.

---

## Responsive Layout Adaptation

### Mobile (< 768px)
- **Single column stack**: Entity Selection → Operations → Status panels flow vertically
- **Full-width controls**: Buttons and inputs span the container
- **Compact padding**: `--space-4` (1rem) margins
- **Touch-friendly targets**: Minimum 44×44px tap zones

### Tablet / Laptop (768px – 1024px)
- **Form rows**: Inputs + buttons align horizontally where space permits
- **Two-column potential**: Panels may share horizontal space
- **Increased padding**: `--space-8` (2rem)

### Desktop (1024px – 1440px)
- **Grid layout**: 2-column arrangement with Operations panel spanning full height
- **Clear zoning**: Command on left, Status on right
- **Dashboard composition**: Panels breathe with generous internal spacing

### Ultrawide / 4K (1440px+)
- **3 or 4 column grid**: Secondary zones (rails) on outer edges
- **Centered max-width**: 2200px container prevents over-stretching
- **Intentional whitespace**: Content doesn't stretch to fill; composition remains focused
- **Increased type scale**: Headings scale up for large display legibility

---

## Motion & Animation Design

### Button Press Feedback
1. **Idle**: Calm outline, glass fill, minimal inner glow
2. **Hover**: Border brightens to cyan; inner glow layer fades in; subtle lift impression
3. **Focus-visible**: 3px cyan ring with 15% opacity fill; strong keyboard visibility
4. **Active/Pressed**: 
   - Transform: `scale(0.98)` for tactile "press" feel
   - Background saturates to higher cyan
   - Radial pulse pseudo-element expands briefly (150ms)
5. **Disabled**: 50% opacity; no glow; cursor not-allowed

All transitions use `150ms cubic-bezier(0.22, 1, 0.36, 1)` for snappy, premium feel.

### State Update Animations (Signal-driven)
When projection data updates in real-time:

1. **Value pulse animation**: Updated values briefly flash cyan with glow (`value-pulse` keyframes, 600ms)
2. **Panel indicator**: Pulsing dot in panel header shows live connection (2s ease-in-out infinite)
3. **Alert entrance**: New alerts fade + slide in from above (`alert-enter` keyframes, 300ms)
4. **Loading spinner**: Minimal border-spin animation (800ms linear) for in-progress operations

### Reduced Motion Support
All animations respect `@media (prefers-reduced-motion: reduce)`:
- Animations set to `duration: 0.01ms` or disabled
- Static alternatives provided (e.g., solid spinner border instead of spinning)
- No essential meaning conveyed only through motion

---

## Accessibility Features

| Feature | Implementation |
|---------|---------------|
| **Keyboard navigation** | All interactive elements reachable via Tab; clear `:focus-visible` rings |
| **ARIA landmarks** | `role="main"`, `aria-labelledby` on sections, `aria-live="polite"` for status updates |
| **Screen reader support** | `.visually-hidden` class for label context; `aria-describedby` for input hints |
| **Color contrast** | Text meets WCAG AA; interactive states maintain 4.5:1 minimum |
| **Reduced motion** | All animations disabled via media query |
| **Semantic HTML** | `<article>`, `<section>`, `<header>`, `<fieldset>`, `<legend>` for structure |

---

## File Structure

```
Spring.Client/
├── Pages/
│   ├── Index.razor          # View markup (semantic HTML, ARIA, CSS hooks)
│   ├── Index.razor.cs       # Code-behind (sealed partial class, logic only)
│   └── Index.razor.css      # Scoped stylesheet (Holographic Console design system)
├── MainLayout.razor          # Shell wrapper with ambient effects
├── MainLayout.razor.css      # Global shell styling
└── wwwroot/
    └── index.html            # Google Fonts, CSS reset, loading state
```

---

## Design Tokens Reference

All design decisions are encoded as CSS custom properties in `Index.razor.css`:

- **Colors**: `--hud-cyan`, `--hud-azure`, `--hud-electric`, etc.
- **Glows**: `--glow-soft`, `--glow-medium`, `--glow-bright`, `--glow-pulse`
- **Typography**: `--font-display`, `--font-body`, `--font-mono`
- **Spacing**: `--space-1` through `--space-16`
- **Timing**: `--duration-instant`, `--duration-fast`, `--duration-normal`, `--duration-slow`
- **Easing**: `--ease-out`, `--ease-in-out`

This enables consistent theming and easy future customization.

---

*Updated @DateTime.UtcNow.ToString("yyyy-MM-dd") — Holographic Console Interface v1.0*
