---
id: aqueduct-operations
title: Aqueduct Operations
sidebar_label: Operations
sidebar_position: 1
description: Change Aqueduct stream identities with a coordinated maintenance procedure and explicit recovery boundaries.
---

# Aqueduct Operations

## Overview

Changing an Aqueduct stream provider or stream namespace changes the routing identity used by the backplane. Treat
that change as a coordinated maintenance operation across every participating runtime and gateway host.

## When this matters

Use this page when changing `StreamProviderName`, `ServerStreamNamespace`, or `AllClientsStreamNamespace`, or when
recovering after a restart or failover that removed in-memory connections, groups, or server-directory state.

An API-only cutover that preserves the existing identities should follow the [Aqueduct Runtime Composition
Migration](../migration/migration.md) guidance. The maintenance procedure below is for an identity or provider change.

## Prerequisites and assumptions

- Inventory the exact provider and namespace values on every runtime and gateway that participates in the backplane.
- Identify all message producers, SignalR gateways, Orleans runtimes, and clients that must move together.
- Confirm the provider-specific procedure for preserving or recovering any durable stream or subscription metadata.
- Treat Aqueduct connection, group-membership, and server-directory state as volatile in-memory state. Aqueduct does not
  persist that state through `IGrainStorage`.
- Do not assume mixed runtime versions, mixed stream identities, a drain API, automatic replay, or zero-loss behavior;
  those behaviors are not established by the current implementation.

## Recommended baseline

Keep the provider name and both stream namespaces unchanged for the ordinary runtime API cutover. Use one matching
configuration set across all participating hosts and apply it through the documented runtime composition path.

If an identity must change, schedule a maintenance window that stops message production, new traffic, and all
participating gateways and runtimes before any host receives the new values. The whole participating backplane is the
blast radius: old and new identities address different streams, so a partially updated fleet can partition delivery.

## Procedure

1. Record the current provider and namespace values, deployment versions, provider-owned durable metadata locations,
   and the clients or subscriptions that must be re-established.
2. Stop application producers and stop accepting traffic that can create or use SignalR connections. Do not assume a
   drain operation exists; keep traffic stopped for the identity change.
3. Stop participating gateway hosts first, then stop all participating runtime hosts. Verify that no old host remains
   able to publish or consume backplane messages.
4. Apply one matching provider and namespace configuration set to every runtime and gateway deployment. Keep the
   provider-owned durable metadata configuration explicit and unchanged unless the provider migration is intentional.
5. Start all runtime hosts first. Wait for their normal Orleans health signal and verify that each runtime has the same
   provider and namespace values.
6. Start the gateway hosts. Allow each gateway to initialize its server and all-client stream subscriptions and register
   its heartbeat with the server directory.
7. Re-establish client connections, group membership, and application subscriptions. Treat all previous in-memory
   membership as lost until these flows complete.
8. Send controlled messages through the real connection, group, and broadcast paths. Verify receipt by the intended
   clients and confirm host health before resuming normal traffic.

## Validation

The change is ready for traffic only when all of the following are true:

- Every participating host reports the same provider and namespace configuration.
- Runtime hosts report their normal Orleans started/healthy state.
- Gateway logs show `Orleans streams initialized for hub`, `Heartbeat manager started`, and `Orleans backplane
  initialized` for each active hub/server; investigate any `Heartbeat failed` warning.
- Clients have reconnected, groups have been rejoined, and application subscriptions have been recreated.
- Controlled direct-connection, group, and broadcast messages traverse the intended paths and are observed by the
  intended clients.
- No `MSB201`, `MSB202`, `MSB203`, `MSB206`, or `MSB207` composition diagnostics are present.
- Provider-owned durable stream or subscription metadata has passed its provider-specific recovery check, when such
  metadata exists.

## Failure modes and rollback

Changing identities can make messages on the previous streams invisible to hosts using the new streams. Messages in
flight during shutdown can be lost, and Aqueduct does not provide automatic replay. Stopping hosts also loses the
in-memory client, group, and server-directory state; clients must reconnect, rejoin groups, and recreate subscriptions.
This volatile membership is separate from any durable metadata owned by an external stream provider.

If validation fails while traffic is still stopped, roll back as one coordinated unit:

1. Stop the gateways and runtimes using the new configuration.
2. Restore the previous binary, provider configuration, and exact provider/namespace identities on every participating
   host.
3. Start the previous runtime set first and verify its Orleans health and provider configuration.
4. Start the previous gateway set and verify stream initialization and server registration.
5. Reconnect clients, rejoin groups, recreate subscriptions, and repeat controlled direct, group, and broadcast message
   checks.
6. Resume traffic only after the previous configuration and real message paths are healthy.

Recover provider-owned durable metadata through that provider's documented procedure. Do not treat volatile Aqueduct
membership as recoverable storage, and do not resume traffic with old and new identity sets mixed.

## Telemetry to watch

Watch the host's Orleans health and stream-provider signals, plus Aqueduct's structured logs:

- `Orleans streams initialized for hub` confirms gateway stream subscriptions completed.
- `Heartbeat manager started` confirms gateway server registration began.
- `Orleans backplane initialized for hub` confirms the gateway completed backplane setup.
- `Heartbeat failed for server` is a warning requiring investigation before traffic resumes.
- Runtime and gateway health checks should remain healthy after clients reconnect and controlled messages succeed.

These signals show initialization and liveness activity. They do not prove zero message loss or durable membership
recovery.

## Summary

Keep stream identities stable for ordinary API-only changes. For an intentional provider or namespace change, stop the
whole participating fleet, apply one matching configuration, start runtimes before gateways, rebuild volatile state,
validate real message paths, and roll back the complete set while traffic remains stopped if validation fails.

## Next Steps

- Follow [Aqueduct Runtime Composition (Next)](../migration/migration.md) for the API cutover and identity-preservation
  constraints.
- Use [Aqueduct Reference](../reference/reference.md) for options and diagnostic codes.
- Read [Aqueduct Troubleshooting](../troubleshooting/troubleshooting.md) for composition and provider failures.
