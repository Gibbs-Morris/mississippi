---
title: Cosmos Pending Append Recovery
description: Describe how Cosmos brook recovery reconciles visible pending writes under the writer lease.
sidebar_position: 3
---

# Cosmos Pending Append Recovery

Cosmos brook recovery checks both the committed cursor and pending append metadata while holding the same brook lease used by writers. An existing committed cursor does not hide a later pending append.

## Recovery outcomes

| Evidence | Recovery action |
| --- | --- |
| No pending append | Return the stored committed cursor, or the unset position when no cursor exists. |
| The committed cursor already includes the pending range | Remove stale pending metadata without deleting committed events. |
| Every event in the pending range exists | Commit the pending target cursor. |
| The pending range is incomplete | Delete the incomplete range and pending metadata, preserving earlier committed history. |
| The lease, storage evidence, or cursor relationship cannot be established | Fail the recovery attempt; do not substitute a cached cursor or infer missing events from a read exception. |

Writers pass their already acquired lease into the internal recovery operation. Recovery does not acquire or dispose that lease again.

## Limits

This operation reconciles visible pending storage work. It does not fence a queued append that has not acquired its lease, and a Blob lease is not a Cosmos-enforced fencing token after expiry. An empty read alone cannot prove that a delayed write will never commit. Recovery-sensitive callers retain protection while the outcome remains unknown.

Reads still depend on the configured Cosmos consistency model. This change does not strengthen account consistency or claim that weakly consistent reads become linearizable through a Blob lease.

## Related concepts

- [Write Model](../concepts/write-model.md)
- [Sagas and Orchestration](../concepts/sagas-and-orchestration.md)
