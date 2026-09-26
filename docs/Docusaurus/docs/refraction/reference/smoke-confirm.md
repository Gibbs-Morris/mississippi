---
title: SmokeConfirm
description: Refraction's presentational confirmation surface with safe native actions.
sidebar_position: 7
---

# SmokeConfirm

`SmokeConfirm` is a sealed presentational confirmation surface in
`Mississippi.Refraction.Client.Components.Organisms.Confirmations`. It renders a
named dialog surface and reports cancel or confirm intent to its parent.

## Applies to

- `Mississippi.Refraction.Client`
- Namespace: `Mississippi.Refraction.Client.Components.Organisms.Confirmations`

## Parameters

| Parameter | Default | Contract |
| --- | --- | --- |
| `AdditionalAttributes` | `null` | Unmatched attributes forwarded to the wrapper. `class`, `role`, `data-state`, `aria-labelledby`, and `aria-describedby` remain component-owned. |
| `CancelText` | `Cancel` | Required nonblank text for the cancel button. |
| `Class` | `null` | Additional wrapper classes, composed with the component class and any caller `class` attribute. |
| `Consequence` | `null` | Optional description; blank values are omitted, while meaningful text is rendered below the title and added to `aria-describedby`. |
| `ConfirmText` | `Confirm` | Required nonblank text for the confirm button. |
| `OnCancel` | empty | Optional parent callback. The cancel button is disabled when no delegate is supplied. |
| `OnConfirm` | empty | Optional parent callback. The confirm button is disabled when no delegate is supplied. |
| `State` | `RefractionStates.Latent` | Visual state value rendered as `data-state`; `Latent` hides the surface and other values are visual hooks whose meaning belongs to the parent. |
| `Title` | none | Required nonblank dialog title and accessible name. |

## Defaults and constraints

Each instance generates a stable heading ID. A meaningful `Consequence` receives a
stable description ID for the lifetime of that instance. Caller
`aria-describedby` tokens are preserved, split on HTML whitespace, and composed
without duplicate tokens before the generated consequence ID.

`Title`, `CancelText`, and `ConfirmText` are validated on every parameter update.
A blank or whitespace-only value throws `ArgumentException` with the parameter
name. Supply a visible title even when the surrounding composition supplies
additional context.

## Behavior

The wrapper has `role="dialog"`, an owned `aria-labelledby` relationship, and a
`data-state` hook. `RefractionStates.Latent` hides the surface; other state
values remain available as visual hooks. The consequence paragraph is omitted
when its value is null, empty, or whitespace-only. The wrapper forwards
unrelated native attributes and keeps its base class, state, and name
relationship authoritative while composing caller description tokens.

Cancel and confirm are native `<button type="button">` elements. They retain
browser Enter and Space behavior without component key handlers and do not
submit an enclosing form. Each callback is independent and awaited by the
Blazor event pipeline; the component does not change parent state or perform
business work. A missing callback disables only its own action.

The surface uses Refraction color, typography, focus, and spacing tokens. Action
targets are at least 44px, focus-visible outlines remain visible, and action
labels wrap at narrow widths. Forced-colors rules provide system color pairings.
Modal lifecycle behavior such as open state, focus containment, Escape handling,
inert background content, and focus restoration belongs to the parent
composition.

## Exceptions

`ArgumentException` is thrown during parameter application when `Title`,
`CancelText`, or `ConfirmText` is blank. The exception identifies the invalid
parameter through `ParamName`.

## Example

The parent owns visibility, callbacks, and any form state. The following
composition starts the surface visibly, closes it from either local callback,
and reopens it with a separate native button. The confirmation actions remain
safe inside the form while the optional consequence description is supplied.

```razor title="Example.razor"
@page "/smoke-confirm-example"
@namespace SmokeConfirmDocumentation
@using Microsoft.AspNetCore.Components.Forms
@using Mississippi.Refraction.Client
@using Mississippi.Refraction.Client.Components.Organisms.Confirmations

<button type="button" @onclick="@(() => IsConfirmationVisible = true)">Open confirmation</button>

<EditForm Model="@Model" OnValidSubmit="SaveFormAsync">
    @if (IsConfirmationVisible)
    {
        <SmokeConfirm State="@RefractionStates.Active"
                      Title="Delete the draft"
                      Consequence="The draft cannot be restored."
                      OnCancel="CancelDeleteAsync"
                      OnConfirm="ConfirmDeleteAsync" />
    }
    <button type="submit">Save form</button>
    <p>@Status</p>
</EditForm>
```

```csharp title="Example.razor.cs"
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;


namespace SmokeConfirmDocumentation;

/// <summary>Demonstrates a parent-owned confirmation surface inside an edit form.</summary>
public sealed partial class Example : ComponentBase
{
    private DraftModel Model { get; } = new();

    private bool IsConfirmationVisible { get; set; } = true;

    private string Status { get; set; } = "Ready";

    private Task CancelDeleteAsync()
    {
        IsConfirmationVisible = false;
        Status = "Delete cancelled";
        return Task.CompletedTask;
    }

    private Task ConfirmDeleteAsync()
    {
        IsConfirmationVisible = false;
        Status = "Delete confirmed";
        return Task.CompletedTask;
    }

    private Task SaveFormAsync(EditContext editContext)
    {
        _ = editContext;
        Status = "Form submitted";
        return Task.CompletedTask;
    }

    private sealed class DraftModel
    {
        public string Draft { get; set; } = string.Empty;
    }
}
```

Use the component's L0 tests as the executable contract for native button type,
callback isolation, disabled transitions, stable IDs, description composition,
and protected attributes.

## Migration from the prototype

Move imports from `Mississippi.Refraction.Client.Components.Organisms` to
`Mississippi.Refraction.Client.Components.Organisms.Confirmations`. Provide a
nonblank `Title`; the title is always rendered as the dialog's accessible name.
Keep `Consequence` for optional explanatory text. Caller `role`, `data-state`,
and `aria-labelledby` values are protected by the component; caller
`aria-describedby` tokens are supported and composed with the generated
consequence ID when one is rendered.

Replace assumptions that the old buttons submit a surrounding form with explicit
parent callbacks. If a callback is unavailable, its action is rendered disabled.
The parent remains responsible for showing or hiding the surface and for modal
focus behavior. Keep that lifecycle composition outside this presentational
component.

## Summary

Use `SmokeConfirm` when a parent needs a named confirmation surface with two
independent, form-safe native action intents and Refraction styling.

## Next steps

- Read [NotificationPulse](./notification-pulse.md) for status content and optional action intents.
- Read [InputField](./input-field.md) for native attribute composition and callback behavior.
- Read [Refraction Concepts](../concepts/concepts.md) for state-down, events-up composition.
