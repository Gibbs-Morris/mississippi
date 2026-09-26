---
title: Emitter
description: Refraction's accessible native-button emitter and controlled interaction contract.
sidebar_position: 5
---

# Emitter

`Emitter` is a sealed presentational atom in
`Mississippi.Refraction.Client.Components.Atoms.Activation`, supplied by
`Mississippi.Refraction.Client`. It renders a native button around the Refraction
8px seed and reports interaction to its parent through typed callbacks.

## Parameters

| Parameter | Default | Contract |
| --- | --- | --- |
| `Label` | `null` | Optional visible button label. When blank, a nonblank string `aria-label` or `aria-labelledby` is required. |
| `Class` | `null` | Additional classes composed with `rf-emitter` and any caller class attribute. |
| `IsDisabled` | `false` | Disables the button when `true`. |
| `State` | `RefractionStates.Idle` | Visual state hook. `Disabled` also disables the button; other values remain available to styling. |
| `OnActivate` | empty | Receives the native `MouseEventArgs` when enabled. |
| `OnFocus` | empty | Receives the native `FocusEventArgs` when enabled. |
| `AdditionalAttributes` | `null` | Native attributes such as accessible names and test identifiers. `aria-label` and `aria-labelledby` must be strings; controlled attributes remain component-owned. |

## Native interaction and accessibility

The atom renders `<button type="button">`, so the browser supplies the button
role and Enter/Space keyboard activation. The component does not add keydown
handlers that could invoke activation twice. Native focus replaces the
prototype's explicit `tabindex`.

The optional `Label` is visible inside the button. Every rendered emitter must
have a meaningful name: supply a nonblank `Label`, a nonblank string
`aria-label`, or a nonblank string `aria-labelledby`. When `Label` is blank and no nonblank string ARIA name
is supplied, `InvalidOperationException` is thrown during parameter
application. Non-string ARIA naming values are rejected separately. The seed
is decorative and carries `aria-hidden="true"`.

Use `aria-labelledby` when an existing nonempty element supplies the name. The
guard checks only the parameter value. A nonblank ID that has no matching
element, or references an empty element, still passes parameter validation.
Callers must supply the ID or space-separated IDs of existing nonempty labeling
elements; the atom does not resolve IDs or inspect remote DOM content.

When callers provide case variants of one naming attribute, the last value is
validated and rendered. Keep one nonblank string value to make the effective
accessible name clear.

```razor
<h2 id="emitter-title">Send an intent</h2>
<Emitter aria-labelledby="emitter-title" />
```

`IsDisabled` and `State == RefractionStates.Disabled` combine into one effective
disabled state. The rendered `disabled`, `aria-disabled`, and `data-state`
attributes agree, and activation/focus callbacks are ignored while disabled.
When the effective state is enabled, values such as `Active` and `Busy` remain
available as visual hooks without imposing application policy.

## Styling and composition

The button target is at least 44px in both dimensions while the seed remains
8px. Isolated styles use Refraction surface, action, text, and focus tokens for
normal, disabled, hover, pressed, and focus-visible states. Unmatched native
attributes are forwarded; `type`, disabled state, required classes, state, and
event wiring remain controlled by the component.

## Migration from the prototype

Move imports from `Mississippi.Refraction.Client.Components.Atoms` to
`Mississippi.Refraction.Client.Components.Atoms.Activation`. Replace any
prototype assumptions about `role="button"`, `tabindex="0"`, or a generic
wrapper with the native button contract. Provide a nonblank `Label` for a
visible action, or provide a nonblank string `aria-label`/`aria-labelledby` for
icon-only use. For `aria-labelledby`, supply IDs of existing nonempty labeling
elements. If no nonblank label or ARIA value is supplied,
`InvalidOperationException` is thrown; non-string ARIA naming values are
rejected as well.

The atom remains presentational: pages or container components own state and
dispatch, while `OnActivate` and `OnFocus` report intent upward. The
[LightSpeed kitchen sink](../getting-started/lightspeed.md) demonstrates this
flow with a controlled disabled checkbox and activation count.
