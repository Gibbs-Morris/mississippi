---
title: Scoped Refraction Themes
description: Built-in theme modes, scoped token overrides, and reduced-motion context.
sidebar_position: 3
---

# Scoped Refraction Themes

`CascadingRefractionProvider` applies a built-in palette to a component subtree
and supplies the named reduced-motion cascade.
It is a sealed component in
`Mississippi.Refraction.Client.Infrastructure.Theming`.

## Stylesheet contract

Load the packaged token stylesheet once in the application's HTML head:

```html
<link rel="stylesheet" href="_content/Mississippi.Refraction.Client/RefractionTokens.css" />
```

Also load the application's generated scoped stylesheet so the provider and
component styles are included. The token stylesheet defines default values on
`:root` and resets them on each `data-rf-theme` scope. The provider renders a
`div` wrapper with that attribute.

## Parameters

| Parameter | Default | Behavior |
| --- | --- | --- |
| `ThemeMode` | `RefractionThemeMode.Dark` | Selects `Dark`, `Light`, or `HighContrast`. Other enum values throw `ArgumentOutOfRangeException` while rendering. |
| `IsReducedMotion` | `false` | Supplies the Boolean `RefractionReducedMotion` cascade to descendants. |
| `Class` | `null` | Adds wrapper classes alongside `rf-theme`. |
| `AdditionalAttributes` | `null` | Forwards native attributes, including inline token overrides, to the wrapper. Owned class and theme attributes take precedence. |
| `ChildContent` | `null` | Renders within the scope. |

## Modes and scope

Dark uses the Neo Blue palette on dark surfaces. Light uses dark ink and darker
status colors on light surfaces. HighContrast uses white text and bright accents
on black surfaces. Browser forced-colors mode uses system color tokens instead.
These presets do not establish whole-application accessibility conformance.

Each provider starts its own token defaults. A nested provider can choose a
different mode without inheriting resolved palette aliases or branding overrides
from its parent. Ordinary descendants inherit the nearest scope. Sibling scopes
remain independent.

Changing a mode updates the wrapper and inherited tokens; it does not recreate
child components or manage application state. Store the selected mode in the
containing page's state and pass it down.

## Branding hooks

Set CSS custom properties on the provider through its native `style` attribute.
No global selector override is required. Override the semantic tokens needed by
the brand and verify contrast in each used state.

Use normal declarations for branded color tokens. In forced-colors mode,
the system-color mappings take priority over normal inline branding. Typography,
spacing, and other non-color tokens remain customizable.

| Purpose | Token examples |
| --- | --- |
| Surfaces | `--rf-color-surface-void`, `--rf-color-surface-elevated`, `--rf-color-surface-glass` |
| Text | `--rf-color-text-primary`, `--rf-color-text-secondary`, `--rf-color-text-accent` |
| Interaction | `--rf-color-action-primary`, `--rf-color-action-hover`, `--rf-color-focus-ring` |
| Status | `--rf-color-status-error`, `--rf-color-status-warning`, `--rf-color-status-success` |
| Typography | `--rf-raw-font-sans`, `--rf-raw-font-mono`, `--rf-raw-text-base` |
| Spacing and focus | `--rf-raw-space-1` through the supplied spacing scale, `--rf-focus-ring-width`, `--rf-focus-ring-offset` |

Default text sizes are 0.75rem, 0.875rem, 1rem, 1.125rem, and 1.25rem for
`xs`, `sm`, `base`, `lg`, and `xl`. The larger defaults improve legibility
compared with the earlier 0.625rem label scale.

The motion parameter propagates intent; it does not observe operating-system
preferences or force arbitrary child content to stop animating. Hosts supply the
preference, and consuming components remain responsible for honoring it.

## Pre-release migration

The provider moved from `Mississippi.Refraction.Client.Infrastructure` to
`Mississippi.Refraction.Client.Infrastructure.Theming`; update C# and Razor
imports. The Boolean parameter is now `IsReducedMotion` rather than
`ReducedMotion`. The named cascade remains `RefractionReducedMotion`.
The new wrapper can affect direct-child layout selectors.

## Related reference

- [InputField](./input-field.md)
- [Refraction reference](./reference.md)
- [Refraction and Reservoir boundaries](../how-to/how-to.md)
