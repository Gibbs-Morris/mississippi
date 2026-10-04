---
id: domain-modeling-reference
title: Domain Modeling Reference
sidebar_label: Reference
sidebar_position: 1
description: Current reference surface for Domain Modeling packages and domain-facing ownership.
---

# Domain Modeling Reference

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

This reference covers the verified subsystem boundary, representative packages, and domain-facing contracts for Domain Modeling.

## Failure Behavior

For runtime and orchestration failure behavior, refer to the [Domain Modeling Concepts](../concepts/concepts.md) page and standard Orleans grain lifecycle diagnostics.

## Summary

Use this page as the current active reference for what Domain Modeling owns and which packages expose that surface.

## Next Steps

- Read [Aggregate Keys](./aggregate-keys.md) for entity-only identity and validation boundaries.
- Read [Event Effect Dispatch](./event-effect-dispatch.md) for matching, ordering, and handler failure isolation.
- Read [Aggregate Effect Iterations](./effect-iterations.md) for cascade limits and already-persisted follow-up events.
- Read [Worker Event Effects](./worker-effects.md) for envelopes, state inputs, routing, and failure observation.
- Read [Persisted Type Registries](./type-registries.md) for lookup, collisions, and assembly-scan counts.
- Read [Domain Operation Results](./operation-results.md) for factories, typed values, and conversion.
- Read [Projection Cache Keys](./projection-cache-keys.md) for versioned cache identity and parsing.
- Read [Projection Cursors](./projection-cursors.md) for shared identity and cached storage or accepted-notification positions.
- Read [Projection Reads](./projection-reads.md) for latest and explicitly versioned reads.
- Read [Command Handler Test Assertions](./command-handler-tests.md) for isolated event and failure-result checks.
- Read [Projection Test Scenarios](./projection-tests.md) for in-memory replay and scenario assertions.
- Read [Isolated Reducer Test Assertions](./reducer-tests.md) for output and exception checks.
- Read [Domain Modeling Concepts](../concepts/concepts.md).
- Use the [Spring Sample](../../samples/spring-sample/index.md) to see domain modeling patterns in practice.
