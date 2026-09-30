---
id: emitter
title: Emitter
sidebar_label: Emitter
sidebar_position: 5
description: Native action behavior, naming, state, attributes, and callbacks for Refraction Emitter.
---

# Emitter

## Overview

`Emitter` is a presentational native button in `Mississippi.Refraction.Client.Components.Atoms.Activation`. It reports activation and focus through callbacks; its parent owns state.

## Parameters

| Parameter | Default | Contract |
| --- | --- | --- |
| `Label` | `null` | Visible button text. A blank value requires an ARIA naming attribute. |
| `Class` | `null` | Additional classes alongside `rf-emitter` and any unmatched `class`. |
| `IsDisabled` | `false` | Disables native interaction when true. |
| `State` | `RefractionStates.Idle` | Visual state hook. `Disabled` also disables interaction. |
| `OnActivate` | Empty | Receives `MouseEventArgs` when enabled. |
| `OnFocus` | Empty | Receives `FocusEventArgs` when enabled. |
| `AdditionalAttributes` | `null` | Forwards unmatched native attributes, including ARIA names. |

## Behavior

The component renders `<button type="button">`. The browser supplies click, Enter, and Space activation without a separate key handler. The button does not submit a containing form. Its target is at least 44 by 44 CSS pixels; the 8px seed is decorative and hidden from the accessibility tree.

Every render requires a nonblank `Label`, string `aria-label`, or string `aria-labelledby`. The parameter guard checks the supplied values. It does not resolve an `aria-labelledby` ID or inspect its target, so callers must reference existing elements with meaningful text. When case variants of a naming attribute are supplied, the last value is forwarded and checked.

`IsDisabled` or `State == RefractionStates.Disabled` sets native `disabled`, `aria-disabled`, and `data-state` together. Activation and focus callbacks are also guarded when disabled. Other state values are visual hooks. `type`, disabled state, base classes, state, and event wiring remain component-owned when unmatched attributes are supplied.

## Failure behavior

An absent or blank naming value throws `InvalidOperationException` during parameter application. A supplied non-string `aria-label` or `aria-labelledby` also throws. A nonblank `aria-labelledby` ID with no matching or nonempty target passes parameter validation but may leave the rendered button unnamed.

## Compatibility

The previous prototype was in `Mississippi.Refraction.Client.Components.Atoms` and rendered a focusable `div`. Import the `Activation` namespace, provide a name, and update selectors or tests that assumed a `div`, explicit button role, or `tabindex="0"`.

## Summary

Emitter is a named native action control with parent-owned state and typed callbacks.

## Next Steps

- [Run the LightSpeed gallery](../getting-started/lightspeed.md) to exercise the controlled action.
- [Refraction reference](./reference.md) lists related component contracts.
