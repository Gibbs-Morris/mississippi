---
title: Troubleshoot Refraction Scope And Entry-Point Confusion
sidebar_position: 1
description: Resolve the common problem of starting in Refraction when the question is really about state management or another layer.
---

# Troubleshoot Refraction Scope And Entry-Point Confusion

## Symptom

You started in Refraction, but the question now appears to be about stores, reducers, effects, or another non-UI concern.

## What This Usually Means

Refraction is the wrong primary entry point when the core problem is outside the UX component contract.

## Probable Causes

- The task belongs to [Reservoir](../../reservoir/index.md) because it is really about state management.
- The task belongs to [Inlet](../../inlet/index.md) because it is really about generated client and gateway alignment.

## How To Confirm

- Stay in Refraction only if the concern is Blazor component composition and the state-down, events-up contract.
- Move sections if the concern is state transitions, projections, or domain behavior.

## Resolution

Use [How To Decide Between Refraction And Reservoir](../how-to/how-to.md) if the question is still on the client side but the boundary is unclear.

## Verify The Fix

You should be able to explain whether the immediate issue is about the UX contract or the state model underneath it.

## Prevention

Start by naming the primary concern: UI contract, state management, generated composition, or domain behavior.

## Summary

Refraction is only the right section when the question is primarily about the UI interaction model.

## Next Steps

- Read [Refraction Getting Started](../getting-started/getting-started.md).
- Move to [Reservoir](../../reservoir/index.md) if the problem is really the state model.
