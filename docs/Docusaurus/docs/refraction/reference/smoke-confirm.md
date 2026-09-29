---
id: smoke-confirm
title: SmokeConfirm
sidebar_label: SmokeConfirm
sidebar_position: 7
description: Exact parameters and behavior for Refraction's parent-controlled confirmation surface.
---

# SmokeConfirm

## Overview

`SmokeConfirm` renders a named confirmation surface and reports cancel or confirm intent to its parent. Its native actions are safe inside a surrounding form.

## Applies To

- Package: `Mississippi.Refraction.Client`
- Namespace: `Mississippi.Refraction.Client.Components.Organisms.Confirmations`
- Parent responsibility: visibility, response to callbacks, and any modal focus lifecycle

## Parameters

| Parameter | Default | Contract |
| --- | --- | --- |
| `Title` | `null` | Required nonblank text for the visible heading and dialog name. |
| `CancelText` | `Cancel` | Required nonblank cancel action label. |
| `ConfirmText` | `Confirm` | Required nonblank confirm action label. |
| `Consequence` | `null` | Optional text beneath the title. Blank text is omitted. |
| `OnCancel` | Empty callback | Parent callback for cancel intent. A missing delegate disables only Cancel. |
| `OnConfirm` | Empty callback | Parent callback for confirm intent. A missing delegate disables only Confirm. |
| `State` | `RefractionStates.Latent` | Parent-owned visual state exposed as `data-state`; `Latent` hides the surface. |
| `Class` | `null` | Additional wrapper classes. |
| `AdditionalAttributes` | `null` | Unmatched attributes forwarded to the wrapper, subject to the owned attributes below. |

## Behavior

The wrapper uses `role="dialog"` and points `aria-labelledby` to its visible title. Each instance keeps a stable, distinct title ID. A nonblank `Consequence` adds a stable description ID and an `aria-describedby` relationship. Caller `aria-describedby` tokens are retained without duplicates and combined with that description ID when the consequence is shown.

The wrapper retains its own `class`, `role`, `data-state`, `aria-labelledby`, and composed `aria-describedby` values. Caller classes are combined with the base class and `Class` parameter; unrelated attributes pass through.

Cancel and Confirm render as `<button type="button">`. Each enabled action invokes only its matching `EventCallback`, so neither action implicitly submits an enclosing form. A missing callback disables its action. Native buttons provide Enter and Space activation. The component does not change `State` or perform business work.

Action targets have a 44px minimum in each dimension. The component provides a visible keyboard focus outline, wrapping at narrow widths, and forced-colors styling. The parent remains responsible for showing or hiding the surface and for focus containment, Escape behavior, background inertness, and focus restoration if a modal interaction is required.

## Failure Behavior

A null, empty, or whitespace-only `Title`, `CancelText`, or `ConfirmText` throws `ArgumentException` during parameter application with the invalid parameter name in `ParamName`.

## Compatibility

The earlier prototype used `Mississippi.Refraction.Client.Components.Organisms`. Update the `@using` directive when adopting the confirmation-folder namespace above. This is a source-breaking namespace move.

## Summary

`SmokeConfirm` is a parent-controlled confirmation surface with two independent, form-safe actions and a required accessible name.

## Next Steps

- Read [Refraction Concepts](../concepts/concepts.md) for the state-down, events-up model.
- Read [InputField](./input-field.md) for another component's attribute and callback contract.
