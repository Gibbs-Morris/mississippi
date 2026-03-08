---
id: tributary-reference
title: Tributary Reference
sidebar_label: Reference
sidebar_position: 1
description: Current reference surface for Tributary packages and reducer and snapshot ownership.
---

# Tributary Reference

Tributary is the Mississippi reducer and snapshot layer.

## Applies To

- `Mississippi.Tributary.Abstractions`
- `Mississippi.Tributary.Runtime`
- `Mississippi.Tributary.Runtime.Storage.Abstractions`
- `Mississippi.Tributary.Runtime.Storage.Cosmos`

## Verified Ownership Boundary

- Event reducer abstractions and runtime composition
- Snapshot abstractions and storage-provider seams
- Runtime support for rebuilding derived state efficiently

## Related But Separate Areas

- [Brooks](../../brooks/index.md) owns raw event streams.
- [Domain Modeling](../../domain-modeling/index.md) owns domain behavior.

## Defaults And Constraints

This reference covers the verified subsystem boundary, representative packages, and reducer and snapshot contracts for Tributary.

## Failure Behavior

For reducer and snapshot failure behavior, refer to the [Tributary Operations](../operations/operations.md) page and standard Orleans grain persistence diagnostics.

## Summary

Use this page as the current active reference for what Tributary owns and which packages expose that surface.

## Next Steps

- Read [Tributary Concepts](../concepts/concepts.md).
- Use [Tributary Operations](../operations/operations.md) for the current operational scope.
