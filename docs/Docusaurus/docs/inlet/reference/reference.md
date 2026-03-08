---
id: inlet-reference
title: Inlet Reference
sidebar_label: Reference
sidebar_position: 1
description: Current reference surface for Inlet packages and cross-layer ownership.
---

# Inlet Reference

Inlet is the Mississippi composition and source-generation layer.

## Applies To

- `Mississippi.Inlet.Abstractions`
- `Mississippi.Inlet.Client`
- `Mississippi.Inlet.Gateway`
- `Mississippi.Inlet.Runtime`

## Verified Ownership Boundary

- Shared abstractions for projection paths and related metadata
- Client support for projection state and subscriptions
- Gateway support for generated APIs and SignalR delivery
- Runtime support for discovery and generated registrations
- Source generators that align those layers

## Related But Separate Areas

- [Aqueduct](../../aqueduct/index.md) owns the real-time backplane.
- [Reservoir](../../reservoir/index.md) owns the client-state model.
- [Domain Modeling](../../domain-modeling/index.md) owns domain behavior.

## Defaults And Constraints

This reference covers the verified subsystem boundary, representative packages, and generated-surface contracts for Inlet.

## Failure Behavior

For generator and runtime registration failure behavior, refer to the [Inlet Operations](../operations/operations.md) page. Generator misalignment typically surfaces at compile time.

## Summary

Use this page as the current active reference for what Inlet owns and which packages expose that surface.

## Next Steps

- Read [Inlet Concepts](../concepts/concepts.md).
- Use the [Spring Sample](../../samples/spring-sample/index.md) to see Inlet composition patterns in practice.
