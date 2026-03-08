---
id: refraction-how-to
title: How To Decide Between Refraction And Reservoir
sidebar_label: How To
sidebar_position: 1
description: Choose Refraction when the problem is the UI interaction contract and Reservoir when it is the state-management model.
---

# How To Decide Between Refraction And Reservoir

## When To Use This Page

Use this page when a client-side question spans both UI composition and state management and you need the correct primary entry point.

## Before You Begin

- Read the [Refraction overview](../index.md).
- Read the [Reservoir overview](../../reservoir/index.md).

## Steps

1. Choose Refraction when the primary concern is component interaction, parameters, and events.
2. Choose Reservoir when the primary concern is state transitions, reducers, selectors, effects, or middleware.
3. Use both sections when the application composes Reservoir beneath Refraction, but start with the subsystem that owns the immediate problem.

## Verify The Result

- You should be able to describe the issue as either UI contract first or state-management first.

## Summary

Refraction owns the Blazor UX contract. Reservoir owns the client-state model.

## Next Steps

- Use [Refraction Reference](../reference/reference.md) if the answer is Refraction.
- Switch to [Reservoir](../../reservoir/index.md) if the answer is Reservoir.
