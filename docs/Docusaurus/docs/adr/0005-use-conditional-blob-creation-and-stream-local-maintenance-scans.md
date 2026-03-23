---
title: "ADR-0005: Use Conditional Blob Creation and Stream-Local Maintenance Scans"
description: Prevent silent overwrite with conditional blob creation and constrain latest, prune, and delete-all discovery to stream-local prefix scans.
sidebar_position: 5
status: "proposed"
date: 2026-03-23
decision_makers:
  - cs ADR Keeper
consulted:
  - Cloud architecture reviewer
  - Three Amigos synthesis
informed:
  - Tributary maintainers
  - Operations maintainers
---

# ADR-0005: Use Conditional Blob Creation and Stream-Local Maintenance Scans

## Context and Problem Statement

Azure Blob uploads overwrite existing blobs by default, but the snapshot provider derives blob names deterministically from stream identity and version. The architecture also relies on prefix listing for prune, delete-all, and any stream-local discovery, which means the design must define both concurrency correctness and the accepted scaling boundary for maintenance operations.

## Decision Drivers

- Duplicate snapshot versions must not silently overwrite existing persisted data.
- Maintenance operations must remain scoped to a single logical stream.
- The architecture must reflect Azure Blob listing behavior honestly rather than treating prefix listing as an index.
- Delete semantics must stay correct even when storage-account features such as soft delete or blob versioning are enabled.

## Considered Options

- Use `If-None-Match = *` for writes and accept stream-local linear prefix scans for latest, prune, and delete-all.
- Allow default blob overwrite behavior and rely on deterministic names alone.
- Introduce a manifest or pointer blob in v1 to avoid linear prefix scans.

## Decision Outcome

Chosen option: "Use `If-None-Match = *` for writes and accept stream-local linear prefix scans for latest, prune, and delete-all", because it closes the Azure overwrite-correctness gap immediately while keeping the v1 design simple and explicit about the cost of prefix-based maintenance.

### Consequences

- Good, because duplicate-version writes become explicit conflicts instead of silent replacement.
- Good, because maintenance scans stay limited to one stream prefix rather than broader container enumeration.
- Good, because the design keeps the door open for a later manifest or pointer optimization if stream histories grow.
- Bad, because latest-by-stream, prune, and delete-all remain linear in snapshots per stream.
- Bad, because provider-level delete only guarantees deletion of the current named blob and cannot promise irreversible purge when account soft delete or blob versioning is enabled.

### Confirmation

Compliance will be confirmed by tests that surface precondition failures as duplicate-version conflicts, prove maintenance listings are scoped to one stream prefix, and verify that partial prefix-enumeration failures abort delete-all or prune instead of continuing with incomplete state.

## Pros and Cons of the Options

### Use `If-None-Match = *` for writes and accept stream-local linear prefix scans for latest, prune, and delete-all

Use Azure access conditions to enforce create-only writes and constrain discovery to the hashed stream prefix.

- Good, because correctness under concurrent or repeated writes does not depend on callers behaving perfectly.
- Good, because the design is operationally honest about prefix scan cost.
- Neutral, because later designs can add a manifest without invalidating the stored blob frame.
- Bad, because maintenance cost grows with the number of snapshots in a stream.

### Allow default blob overwrite behavior and rely on deterministic names alone

Assume naming uniqueness is sufficient and let Azure overwrite existing blobs on repeated writes.

- Good, because the write path is simpler.
- Bad, because duplicate or racing writes can silently replace existing persisted snapshots.
- Bad, because overwrite behavior diverges from the design's correctness expectations.

### Introduce a manifest or pointer blob in v1 to avoid linear prefix scans

Add extra persisted indexing state in the first version of the provider.

- Good, because latest lookup and some maintenance operations can become cheaper.
- Bad, because v1 gains more moving parts, additional consistency rules, and another persisted contract to own.
- Bad, because the requested first release explicitly favors correctness and clarity over Blob-native optimization.

## More Information

- [Architecture Decision Records](index.md)
