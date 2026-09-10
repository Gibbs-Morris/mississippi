---
title: InputField
description: Refraction InputField identity, native attributes, and controlled value contract.
sidebar_position: 2
---

# InputField

`InputField` renders a native HTML input with an optional associated label.
It is a sealed presentational atom in
`Mississippi.Refraction.Client.Components.Atoms.Input`, supplied by
`Mississippi.Refraction.Client`.
It receives values through parameters and reports user input through callbacks.

## Identity and attributes

| Parameter | Default | Behavior |
| --- | --- | --- |
| `Id` | Empty string | A nonblank value identifies the input. Otherwise the component uses a unique ID that remains stable for its lifetime. |
| `Label` | `null` | Nonempty text renders a label whose `for` matches the input ID. |
| `InputAttributes` | `null` | Attributes such as `name`, `autocomplete`, `inputmode`, `required`, `aria-label`, and `aria-describedby` are applied to the native input. |
| `AdditionalAttributes` | `null` | Unmatched attributes are applied to the outer wrapper, as in earlier releases. |

Explicit component attributes and callbacks take precedence over entries in
`InputAttributes`. Use the dedicated `Id`, `Type`, `Value`, `Placeholder`,
`IsDisabled`, and `IsReadOnly` parameters for those settings.
The generated ID is not a persistent business identifier and changes when a
component instance is recreated.

Provide a visible `Label` wherever possible. For an intentionally unlabeled
input, supply an accessible name through `InputAttributes`.
Caller-supplied `aria-describedby` values need to identify existing description
elements. A placeholder does not replace a label.

## Value and interaction

`Value` defaults to an empty string, `Type` to `text`, and `State` to `idle`.
`IsDisabled` and `IsReadOnly` default to `false`.
`ValueChanged` reports text on the native input event; it does not assign
`Value`. The parent supplies the next value.
`OnFocus` and `OnBlur` forward the native focus events.

The browser implements native input behavior. This atom does not manage an
`EditContext`, run application validation, dispatch Reservoir actions, or call
APIs. Store integration belongs in the containing page.

## Pre-release API change

The component moved from `Mississippi.Refraction.Client.Components.Atoms` to
`Mississippi.Refraction.Client.Components.Atoms.Input` and is now sealed.
Update C# imports and Razor `@using` directives to the new namespace.
Compose the component through parameters and callbacks instead of inheritance.
Existing wrapper attributes retain their target; native input attributes use
the new `InputAttributes` parameter.

## Related reference

- [Refraction reference](./reference.md)
- [Refraction and Reservoir boundaries](../how-to/how-to.md)
