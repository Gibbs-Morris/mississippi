# Aggregate keeps old state when a saved write reports a notification failure

## Source location

- Project: `DomainModeling.Runtime`.
- Source file: [src/DomainModeling.Runtime/GenericAggregateGrain.cs lines 690-693](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/GenericAggregateGrain.cs#L690-L693).
- Type: `GenericAggregateGrain<TAggregate>`.
- Member: `PersistEventsAndDispatchEffectsAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The writer can save events successfully and then throw BrookCursorPublicationException because it could not announce the new position. The aggregate updates its remembered position only when the whole call succeeds. It therefore keeps using the position from before the saved write. The same pattern appears when an effect produces another event.

## Trigger

An active aggregate is at position N. A command saves events at N+1, but publishing the new position fails. The caller then reads state or runs another command on the same active aggregate.

## Potential impact

State reads can keep returning version N until the aggregate is recreated. Later writes can fail because their expected position is too old. For a new stream whose remembered position is NotSet, a retry can append a duplicate creation event if its handler does not prevent duplicates. The saved events remain in storage.

## Evidence

- [src/DomainModeling.Runtime/GenericAggregateGrain.cs lines 602-631](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/GenericAggregateGrain.cs#L602-L631): Command execution obtains the cached current position, loads that version's state, runs the handler, and persists its events. There is no cache invalidation or deactivation on an append exception.
- [src/DomainModeling.Runtime/GenericAggregateGrain.cs lines 668-700](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/GenericAggregateGrain.cs#L668-L700): GetCurrentPositionAsync returns lastKnownPosition whenever present. PersistEventsAndDispatchEffectsAsync supplies null for a cached NotSet cursor and assigns the new cached position only after AppendEventsAsync completes.
- [src/DomainModeling.Runtime/GenericAggregateGrain.cs lines 224-247](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/GenericAggregateGrain.cs#L224-L247): GetStateAsync also prefers lastKnownPosition and returns default immediately for cached NotSet, or requests a snapshot at the cached historical position.
- [src/DomainModeling.Runtime/GenericAggregateGrain.cs lines 445-454](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/GenericAggregateGrain.cs#L445-L454): Synchronous-effect yielded events also advance the cached position only after awaiting the writer. The encompassing dispatcher catches ordinary exceptions at lines 573-580, so that path can hide a committed append while leaving the same stale field.
- [src/Brooks.Runtime/Writer/BrookWriterGrain.cs lines 82-101](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime/Writer/BrookWriterGrain.cs#L82-L101): The writer first awaits durable storage append, then publishes the cursor. Any ordinary publication failure is wrapped in BrookCursorPublicationException with the committed newPosition.
- [src/Brooks.Abstractions/Writer/BrookCursorPublicationException.cs lines 8-13](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Abstractions/Writer/BrookCursorPublicationException.cs#L8-L13): The public contract says the exception reports a publication failure after events were durably appended and permits republishing without appending again.
- [docs/Docusaurus/docs/reference/brook-append-outcomes.md lines 17-23](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/docs/Docusaurus/docs/reference/brook-append-outcomes.md#L17-L23): The outcome table says the position-bearing exception represents committed events and callers must retry publication rather than append. It also states that omission of the expected cursor does not make retries idempotent.

## Verification notes

Static inspection of complete assigned files and supporting contracts. No production/test files changed, and no runtime reproduction or tests executed.

- Exercise a position-bearing BrookCursorPublicationException from a mocked or fault-injected writer, then query and execute another command on the same aggregate activation.
- The finding assumes the ordinary Orleans activation remains available after a method exception; this class contains no DeactivateOnIdle or append-error invalidation.
- Saga reminder recovery has a separate confirmed-position assignment at GenericAggregateGrain.cs:314; restrict the basic reproduction to a non-saga aggregate so that recovery cannot mask the stale cache.
- Duplicate creation is a conditional risk for non-idempotent handlers when the prior cached position was NotSet, not a claim that every retry duplicates events.

## Confidence

**High**. The committed-error writer contract and both update-after-await paths are explicit, and subsequent reads directly reuse the unchanged field.
