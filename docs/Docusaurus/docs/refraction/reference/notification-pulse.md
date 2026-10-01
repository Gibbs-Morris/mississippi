---
title: NotificationPulse
description: Exact status, action, and parameter contract for Refraction NotificationPulse.
sidebar_position: 6
---

# NotificationPulse

## Overview

`NotificationPulse` renders a status region and optional native action buttons.
It is a sealed presentational molecule in
`Mississippi.Refraction.Client.Components.Molecules.Notifications`.
The parent owns notification state, details, visibility, and focus after an action.

## Parameters

| Parameter | Default | Contract |
| --- | --- | --- |
| `ChildContent` | `null` | Content inside the status region. |
| `State` | `RefractionStates.New` | Visual state on the wrapper's `data-state`; `Critical` changes the dot color. |
| `OnExpand` | No delegate | `EventCallback<MouseEventArgs>` for a one-way expansion request. |
| `OnDismiss` | No delegate | `EventCallback` for a dismissal request. |
| `ExpandText` | `View details` | Visible button text when `OnExpand` has a delegate. |
| `DismissText` | `Dismiss notification` | Visible button text when `OnDismiss` has a delegate. |
| `Class` | `null` | Additional wrapper CSS classes. |
| `AdditionalAttributes` | `null` | Unmatched HTML attributes on the wrapper. Caller `class` joins the component classes; `data-state` stays component-owned. |

## Status and action behavior

The inner content region has `role="status"` and `aria-atomic="true"`.
The dot has `aria-hidden="true"`. Actions are siblings of the status region,
so button text is outside the announced content. The component does not add
an assertive alert, disclosure state, `aria-expanded`, a root click handler,
or a root tab stop.

An action appears only while its callback has a delegate. Each action is a
native `<button type="button">`, so it receives native Enter and Space
activation and does not submit a containing form. The callbacks report intent;
the component does not change its own `State` or remove itself.

The status text, callback availability, and visual state update when the parent
supplies new parameters. `Critical` changes only styling, not the live-region
role. The action row wraps long labels in narrow containers. Each action has a
minimum 44px width and height and a visible keyboard focus outline.

## Validation failures

When `OnExpand` has a delegate, blank or whitespace-only `ExpandText` throws
`ArgumentException` during `OnParametersSet`. `OnDismiss` and `DismissText`
follow the same rule. Initial rendering and later parameter updates can both
trigger these failures. These message-only exceptions do not promise a
`ParamName` value.

## Example

This complete Razor component keeps the message and visibility in its parent:

```razor
@using Microsoft.AspNetCore.Components.Web
@using Mississippi.Refraction.Client.Components.Molecules.Notifications

@if (visible)
{
    <NotificationPulse OnExpand="@ShowDetails" OnDismiss="@Dismiss">
        Export complete.
    </NotificationPulse>
    @if (showDetails)
    {
        <p>24 rows exported.</p>
    }
}

@code {
    private bool visible = true;
    private bool showDetails;

    private void ShowDetails(MouseEventArgs _) => showDetails = true;
    private void Dismiss() => visible = false;
}
```

## Pre-release API change

The prototype lived in `Mississippi.Refraction.Client.Components.Atoms`.
Update imports to the molecule namespace above. The former focusable status
root and its click behavior are replaced by an inner status region and
callback-supported buttons; update selectors and event handling accordingly.
The component is sealed. This describes a pre-release source change, not a
versioned upgrade path.

## Summary

`NotificationPulse` exposes status content and independent expansion and
dismissal intents. The parent controls what those intents do.

## Next Steps

- See the [Refraction reference](./reference.md) for related controls.
- See [Refraction concepts](../concepts/concepts.md) for the state-down,
  events-up boundary.
