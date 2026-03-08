---
id: brooks-reference
title: Brooks Reference
sidebar_label: Reference
sidebar_position: 1
description: Current reference surface for Brooks packages and event-stream ownership.
---

# Brooks Reference

Brooks is the Mississippi event-stream and provider foundation.

## Applies To

- `Mississippi.Brooks.Abstractions`
- `Mississippi.Brooks.Runtime`
- `Mississippi.Brooks.Serialization.Abstractions`
- `Mississippi.Brooks.Serialization.Json`

## Verified Ownership Boundary

- Event-stream abstractions and runtime services
- Storage-provider contracts for brook persistence
- Serialization seams used by event persistence and retrieval

## Related But Separate Areas

- [Tributary](../../tributary/index.md) owns reducers and snapshots.
- [Domain Modeling](../../domain-modeling/index.md) owns domain behavior.

## Defaults And Constraints

This reference covers the verified subsystem boundary, representative packages, and provider contracts for Brooks.

## Failure Behavior

For provider and serializer failure behavior, refer to the [Brooks Operations](../operations/operations.md) page and standard Orleans persistence diagnostics.

## Summary

Use this page as the current active reference for what Brooks owns and which packages expose that surface.

## Next Steps

- Read [Brooks Concepts](../concepts/concepts.md).
- Use [Brooks Operations](../operations/operations.md) for the current operational scope.
