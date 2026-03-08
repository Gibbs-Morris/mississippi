---
title: Tributary Reference
sidebar_position: 1
description: Current reference surface for Tributary packages and reducer and snapshot ownership.
---

# Tributary Reference

:::warning Holding Page
This page is a holding page awaiting full content in a future pull request. It exists to establish the navigation path and currently verified subsystem boundary. Some details may still be incomplete or revised as the active documentation set is rebuilt.
:::

## Summary

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

The active docs currently verify the subsystem boundary and representative packages. They do not yet publish a rebuilt reducer catalog, snapshot configuration matrix, or a full operational reference.

## Failure Behavior

Detailed reducer and snapshot failure behavior is not yet documented in the active Tributary section.

## Summary

Use this page as the current active reference for what Tributary owns and which packages expose that surface.

## Next Steps

- Read [Tributary Concepts](../concepts/concepts.md).
- Use [Tributary Operations](../operations/operations.md) for the current operational scope.