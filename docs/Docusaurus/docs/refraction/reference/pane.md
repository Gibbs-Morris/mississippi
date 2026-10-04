---
title: Pane
description: Reference Pane title, main content and footer slots, presentation markers, and interaction ownership.
sidebar_position: 10
---

# Pane

`Pane` wraps application content in a section with an optional title and footer. It accepts presentation markers and render fragments; application code owns the interactions inside and around that surface.

## Applies To

- `Mississippi.Refraction.Client.Components.Organisms.Pane`
- Blazor composition using `Mississippi.Refraction.Client`

## Parameters

The [component parameters](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Refraction.Client/Components/Organisms/Pane.razor.cs) are:

| Parameter | Default | Behavior |
| --- | --- | --- |
| `Title` | `null` | Text rendered in the optional header's `h2` |
| `ChildContent` | `null` | Main content rendered inside the content wrapper |
| `Footer` | `null` | Optional footer render fragment |
| `Variant` | `RefractionPaneVariants.Primary` (`"primary"`) | Root `data-variant` marker |
| `State` | `RefractionStates.Idle` (`"idle"`) | Root `data-state` marker |
| `Depth` | `RefractionDepthBands.Mid` (`"mid"`) | Root `data-depth` marker |
| `AdditionalAttributes` | `null` | Unmatched attributes forwarded to the root section |

The variant, state, and depth inputs are strings without component-level validation. [`RefractionPaneVariants`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Refraction.Client/RefractionPaneVariants.cs) defines primary, orbital, and telemetry names; [`RefractionDepthBands`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Refraction.Client/RefractionDepthBands.cs) defines near, mid, and far names.

## Conditional Markup

The [Razor markup](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Refraction.Client/Components/Organisms/Pane.razor) always renders its root `section` and main content wrapper.

The header and `h2` render only when `Title` is neither null nor empty. Whitespace-only titles still satisfy that condition. A null `ChildContent` leaves the main content wrapper present without fragment content.

The footer wrapper renders whenever `Footer` is non-null. An empty render fragment therefore still produces a footer wrapper; a null fragment omits the entire footer.

## Composition Boundary

`Pane` exposes no event callbacks or built-in open, close, or focus routines. A state or depth marker does not make it a modal dialog or manage browser focus. Put interactive controls in its fragments and handle their events in the owning application.

The title uses a fixed `h2`; choose surrounding headings accordingly. Pass native naming or other section attributes through `AdditionalAttributes` when the application's content requires them. The component does not infer those attributes from `Title`.

Existing [Pane tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Refraction.Client.L0Tests/Components/Organisms/PaneTests.cs) cover defaults, omitted headers and footers, supplied title/content/footer fragments, alternate markers, and forwarded attributes.

## Summary

Use `Pane` as a content surface with independently optional title and footer slots. Its markup and markers provide presentation while the parent application owns interaction and accessible composition.

## Next Steps

- Read [Refraction Reference](./reference.md) for component and package boundaries.
- Read [Scoped Refraction Themes](./themes.md) for shared appearance configuration.
- Read [Refraction Concepts](../concepts/concepts.md) for application state ownership.
