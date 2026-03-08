---
title: Domain Modeling Reference
sidebar_position: 1
description: Current reference surface for Domain Modeling packages and domain-facing ownership.
---

# Domain Modeling Reference

:::warning Holding Page
This page is a holding page awaiting full content in a future pull request. It exists to establish the navigation path and currently verified subsystem boundary. Some details may still be incomplete or revised as the active documentation set is rebuilt.
:::

## Summary

Domain Modeling is the Mississippi domain-facing layer for aggregates, sagas, effects, and UX projections.

## Applies To

- `Mississippi.DomainModeling.Abstractions`
- `Mississippi.DomainModeling.Runtime`
- `Mississippi.DomainModeling.Gateway`
- `Mississippi.DomainModeling.TestHarness`

## Verified Ownership Boundary

- Aggregate command-handling abstractions and runtime support
- Saga orchestration surfaces
- Event-effect patterns attached to domain behavior
- UX projection abstractions and runtime support

## Related But Separate Areas

- [Tributary](../../tributary/index.md) owns reducers and snapshots.
- [Brooks](../../brooks/index.md) owns raw event streams.
- [Inlet](../../inlet/index.md) owns full-stack generated alignment.

## Defaults And Constraints

The active docs currently verify the subsystem boundary and representative packages. They do not yet publish a rebuilt API-level reference for aggregates, sagas, effects, or UX projections.

## Failure Behavior

Detailed runtime and orchestration failure behavior is not yet documented in the active Domain Modeling section.

## Summary

Use this page as the current active reference for what Domain Modeling owns and which packages expose that surface.

## Next Steps

- Read [Domain Modeling Concepts](../concepts/concepts.md).
- Use [Spring Sample](../../samples/spring-sample/index.md) for a verified sample entry point while detailed active pages are rebuilt.