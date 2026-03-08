---
title: Inlet Reference
sidebar_position: 1
description: Current reference surface for Inlet packages and cross-layer ownership.
---

# Inlet Reference

## Summary

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

The active docs currently verify the subsystem boundary and representative packages. They do not yet publish a rebuilt generated-surface reference or a full generator input and output guide.

## Failure Behavior

Detailed generator, runtime registration, and API failure behavior is not yet documented in the active Inlet section.

## Summary

Use this page as the current active reference for what Inlet owns and which packages expose that surface.

## Next Steps

- Read [Inlet Concepts](../concepts/concepts.md).
- Use [Spring Sample](../../samples/spring-sample/index.md) for a verified sample entry point while deeper active pages are rebuilt.
