---
id: aqueduct-reference
title: Aqueduct Reference
sidebar_label: Reference
sidebar_position: 1
description: Current reference surface for Aqueduct packages and subsystem ownership.
---

# Aqueduct Reference

Aqueduct is the Mississippi subsystem for Orleans-backed SignalR backplane integration.

## Applies To

- `Mississippi.Aqueduct.Abstractions`
- `Mississippi.Aqueduct.Gateway`
- `Mississippi.Aqueduct.Runtime`

## Verified Ownership Boundary

- Distributed SignalR backplane integration
- Orleans-driven push delivery of events and notifications into SignalR-connected clients
- Gateway-side hub lifetime management and notifier registration
- Runtime-side backplane registration
- Aqueduct-specific options and abstractions for distributed message routing

## Related But Separate Areas

- [Inlet](../../inlet/index.md) composes with Aqueduct for higher-level projection delivery.
- [Domain Modeling](../../domain-modeling/index.md) owns domain behavior, not transport infrastructure.

## Defaults And Constraints

The active docs currently verify the subsystem boundary and package entry points. They do not yet publish a complete configuration matrix, operational default table, or supported-topology guide for Aqueduct.

## Failure Behavior

Detailed failure behavior is not documented here yet because it has not been verified for publication.

## Summary

Use this page as the current reference boundary for what Aqueduct owns and which packages expose that surface.

## Next Steps

- Read [Aqueduct Concepts](../concepts/concepts.md).
- Read [Aqueduct Operations](../operations/operations.md) for the current operational scope.
