---
title: Reservoir Reference
sidebar_position: 1
description: Current reference surface for Reservoir packages and client-state ownership.
---

# Reservoir Reference

## Summary

Reservoir is the Mississippi client-state management subsystem.

## Applies To

- `Mississippi.Reservoir.Abstractions`
- `Mississippi.Reservoir.Core`
- `Mississippi.Reservoir.Client`
- `Mississippi.Reservoir.TestHarness`

## Verified Ownership Boundary

- Store and dispatch pipeline abstractions
- Feature state, actions, reducers, selectors, effects, and middleware
- Client integration and testing support for that model

## Related But Separate Areas

- [Refraction](../../refraction/index.md) owns the Blazor UX layer.
- [Inlet](../../inlet/index.md) owns generated full-stack alignment.

## Defaults And Constraints

The active docs currently verify the subsystem boundary and representative package entry points. They do not yet publish the rebuilt API-level reference or the full preserved guidance from the archived section.

## Failure Behavior

Detailed runtime and API-level failure behavior is not yet republished in the active Reservoir section.

## Summary

Use this page as the current active reference for what Reservoir owns and which packages expose that surface.

## Next Steps

- Read [Reservoir Concepts](../concepts/concepts.md).
- Use [Archived Reservoir Docs](../../archived/client-state-management/reservoir.md) for preserved deep material.
