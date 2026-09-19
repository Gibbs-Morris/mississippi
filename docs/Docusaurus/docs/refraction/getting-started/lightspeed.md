---
title: Explore Refraction in LightSpeed
description: Run the LightSpeed sample and inspect its Reservoir-driven component gallery.
sidebar_position: 2
---

# Explore Refraction in LightSpeed

LightSpeed demonstrates Refraction inputs, progress arcs, an accessible emitter, and scoped themes with
Reservoir client state. Its kitchen sink shows the selected state after each action.

## What you will achieve

Run a local component gallery, switch its theme, and inspect form actions in
Reservoir without setting up a domain backend.

## Prerequisites

- A repository checkout and the .NET SDK selected by `global.json`.
- PowerShell.

## Install

The run command restores the sample's dependencies from the repository's
central package configuration. No sample-specific credentials are required.

## Run the sample

From the repository root:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --no-launch-profile --project samples/LightSpeed/LightSpeed.Gateway/LightSpeed.Gateway.csproj --urls http://127.0.0.1:5278
```

Open `http://127.0.0.1:5278`, then select **Kitchen sink**.
Change the color theme, clear the work email, and select **Validate profile**.
Enter a valid address to clear the error. Select **Reset example** to restore
the starting form while retaining the chosen theme.

In **Completion at a glance**, select a percentage or **Unknown duration**.
The arc and the inspector follow the same action. Resetting the form preserves
this selection; no background operation starts.

In **Send an intent**, activate **Emit signal** and observe the activation count.
Toggle **Disable emitter**, try the button again, and reset the example. The
button stays disabled and its activation count survives the reset.

In **Keep the team moving**, choose **View details** to open the separate
details region, **Dismiss notification** to hide and collapse the message, and
**Restore notification** to show it again in the collapsed state. Each action
travels from the presentational organism through the page and Reservoir to the
selected inspector values. Focus moves to the changed details, restore action,
or stable section heading after the parent state update; the organism does not
steal the initial route-heading focus.

## Verify it works

The form displays an associated error for an empty submitted address.
The state inspector updates its email, emitter count, notification visibility
and expansion, action count, and last action.
Theme choices persist between the overview and kitchen-sink routes in the same
browser session. Reloading starts a new in-memory store.

## What happened

Pages inherit `StoreComponent`, select a `ShowcaseView`, and dispatch actions.
The shell and form receive parameters and emit callbacks. These local UI
actions are handwritten because no domain generator covers this scenario.
The sample does not persist profile changes or make domain API calls.

## Summary

The running gallery connects presentational controls to one local Reservoir
feature, with a visible action and state trail. The notification organism keeps
the message and details presentational while the page owns visibility and
expansion; the organism coordinates the pending focus target around those parent
state updates.

## Next Steps

- [InputField contract](../reference/input-field.md)
- [ProgressArc contract](../reference/progress-arc.md)
- [Emitter contract](../reference/emitter.md)
- [NotificationPulse contract](../reference/notification-pulse.md)
- [Scoped themes](../reference/themes.md)
- [Reservoir overview](../../reservoir/index.md)
