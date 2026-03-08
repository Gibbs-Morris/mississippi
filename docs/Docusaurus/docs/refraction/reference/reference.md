---
id: refraction-reference
title: Refraction Reference
sidebar_label: Reference
sidebar_position: 1
description: Current reference surface for Refraction packages and UX-layer ownership.
---

# Refraction Reference

Refraction is the Mississippi Blazor UX layer organized around a state-down, events-up interaction model.

## Applies To

- `Mississippi.Refraction.Abstractions`
- `Mississippi.Refraction.Client`
- `Mississippi.Refraction.Client.StateManagement`

## Verified Ownership Boundary

- Reusable Blazor UI components
- State-down, events-up interaction conventions
- Composition helpers around that UX model

## Related But Separate Areas

- [Reservoir](../../reservoir/index.md) owns the Redux-style state-management subsystem.
- [Inlet](../../inlet/index.md) owns full-stack generated alignment across client, gateway, and runtime layers.

## Defaults And Constraints

This reference covers the verified interaction model and package boundaries for Refraction.

## Failure Behavior

For component-level failure behavior, refer to standard Blazor rendering diagnostics and the [Refraction Concepts](../concepts/concepts.md) page.

## Summary

Use this page as the current reference boundary for what Refraction owns and which packages expose that surface.

## Next Steps

- Read [Refraction Concepts](../concepts/concepts.md).
- Use [Refraction Troubleshooting](../troubleshooting/troubleshooting.md) if you are still deciding whether the problem belongs here.
