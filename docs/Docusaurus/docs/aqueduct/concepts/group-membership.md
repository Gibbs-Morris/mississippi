---
title: SignalR Group Membership
description: Understand how Aqueduct tracks group membership and cleans it up when a connection disconnects.
sidebar_position: 2
---

# SignalR Group Membership

Aqueduct associates group membership with the client grain so a normal disconnect can remove groups joined through any gateway.

## The problem this solves

A connection can join several groups, including through a gateway other than its hosting gateway. Keeping membership only on the gateway that performed the join would leave the hosting gateway unable to clean up every group. Retained connection IDs also cause broadcasts to visit disconnected clients.

## Core idea

The client grain owns the connection's membership list. Group grains own the sets used for broadcasts. The hub lifetime manager routes joins, removals, and disconnects through the same client grain.

## How it works

The client grain records each group before requesting the remote join, retaining cleanup ownership even if that request reports an uncertain outcome. A successful explicit removal removes the group from its cleanup list.

On disconnect, the client grain stops delivery, attempts every tracked removal, and clears its connection state after cleanup succeeds. Its ordinary Orleans request scheduling serializes joins, removals, and disconnects. A join processed after disconnect is ignored.

Group membership updates can interleave with a broadcast waiting for a client grain. The built-in updates are synchronous, and broadcasts enumerate an immutable membership snapshot. This prevents cleanup and broadcasts from waiting on each other's grain calls.

## Guarantees

- Successful normal disconnect removes the memberships tracked through the hub lifetime manager, including joins initiated from other gateways.
- Removing one connection preserves other connections in its groups.
- Failed removals propagate to the caller. Other groups are still attempted, and unsuccessful removals remain available for retry while the client activation retains its state.

## Limits

- Membership is held in memory. This mechanism does not provide durable recovery after silo loss or replace detection of a gateway that disappears without disconnect callbacks.
- Cleanup does not retract messages already being broadcast from a membership snapshot.
- Failed disconnect cleanup is not retried automatically. A subsequent disconnect request can retry it while the activation remains available.
- Low-level direct calls to group-grain membership methods bypass client ownership. Application group operations should use the SignalR group manager.
- These changes do not add cancellation to grain membership operations.

## Trade-offs

Routing membership through a client grain adds a grain call, but gives all gateways one cleanup owner. Custom `ISignalRClientGrain` implementations must implement `AddToGroupAsync` and `RemoveFromGroupAsync` with the same lifecycle behavior. Custom group grains must support the membership methods' interleaving contract.

## Related tasks and reference

- [Aqueduct Concepts](concepts.md)
- [Aqueduct Reference](../reference/reference.md)
- [Aqueduct Operations](../operations/operations.md)
