---
id: reticle
title: Reticle
description: Reference Reticle mode and state inputs, rendered ring markup, and application-owned focus behavior.
sidebar_position: 9
sidebar_label: Reticle
---

# Reticle

## Overview

`Reticle` renders a visual ring with mode and state markers. The parent application decides what the ring represents and where it appears.

## Applies To

- `Mississippi.Refraction.Client.Components.Atoms.Reticle`
- `Mississippi.Refraction.Client.RefractionReticleModes`

## Parameters And Modes

The [component parameters](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Refraction.Client/Components/Atoms/Reticle.razor.cs) are:

| Parameter | Default | Behavior |
| --- | --- | --- |
| `Mode` | `RefractionReticleModes.Focus` (`"focus"`) | Value for the root's `data-mode` marker |
| `State` | `RefractionStates.Idle` (`"idle"`) | Value for the root's `data-state` marker |
| `AdditionalAttributes` | `null` | Unmatched attributes forwarded to the root element |

[`RefractionReticleModes`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Refraction.Client/RefractionReticleModes.cs) defines `Focus` (`"focus"`), `Select` (`"select"`), and `Command` (`"command"`). These constants provide names for inputs; they do not invoke browser focus, selection, or command handling.

`Mode` and `State` accept strings without validation. Changing their values changes the rendered markers, rather than starting an interaction routine.

## Rendered Anatomy

The [Razor markup](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Refraction.Client/Components/Atoms/Reticle.razor) contains a root `div` and one empty ring span. That ring renders for every mode and state. The current markup has no center dot, snap ticks, label arc, or child-content slot.

`AdditionalAttributes` is applied last, so a colliding `class`, `data-mode`, or `data-state` overrides the explicit root value under Blazor's [attribute precedence](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/splat-attributes-and-arbitrary-parameters#arbitrary-attributes). A caller class replaces rather than appends to `rf-reticle`; retain that base class when adding classes and avoid unintended marker collisions.

The [isolated stylesheet](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Refraction.Client/Components/Atoms/Reticle.razor.css) supplies the ring's basic appearance. It does not define mode or state variants, so setting a different marker alone does not select a different component-owned rendering or animation.

## Composition Boundary

`Reticle` has no event callbacks, keyboard handling, automatic target placement, or DOM focus operation. The application owns actual focus and selection state and passes an appropriate visual marker into this component.

Decide whether the ring is decorative or conveys information beyond the underlying focused or selected control. Pass native attributes through `AdditionalAttributes` to express that decision. A `Mode` value alone does not provide an accessible name or a focusable control.

Existing [Reticle tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Refraction.Client.L0Tests/Components/Atoms/ReticleTests.cs) cover default mode/state, a custom command mode, ring rendering, and forwarded attributes. They do not assert an alternate state or establish interactive focus or selection behavior.

## Summary

Use `Reticle` as a presentational ring within application-owned interactions. Mode and state are markers; browser focus, selection, placement, and accessibility meaning remain application responsibilities.

## Next Steps

- Read [Refraction Reference](./reference.md) for the component and package boundaries.
- Read [Scoped Refraction Themes](./themes.md) for shared appearance configuration.
- Read [Refraction Concepts](../concepts/concepts.md) for state ownership and composition.
