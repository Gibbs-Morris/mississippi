---
id: domain-modeling-reference
title: Domain Modeling Reference
sidebar_label: Reference
sidebar_position: 1
description: Current reference surface for Domain Modeling packages and domain-facing ownership.
---

# Domain Modeling Reference

Domain Modeling is the Mississippi domain-facing layer for aggregates, sagas, effects, UX projections, and projection replication sinks.

## Applies To

- `Mississippi.DomainModeling.Abstractions`
- `Mississippi.DomainModeling.Runtime`
- `Mississippi.DomainModeling.Gateway`
- `Mississippi.DomainModeling.TestHarness`
- `Mississippi.DomainModeling.ReplicaSinks.Abstractions`
- `Mississippi.DomainModeling.ReplicaSinks.Runtime`
- `Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions`
- `Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Bootstrap`
- `Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos`

## Verified Ownership Boundary

- Aggregate command-handling abstractions and runtime support
- Saga orchestration surfaces
- Event-effect patterns attached to domain behavior
- UX projection abstractions and runtime support
- Projection replication sink metadata, runtime delivery, bounded dead-letter operator APIs, and provider-facing storage abstractions

## Related But Separate Areas

- [Tributary](../../tributary/index.md) owns reducers and snapshots.
- [Brooks](../../brooks/index.md) owns raw event streams.
- [Inlet](../../inlet/index.md) owns full-stack generated alignment.

## Defaults And Constraints

This reference covers the verified subsystem boundary, representative packages, and domain-facing contracts for Domain Modeling.

Projection replication sinks are documented as a bounded single-instance latest-state slice. Use the focused reference page for their public APIs, provider defaults, operator surface, and carry-forward limitations.

## Failure Behavior

For runtime and orchestration failure behavior, refer to the [Domain Modeling Concepts](../concepts/concepts.md) page, [Projection replication sinks](../concepts/projection-replication-sinks.md), and [Projection replication sinks reference](./projection-replication-sinks.md).

## Summary

Use this page as the current active reference for what Domain Modeling owns and which packages expose that surface.

## Next Steps

- Read [Domain Modeling Concepts](../concepts/concepts.md).
- Read [Projection replication sinks reference](./projection-replication-sinks.md).
- Use the [Spring Sample](../../samples/spring-sample/index.md) to see domain modeling patterns in practice.
