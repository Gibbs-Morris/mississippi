---
title: Projection Replication Sinks
description: Understand how Domain Modeling projections publish latest-state external read models through named replica sinks and explicit contract identity.
sidebar_position: 2
---

# Projection Replication Sinks

Projection replication sinks let a Domain Modeling projection publish a separate external latest-state read model without making projection success depend on the external sink. In the shipped slice, bindings are discovered from attributes, resolved by named sink registrations, and limited to single-instance execution.

## The problem this solves

A projection often needs to materialize data outside Mississippi, for example into a search-oriented or query-oriented store. Doing that work inline with projection updates would couple projection correctness to external availability and configuration.

Replica sinks keep the projection as the source of truth while giving the runtime a separate latest-state delivery lane for the external copy. The model also lets one projection declare more than one sink and keeps external contract identity explicit instead of inferring it from CLR type names.

## Core idea

A replica-sink binding combines four pieces of metadata:

- a `sink` key that matches a named runtime/provider registration
- a provider-neutral `targetName`
- a contract identity from `ReplicaContractNameAttribute`
- a write mode from `ReplicaWriteMode`

Mapped replica contracts are the default path. A projection can point at a separate contract type through `ProjectionReplicationAttribute`, and the runtime resolves an `IMapper<TProjection, TContract>` for that binding.

Direct projection replication exists, but only as an explicit opt-in. When `ContractType` is omitted, the projection itself must declare `ReplicaContractNameAttribute` and set `IsDirectProjectionReplicationEnabled = true`.

## How it works

`AddReplicaSinks()` adds discovery, startup validation, runtime execution, and the bounded operator surface. `ScanReplicaSinkAssemblies(...)` then scans projection types decorated with `ProjectionReplicationAttribute`.

During startup, the runtime validates each discovered binding before traffic starts. It rejects missing sink registrations, missing contract identity, missing mappers, direct replication without explicit opt-in, unsupported `History` mode, ambiguous sink registrations, and overlapping physical targets.

At runtime, the current slice delivers `LatestState` only. The bounded operator surface exposes dead-letter paging and controlled re-drive. Re-drive does not replay a stored payload. Instead, it re-reads the current eligible source state for the delivery lane and queues fresh latest-state work.

## Guarantees

- Mapped replica contracts are the default programming model.
- Direct projection replication stays explicit rather than being inferred automatically.
- Stable external contract identity is derived from `ReplicaContractNameAttribute` as `AppName.ModuleName.Name.V{Version}`.
- Invalid bindings fail fast during startup instead of waiting for the first live delivery attempt.
- Dead-letter reads are bounded, and the runtime caps operator page size through `ReplicaSinkRuntimeOptions.MaxDeadLetterPageSize` (default `50`).
- Re-drive is fail-closed. If the runtime successfully queues a re-drive but cannot persist dead-letter clearing afterward, it quarantines the sink and keeps the dead-letter evidence instead of silently clearing it.

## Limits

- This slice is accepted only for single-instance deployment. It does not claim multi-node ownership, distributed blocking, or CAS/ETag fencing safety.
- `ReplicaWriteMode.History` exists in the public contract, but the runnable slice rejects it during startup validation.
- The documented provider evidence is bounded. The accepted Cosmos validation covers the emulator-backed calibration harness and targeted L0/L2 tests, not a full persistence conformance suite.
- Targeted mutation proof is not established for this feature slice. The accepted assurance is clean build, cleanup, unit-test success, and strengthened deterministic coverage, with mutation carried forward as blocked/not proven.

## Trade-offs

Replica sinks reduce coupling between projection success and external sink availability, but they add explicit registration, contract-identity, and operator-management work.

Fail-fast startup validation keeps runtime behavior predictable, but one invalid sink configuration can stop the host from starting.

Latest-state-only delivery keeps the first slice small and verifiable, but full history delivery and clustered safety are deferred follow-up work.

## Related tasks and reference

- [How to configure projection replication sinks](../how-to/configure-projection-replication-sinks.md)
- [Projection replication sinks reference](../reference/projection-replication-sinks.md)
- [Domain Modeling overview](../index.md)
- [ADR-0002 background](../../adr/0002-named-versioned-replica-sinks-use-durable-out-of-band-delivery.md)
