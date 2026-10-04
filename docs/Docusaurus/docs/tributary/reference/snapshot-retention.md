---
id: tributary-snapshot-retention
title: Tributary Snapshot Retention Reference
sidebar_label: Snapshot Retention
sidebar_position: 2
description: Reference snapshot retention attributes, configuration precedence, checkpoint positions, and persistence behavior in Tributary.
---

# Tributary Snapshot Retention Reference

## Overview

This page documents how Tributary selects snapshot checkpoints for state reconstruction and persistence. Retention is a persistence interval; it is not time-to-live (TTL), deletion, or event-stream pruning.

## Applies to

- `Mississippi.Tributary.Abstractions`
- `Mississippi.Tributary.Runtime`
- Mississippi hosts that register snapshot caching

## Configuration

Register snapshot caching with one of the supported overloads:

```csharp
builder.Services.AddSnapshotCaching(
    builder.Configuration.GetSection("Mississippi:SnapshotCaching"));
```

The registration also supports a parameterless call, an `Action<SnapshotRetentionOptions>` callback, and an explicit `defaultRetainModulus` with an optional `shouldPersistAllSnapshots` flag.

The configuration section uses these option names:

```json
{
  "Mississippi": {
    "SnapshotCaching": {
      "DefaultRetainModulus": 50,
      "ShouldPersistAllSnapshots": false,
      "StateTypeOverrides": {
        "SPRING.BANKING.ACCOUNTLEDGER.V1": 100
      }
    }
  }
}
```

Configuration is read through the options pattern and is fixed for the host lifetime. Restart the host after changing the section.

## Options

| Option | Default | Meaning |
| --- | --- | --- |
| `DefaultRetainModulus` | `50` | Fallback interval for state types without an override or retention attribute. |
| `ShouldPersistAllSnapshots` | `false` | Persists every reconstructed snapshot when enabled. It does not change the replay interval. |
| `StateTypeOverrides` | Empty | Positive intervals keyed by stable snapshot storage name or CLR type name. |

## Attribute

Apply `SnapshotRetentionAttribute` to a state class when that state needs an interval different from the global default:

```csharp
[SnapshotStorageName("SPRING", "BANKING", "ACCOUNTSTATE")]
[SnapshotRetention(20)]
public sealed record BankAccountAggregate
{
    // State members omitted.
}
```

The constructor accepts one positive `int` modulus. The attribute targets classes, cannot be applied more than once, and is not inherited. A state type should also carry `SnapshotStorageNameAttribute` so its persisted identity remains stable.

## Precedence

Tributary resolves one effective interval for each state type in this order:

1. `StateTypeOverrides` keyed by the stable `SnapshotStorageNameAttribute.StorageName`.
2. `StateTypeOverrides` keyed by the state type's `Type.FullName`.
3. `SnapshotRetentionAttribute.Modulus`.
4. `DefaultRetainModulus`.

Retention and snapshot-storage metadata is discovered once per CLR type and reused for later requests.

## Checkpoint behavior

Brook positions are zero-based: position `0` contains the first event. With modulus `50`, eligible checkpoint positions are `0`, `50`, `100`, and so on.

When a state is reconstructed, persistence eligibility is:

```text
ShouldPersistAllSnapshots || version % effectiveModulus == 0
```

Tributary evaluates eligibility before serializing the intermediate envelope or resolving its persister. The state remains available in its exact immutable in-memory version even when persistence is skipped.

An exact snapshot lookup happens before reconstruction. A valid exact snapshot is loaded and is not rewritten. If reconstruction is required for a target greater than `0`, the nearest checkpoint is strictly earlier than the target; checkpoint `0` is a valid base for later versions. Target version `0` reconstructs from the initial state and the first event without calling itself as a base.

The save-all switch is demand-driven. It persists reconstructed versions requested by the application; it does not create unused projection snapshots or backfill every historical version. Changing the interval does not delete or rewrite existing snapshot documents.

## Recovery and diagnostics

The event stream remains the source of truth when a checkpoint is missing or its reducer hash is empty or incompatible. A valid persisted checkpoint with a matching reducer hash can be reused after a fresh activation. Snapshot persistence remains a background one-way operation, so a command does not wait for the storage write to finish.

The runtime records persistence requests, retention skips, successful writes, failed writes, and replay-event counts through the existing snapshot meter. Startup logging records the configured default, override count, and save-all setting; enabling save-all emits one warning. Each activated state type logs its effective interval.

Invalid defaults and override values fail options validation before host startup. Retention attributes reject nonpositive moduli, and registered snapshot types surface invalid retention metadata when their metadata is inspected.

## Spring sample

The Spring sample demonstrates three state-type policies:

- `BankAccountAggregate` uses `[SnapshotRetention(20)]`.
- `BankAccountBalanceProjection` has no retention attribute and therefore uses the configured default `50`.
- `BankAccountLedgerProjection` uses `[SnapshotRetention(100)]`.

Spring binds `Mississippi:SnapshotCaching` in `Spring.Runtime` and keeps save-all disabled by default. A configuration override for a stable snapshot storage name takes precedence over either sample attribute.

## Compatibility

Retention changes do not change snapshot document IDs, partition keys, stable snapshot storage names, reducer hashes, or event durability. Existing snapshots remain readable. This is a pre-1.0 default-behavior change: applications that need the previous write frequency should set an explicit interval.

Retention does not provide automatic pruning, TTL, delayed-latest scheduling, event batching, notification coalescing, or aggregate cache redesign.

## Summary

Use a positive `SnapshotRetentionAttribute` or a positive configuration override to choose a state-specific checkpoint interval. Otherwise, Tributary uses `DefaultRetainModulus = 50`. Use `ShouldPersistAllSnapshots` only when every requested reconstruction should be persisted.

## Next Steps

- [Tributary Reference](./reference.md)
- [Tributary Cosmos DB Provider](../storage-providers/cosmos.md)
- [Building an Aggregate: BankAccount](../../samples/spring-sample/tutorials/building-an-aggregate.md)
- [Building Projections: Read-Optimized Views](../../samples/spring-sample/tutorials/building-projections.md)
