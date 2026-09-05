---
title: Brook Append Outcomes
description: Distinguish committed event appends from uncertain outcomes and retry cursor publication without appending again.
sidebar_position: 2
---

# Brook Append Outcomes

`IBrookWriterGrain` separates durable event append from cursor-update publication. A publication failure can occur after the events have committed.

This reference covers `Mississippi.Brooks.Abstractions.Writer`, the low-level writer contract used by the aggregate runtime. Application commands continue to use the [Write Model](../concepts/write-model.md).

## AppendEventsAsync

`AppendEventsAsync(events, expectedCursorPosition, cancellationToken)` appends events and then publishes the resulting cursor position.

| Observed outcome | Meaning | Caller responsibility |
| --- | --- | --- |
| Returns a `BrookPosition` | Storage append and the publication call completed. | Continue from the returned position. |
| Throws `BrookCursorPublicationException` with a supplied `Position` | The writer committed the events at that position, but publication did not complete successfully. | Retry publication for that position; do not append the events again. |
| Times out or throws another exception | The exception alone does not establish whether the append committed. | Confirm the outcome through authoritative storage recovery before deciding what work remains. |

The optional expected cursor is an optimistic concurrency precondition. Omitting it does not make retries idempotent.

## BrookCursorPublicationException

`Position` identifies the committed cursor when the writer supplies it. `InnerException` describes the publication failure. General-purpose constructors which do not receive a position leave `Position` unset (`-1`); that value is not evidence of a committed append and cannot be published.

A lost response can hide this exception from the caller. Do not classify every timeout as either a failed append or a known publication failure.

## PublishCursorAsync

`PublishCursorAsync(position, cancellationToken)` publishes a cursor update without appending events. The caller supplies a position confirmed by a successful append, a writer-issued publication exception, or authoritative storage recovery for the same brook.

The method rejects negative positions and checks cancellation before publication. It does not validate the supplied position against storage. Never invent a position, substitute an expected future position, or treat an unconfirmed cached position as proof of commitment.

Repeating publication can repeat the cursor notification. A successful publication call does not establish that every projection or client has caught up.

## Limits

These contracts do not make arbitrary external side effects execute exactly once. They do not dispatch aggregate effects, decide saga recovery policy, or prove that a timed-out request has stopped running.

An empty storage read does not by itself prevent a delayed append from committing later. Preserve recovery protection while that outcome remains uncertain.

## Related reference

- [Write Model](../concepts/write-model.md)
- [Read Models and Client Sync](../concepts/read-models-and-client-sync.md)
