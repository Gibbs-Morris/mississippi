---
title: NotificationPulse
description: Refraction's status message molecule with optional expansion and dismissal intents.
sidebar_position: 6
---

# NotificationPulse

`NotificationPulse` is a sealed presentational molecule in
`Mississippi.Refraction.Client.Components.Molecules.Notifications`, supplied by
`Mississippi.Refraction.Client`. It separates live status content from optional
native action buttons and reports one-way intent to its parent.

## Parameters

| Parameter | Default | Contract |
| --- | --- | --- |
| `ChildContent` | `null` | Content rendered inside the stable status region. |
| `Class` | `null` | Additional wrapper classes composed with `rf-notification-pulse`. |
| `ExpandText` | `View details` | Visible expansion action text; must be nonblank when `OnExpand` is supplied. |
| `DismissText` | `Dismiss notification` | Visible dismissal action text; must be nonblank when `OnDismiss` is supplied. |
| `OnExpand` | empty | Optional `EventCallback<MouseEventArgs>` for one-way expansion intent. |
| `OnDismiss` | empty | Optional `EventCallback` for dismissal intent. |
| `State` | `RefractionStates.New` | Visual state hook. `Critical` changes the attention dot color. |
| `AdditionalAttributes` | `null` | Native attributes forwarded to the wrapper; `class` and `data-state` remain component-owned. |

## Exceptions

When `OnExpand` has a delegate, a blank or whitespace-only `ExpandText` causes
an `ArgumentException` with the message `ExpandText must be nonblank when
OnExpand is supplied.` When `OnDismiss` has a delegate, the corresponding
`DismissText` condition throws with `DismissText must be nonblank when OnDismiss
is supplied.`

These checks run in `OnParametersSet`, so Blazor raises the exception while it
applies the initial or updated parameter set, before that set is rendered. The
exception uses a message-only constructor; consumers should not rely on a
specific `ArgumentException.ParamName` value.

## Status and actions

The inner status content renders with `role="status"` and
`aria-atomic="true"`. The attention dot is decorative and carries
`aria-hidden="true"`. Action buttons are siblings outside that status region
and appear only when their callbacks have delegates.

Each action is a native `<button type="button">`. The browser supplies Enter
and Space activation without duplicate key handlers. The wrapper has no
component-owned click handler or tab stop, and the molecule does not add
`aria-expanded` or disclosure state for a region that the parent owns.

The parent owns visibility, status text, and the response to each typed intent.
This split Razor and code-behind example keeps those responsibilities explicit:

```razor title="ExportNotification.razor"
@namespace Example.Components
@using Microsoft.AspNetCore.Components.Web
@using Mississippi.Refraction.Client
@using Mississippi.Refraction.Client.Components.Molecules.Notifications

@if (isVisible)
{
    <NotificationPulse State="@notificationState"
                       ExpandText="View details"
                       DismissText="Dismiss notification"
                       OnExpand="@HandleExpandAsync"
                       OnDismiss="@HandleDismissAsync">
        <p>@statusMessage</p>
    </NotificationPulse>
}
else
{
    <button type="button" @onclick="@HandleRestoreAsync">Restore notification</button>
}
```

```csharp title="ExportNotification.razor.cs"
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Web;

using Mississippi.Refraction.Client;


namespace Example.Components;

/// <summary>Shows parent-owned notification state and actions.</summary>
public sealed partial class ExportNotification
{
    private bool isVisible = true;

    private string notificationState = RefractionStates.New;

    private string statusMessage = "Export completed. Review the result before dismissing it.";

    private Task HandleExpandAsync(
        MouseEventArgs mouseEventArgs
    )
    {
        _ = mouseEventArgs;
        notificationState = RefractionStates.Expanded;
        statusMessage = "Export completed: 24 rows exported.";
        return Task.CompletedTask;
    }

    private Task HandleDismissAsync()
    {
        isVisible = false;
        notificationState = RefractionStates.Acknowledged;
        return Task.CompletedTask;
    }

    private Task HandleRestoreAsync(
        MouseEventArgs mouseEventArgs
    )
    {
        _ = mouseEventArgs;
        isVisible = true;
        notificationState = RefractionStates.New;
        statusMessage = "Export completed. Review the result before dismissing it.";
        return Task.CompletedTask;
    }
}
```

Keep expansion, dismissal, and any details region in the parent. `Critical`
remains a visual hook; this non-intrusive status molecule does not create an
assertive alert, notification service, timer, or domain state.

## Styling and composition

The status content wraps long text and the optional actions retain visible
focus rings and targets of at least 44px. Styles use Refraction surface, text,
status, action, and focus tokens and wrap the action row at narrow widths.
Unmatched attributes reach the wrapper, while its base class and `data-state`
remain stable for composition and state styling.

## Migration from the prototype

Move imports from `Mississippi.Refraction.Client.Components.Atoms` to
`Mississippi.Refraction.Client.Components.Molecules.Notifications`.
`NotificationPulse` is sealed and now renders a status region plus optional
native actions. Replace selectors that assumed the old root `role="status"`,
`tabindex="0"`, or root click behavior. Supply `OnExpand` and/or `OnDismiss`
when the parent owns those intents, and provide nonblank custom action text
when replacing the defaults.

The molecule remains presentational: parents own details, state, focus, and
restoration behavior. When an action changes the surrounding view, the parent
should move focus to the relevant heading or restore control.

## Next Steps

- Read the [Refraction overview](../index.md) for the state-down, events-up UI
  model that this molecule follows.
- Use the [Refraction Reference](./reference.md) to review package boundaries
  and adjacent component contracts.
- Run [Explore Refraction in LightSpeed](../getting-started/lightspeed.md) to
  inspect existing component examples and page-owned state flow in a working
  sample.
- Read [Refraction Concepts](../concepts/concepts.md) when deciding whether a
  behavior belongs in the component or its parent state flow.
