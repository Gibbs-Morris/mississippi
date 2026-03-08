---
title: Brooks Reference
sidebar_position: 1
description: Current reference surface for Brooks packages and event-stream ownership.
---

# Brooks Reference

## Summary

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

The active docs currently verify the subsystem boundary and representative packages. They do not yet publish a rebuilt provider matrix, serializer contract details, or a full operational reference.

## Failure Behavior

Detailed provider and serializer failure behavior is not yet documented in the active Brooks section.

## Summary

Use this page as the current active reference for what Brooks owns and which packages expose that surface.

## Next Steps

- Read [Brooks Concepts](../concepts/concepts.md).
- Use [Brooks Operations](../operations/operations.md) for the current operational scope.
