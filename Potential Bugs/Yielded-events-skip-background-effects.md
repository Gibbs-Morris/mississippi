# Events produced by synchronous effects skip their background effects

## Source location

- Project: `DomainModeling.Runtime`.
- Source file: [src/DomainModeling.Runtime/GenericAggregateGrain.cs lines 445-468](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/GenericAggregateGrain.cs#L445-L468).
- Type: `GenericAggregateGrain<TAggregate>`.
- Member: `DispatchEffectsAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The aggregate saves events produced by synchronous effects and includes them in later synchronous processing. It then starts background effects using only the command's original event list. A background effect registered for one of the additional saved events is never offered that event.

## Trigger

A command produces event A. A synchronous effect for A produces event B, and the application has registered a background effect for B.

## Potential impact

Event B appears in saved history and state, but its configured background notification or integration does not run. This concerns the missing attempt to run that effect; it does not assume guaranteed delivery or exactly-once execution.

## Evidence

- [src/DomainModeling.Runtime/GenericAggregateGrain.cs lines 424-468](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/GenericAggregateGrain.cs#L424-L468): Initial events form the pending synchronous list. Yielded events are appended at 445-451, tracked at 454, reduced via a new snapshot at 457-463, and reused only as pendingEvents for synchronous dispatch at 468.
- [src/DomainModeling.Runtime/GenericAggregateGrain.cs lines 680-700](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/GenericAggregateGrain.cs#L680-L700): PersistEventsAndDispatchEffectsAsync passes its original events argument to synchronous dispatch and then passes the same original list to DispatchFireAndForgetEffectsIfRegisteredAsync. The yielded list is not returned or added.
- [src/DomainModeling.Runtime/GenericAggregateGrain.cs lines 531-546](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/GenericAggregateGrain.cs#L531-L546): The background wrapper obtains the latest state but forwards only its supplied events list to DispatchFireAndForgetEffects.
- [src/DomainModeling.Runtime/GenericAggregateGrain.cs lines 506-517](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/GenericAggregateGrain.cs#L506-L517): The only background dispatch loop selects registrations matching each event in that supplied list; no stream-based dispatch is used here.
- [src/DomainModeling.Abstractions/IEventEffect.cs lines 12-19](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Abstractions/IEventEffect.cs#L12-L19): Synchronous effects are allowed to yield additional events, and the public contract explicitly says those events are persisted immediately.
- [src/DomainModeling.Abstractions/IFireAndForgetEventEffect.cs lines 8-24](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Abstractions/IFireAndForgetEventEffect.cs#L8-L24): The background interface describes side effects triggered by domain events and executed in a separate worker context.
- [src/DomainModeling.Abstractions/IFireAndForgetEventEffect.cs lines 35-48](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Abstractions/IFireAndForgetEventEffect.cs#L35-L48): HandleAsync receives the persisted event, aggregate snapshot, brook key and event position; the contract does not exclude effect-generated events.

## Verification notes

Static inspection of complete assigned files and supporting contracts. No production/test files changed, and no runtime reproduction or tests executed.

- Register a synchronous effect that yields B and a background registration for B, execute a command that emits A, and compare persisted B with background Dispatch invocations.
- The finding assumes B has a valid event registration and reducer so conversion/snapshot reconstruction succeeds; it does not rely on missing registration or a throwing synchronous effect.
- The current path does not have an alternate automatic background event subscriber; verify any future alternate dispatcher before remediation.

## Confidence

**High**. The yielded events and the sole background dispatcher use separate lists, and the original list is never expanded.
