---
title: Cosmos Pending Append Recovery
description: Describe how Cosmos brook recovery reconciles visible pending writes under the writer lease.
sidebar_position: 3
---

# Cosmos Pending Append Recovery

Cosmos brook recovery first checks for pending append metadata. When pending work is visible, recovery acquires the writer's brook lease and rereads both the committed cursor and pending metadata before reconciling them. An existing committed cursor does not hide a visible later pending append.

## Recovery outcomes

| Evidence | Recovery action |
| --- | --- |
| No pending append is visible | Return the stored committed cursor, or the unset position when no cursor exists, without allocating a writer lease. |
| The committed cursor already includes the pending range | Remove stale pending metadata without deleting committed events. |
| Every event in the pending range exists | Commit the pending target cursor. |
| The pending range is incomplete | Retain events and pending metadata, and fail with an unresolved-outcome error so later recovery can retry. |
| The lease, storage evidence, or cursor relationship cannot be established | Fail the recovery attempt; do not substitute a cached cursor or infer missing events from a read exception. |

Writers pass their already acquired lease into the internal recovery operation, which always reads both cursor documents under that lease. Recovery does not acquire or dispose that lease again. Read-only lookups for streams without visible pending work do not create lock blobs.

## Limits

This operation reconciles visible pending storage work. It does not fence a queued append that has not acquired its lease, and a Blob lease is not a Cosmos-enforced fencing token after expiry. A missing event cannot prove that a delayed write will never commit, so recovery preserves incomplete ranges and their pending metadata. Recovery-sensitive callers retain protection while the outcome remains unknown. An incomplete range can remain blocked indefinitely; this operation does not infer safe abandonment from elapsed time.

Reads still depend on the configured Cosmos consistency model. This change does not strengthen account consistency or claim that weakly consistent reads become linearizable through a Blob lease.

The no-pending result is a read snapshot, not proof that no queued or delayed append exists. It does not justify discarding protection for an unknown append. Hosts still own subscription authorization and request quotas.

## Related concepts

- [Write Model](../concepts/write-model.md)
- [Sagas and Orchestration](../concepts/sagas-and-orchestration.md)
