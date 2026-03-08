---
title: Troubleshoot Reservoir Scope And Entry-Point Confusion
sidebar_position: 1
description: Resolve the common problem of starting in Reservoir when the question is really about UI composition or another layer.
---

# Troubleshoot Reservoir Scope And Entry-Point Confusion

:::warning Holding Page
This page is a holding page awaiting full content in a future pull request. It exists to establish the navigation path and currently verified subsystem boundary. Some details may still be incomplete or revised as the active documentation set is rebuilt.
:::

## Symptom

You started in Reservoir, but the question now appears to be about component composition, generated contracts, or another layer outside the state model.

## What This Usually Means

Reservoir is the wrong primary entry point when the issue is not actually the client-state subsystem.

## Probable Causes

- The task belongs to [Refraction](../../refraction/index.md) because it is really about the UI contract.
- The task belongs to [Inlet](../../inlet/index.md) because it is really about generated full-stack alignment.

## How To Confirm

- Stay in Reservoir only if the primary concern is store behavior, reducers, selectors, effects, middleware, or testing that model.

## Resolution

Use [How To Choose A Reservoir Package Entry Point](../how-to/how-to.md) if the issue still belongs to Reservoir. Otherwise switch to the correct subsystem overview.

## Verify The Fix

You should be able to name the primary concern as state model, UI contract, generated composition, or domain behavior.

## Prevention

Start by identifying whether the immediate problem is state transition logic or UI rendering and interaction logic.

## Summary

Reservoir is the correct section only when the client-state subsystem is the core problem.

## Next Steps

- Read [Reservoir Getting Started](../getting-started/getting-started.md).
- Move to [Refraction](../../refraction/index.md) if the issue is really the UI layer.