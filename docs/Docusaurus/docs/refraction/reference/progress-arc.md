---
title: ProgressArc
description: Refraction circular progress ranges, accessible states, motion, and customization.
sidebar_position: 4
---

# ProgressArc

`ProgressArc` displays completion as a proportional arc, or unknown-duration
activity as a rotating segment. This sealed presentational atom is in
`Mississippi.Refraction.Client.Components.Atoms.Progress`, supplied by
`Mississippi.Refraction.Client`. It receives state from its parent and starts
no background work.

## Range and state

| Parameter | Default | Contract |
| --- | --- | --- |
| `Min` | `0` | Finite lower bound. |
| `Max` | `100` | Finite upper bound, greater than `Min`; `Max - Min` must also be finite. |
| `Value` | `0` | Finite determinate value, clamped to the range for both rendering and accessibility. The supplied parameter is unchanged. |
| `State` | `RefractionStates.Determinate` | Use `Determinate` for completion or `Indeterminate` for unknown duration. |
| `Class` | `null` | Additional classes alongside the component class. |
| `AdditionalAttributes` | `null` | Native wrapper attributes, including accessible names and inline branding. |

Invalid ranges, unsupported states, and nonfinite determinate values throw
`ArgumentOutOfRangeException` during parameter application. Indeterminate mode
ignores `Value`, including nonfinite values, but still requires a valid range.
Changing parameters updates the geometry and accessible state together.
Zero completion hides the fill; full completion fills the circle.

## Accessible name and motion

Always supply `aria-label` or an `aria-labelledby` reference to existing text.
The wrapper exposes `role="progressbar"`, range bounds, and the clamped
`aria-valuenow`. Indeterminate mode omits `aria-valuenow`. Explicit component
state attributes take precedence over unmatched attributes. The SVG is
decorative and cannot receive focus.

```razor
@using Mississippi.Refraction.Client
@using Mississippi.Refraction.Client.Components.Atoms.Progress

<ProgressArc Value="75" aria-label="Import records"/>
<ProgressArc State="@RefractionStates.Indeterminate" aria-label="Preparing import"/>
```

Unknown-duration rotation stops when the operating system requests reduced
motion or the nearest `CascadingRefractionProvider` has `IsReducedMotion="true"`.
The static segment retains unknown-duration semantics. Supply nearby status
text so meaning does not depend on animation. Progress bars do not announce
every update as a live region.

## Scoped customization

Use the [theme provider](./themes.md) and its semantic color tokens. The fill
uses `--rf-color-action-primary`; the track uses `--rf-color-text-muted`.
Set `--rf-progress-size` (default `36px`) and `--rf-progress-stroke-width`
(default `2` SVG units) on the wrapper or an ancestor. Keep stroke width within
the view box's four-unit margin to avoid clipping.

## Pre-release API change

Update imports from `Mississippi.Refraction.Client.Components.Atoms` to
`Mississippi.Refraction.Client.Components.Atoms.Progress`. Compose this sealed
atom rather than subclassing it. State now accepts only the two documented
progress modes, and invalid numeric inputs fail explicitly.

The [LightSpeed kitchen sink](../getting-started/lightspeed.md)
demonstrates completion choices through Reservoir actions, reducers and selectors.
