---
id: reservoir-reference
title: Reservoir Reference
sidebar_label: Reference
sidebar_position: 1
description: Current reference surface for Reservoir packages and client-state ownership.
---

# Reservoir Reference

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

This reference covers the verified subsystem boundary and representative package entry points for Reservoir. See [Archived Reservoir Docs](../../archived/client-state-management/reservoir.md) for deeper API-level material.

## Failure Behavior

For runtime and API-level failure behavior, refer to the [Archived Reservoir Docs](../../archived/client-state-management/reservoir.md) and the [Reservoir Concepts](../concepts/concepts.md) page.

## Summary

Use this page as the current active reference for what Reservoir owns and which packages expose that surface.

## Next Steps

- Read [Reservoir Concepts](../concepts/concepts.md).
- Use [Archived Reservoir Docs](../../archived/client-state-management/reservoir.md) for preserved deep material.
