---
id: aqueduct-concepts
title: Aqueduct Concepts
sidebar_label: Concepts
sidebar_position: 1
description: Understand Aqueduct's nested runtime builder, composition lifecycle, and SignalR backplane boundary.
---

# Aqueduct Concepts

## Overview

Aqueduct's runtime integration is a nested composition scope inside Mississippi's Orleans `RuntimeBuilder`. This
page explains that boundary and the lifecycle rules that determine when Aqueduct configuration is accepted.

## The problem this solves

Aqueduct exists for the case where Orleans-driven events and real-time delivery must work across multiple hosts without forcing application code to manage SignalR backplane mechanics directly.

## Core idea

Aqueduct separates distributed SignalR routing concerns into a dedicated backplane layer with clear gateway and runtime
package boundaries. Runtime hosts opt in with `runtime.AddAqueduct(...)` inside the one canonical
`UseMississippi(...)` terminal callback.

## How it works

The runtime composition has two levels:

1. `UseMississippi(...)` creates and validates the staged `RuntimeBuilder` for the Orleans host.
2. `runtime.AddAqueduct(...)` queues Aqueduct's native registration callback.
3. The callback creates an `AqueductBuilder`, applies the nested settings to the staged silo, snapshots the values into
   `AqueductOptions`, and closes the nested scope.
4. The runtime terminal callback applies queued native configuration and publishes the staged service graph.

The nested builder does not expose a second service collection. Use `runtime.Services` or `runtime.ConfigureSilo(...)`
for advanced runtime composition, as described in [Runtime Composition](../../reference/runtime-composition.md).

Aqueduct is infrastructure. It is not the domain layer, the client state layer, or the source-generation layer.

Within Mississippi, Inlet can compose with Aqueduct for real-time projection delivery, but Aqueduct remains the
underlying backplane concern.

## Guarantees

- A runtime host can attach Aqueduct once through `runtime.AddAqueduct(...)`.
- The default stream provider name and stream namespaces are copied into the runtime's `AqueductOptions` at
  composition time; later changes to the captured nested builder cannot change the registered snapshot.
- The builder rejects empty or whitespace-only stream names and nonpositive timing values before terminal attachment.
- `UseMemoryStreams(...)` uses the final selected provider name and registers the Orleans `PubSubStore` convention for
  local development or tests.

## Non-guarantees

- `UseMississippi(...)` does not build the service provider, start the silo, or verify network connectivity.
- Aqueduct does not create an external production stream provider. The host must configure that provider and select the
  matching `StreamProviderName`.
- Staging does not roll back mutations to shared configuration objects, existing service instances, or external
  callback side effects.
- Memory stream registration is a development/test facility; this page does not promise persistence or production
  durability for that path.

## Trade-offs

Builder-first composition centralizes validation and makes the runtime host's attachment boundary explicit. It also means
that settings must be supplied during the synchronous terminal callback. Code that needs asynchronous initialization
belongs in hosted services or Orleans lifecycle participants.

The runtime and gateway package boundaries remain separate. A gateway can register a SignalR hub lifetime manager with
its gateway package, while the Orleans host composes the runtime backplane with `runtime.AddAqueduct(...)`. End-to-end
projection delivery additionally involves Inlet.

This page describes the Aqueduct runtime layer. Gateway security intent and any typed domain builders belong to their
host or domain documentation.

## Related tasks and reference

Use the runtime composition reference for terminal attachment and native Orleans integration. Use the group-membership
concept page for connection cleanup behavior.

## Summary

Think of Aqueduct as a validated, nested runtime registration for the distributed real-time transport boundary in
Mississippi. Its configuration is staged, snapshotted, and closed with the surrounding runtime composition.

## Next Steps

- [SignalR Group Membership](group-membership.md)
- [Aqueduct Reference](../reference/reference.md)
- [Aqueduct Runtime Getting Started](../getting-started/getting-started.md)
- [How To Configure Aqueduct Runtime Composition](../how-to/how-to.md)
- [Inlet Overview](../../inlet/index.md)
