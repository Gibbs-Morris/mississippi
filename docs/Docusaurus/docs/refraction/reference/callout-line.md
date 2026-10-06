---
id: callout-line
title: CalloutLine
description: Reference the current CalloutLine markup, label and state parameters, and composition boundary.
sidebar_position: 8
sidebar_label: CalloutLine
---

# CalloutLine

## Overview

`CalloutLine` renders a small visual anchor, leader line, and optional text label. Its current implementation supplies presentation; application code owns target placement and interaction.

## Applies To

- `Mississippi.Refraction.Client.Components.Atoms.CalloutLine`
- Blazor composition using `Mississippi.Refraction.Client`

## Parameters

The [component parameters](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Refraction.Client/Components/Atoms/CalloutLine.razor.cs) are:

| Parameter | Default | Behavior |
| --- | --- | --- |
| `Label` | `null` | Text for the optional label span |
| `State` | `RefractionStates.Idle` (`"idle"`) | Value for the root's `data-state` marker |
| `AdditionalAttributes` | `null` | Unmatched attributes forwarded to the root element |

`State` is a string parameter. The component does not validate it against `RefractionStates` or turn state values into events.

## Rendered Anatomy

The [Razor markup](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Refraction.Client/Components/Atoms/CalloutLine.razor) uses a root `div` containing an anchor span and a line span. Both spans render even when the label is absent.

`AdditionalAttributes` is applied after the root's explicit attributes, so collisions override `class` or `data-state` under Blazor's [attribute precedence](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/splat-attributes-and-arbitrary-parameters#arbitrary-attributes). A supplied class replaces `rf-callout-line` instead of appending to it; retain that base class when adding classes, and avoid unintended marker overrides.

The label span renders only when `Label` is neither null nor empty. Whitespace-only labels still pass that condition. Blazor renders the value as text.

The [isolated stylesheet](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Refraction.Client/Components/Atoms/CalloutLine.razor.css) supplies the horizontal arrangement and basic anchor, line, and label appearance. It has no `data-state` variants implementing active or idle behavior.

## Composition Boundary

The component has no event callbacks, target-element parameter, focus operation, or automatic positioning algorithm. Its current markup has one line span; it does not calculate an angled path or a separate horizontal segment from a target.

Compose it within the application layout and update `Label` or `State` through parent state. Decide whether its text is meaningful to assistive technology or whether the whole callout is decorative, then pass suitable native attributes through `AdditionalAttributes`.

Existing [CalloutLine tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Refraction.Client.L0Tests/Components/Atoms/CalloutLineTests.cs) cover default rendering, optional labels, state markers, and forwarded attributes. Positioning and animation behavior are not supplied by this component.

## Summary

Use `CalloutLine` for a visual label anchor in an application-owned layout. Its label and state are inputs; it does not emit interactions or locate a target.

## Next Steps

- Read [Refraction Reference](./reference.md) for package and component boundaries.
- Read [Scoped Refraction Themes](./themes.md) for shared appearance configuration.
- Read [Refraction Concepts](../concepts/concepts.md) for state ownership and composition.
