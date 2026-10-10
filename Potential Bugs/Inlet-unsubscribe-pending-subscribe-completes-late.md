# Late subscription reply restores an interest that was already released

## Source location

- Project: `Inlet.Client`.
- Source file: [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 345-355](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L345-L355).
- Type: `InletSignalRActionEffect`.
- Member: `HandleSubscribeAsync / HandleUnsubscribeAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

A pending subscription has no map entry. Unsubscribe treats that missing entry as already released and returns. The earlier hub reply can then arrive and install the subscription without checking the intervening release.

## Trigger

A single owner starts subscribing to entity A and switches to entity B before A's hub invocation completes, dispatching unsubscribe for A during the pending operation. A remains on the same transport connection.

## Potential impact

The released entity can keep receiving notifications and triggering fetches on the same connection until another explicit unsubscribe or disconnect.

## Evidence

- [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 271-297](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L271-L297): The first map entry for a new interest is created after the awaited hub response.
- [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 345-355](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L345-L355): Missing map entry is treated as an already-unsubscribed interest, with no pending state recorded.
- [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 402-422](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L402-L422): The late map entry qualifies subsequent notifications for HTTP fetch and store updates.
- [src/Reservoir.Core/Store.cs lines 296-300](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Reservoir.Core/Store.cs#L296-L300): Asynchronous subscription work does not block a later unsubscribe dispatch.
- [docs/Docusaurus/docs/inlet/how-to/subscribe-to-projections.md lines 109-130](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/docs/Docusaurus/docs/inlet/how-to/subscribe-to-projections.md#L109-L130), [docs/Docusaurus/docs/inlet/how-to/subscribe-to-projections.md lines 137-152](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/docs/Docusaurus/docs/inlet/how-to/subscribe-to-projections.md#L137-L152): The supported single-owner entity-switch pattern releases the former entity before subscribing to the new entity.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Delay A's hub response; dispatch A unsubscribe, then complete A subscribe. Check that the returned ID is either released or never becomes active.
- This does not require multiple owners and does not claim page disposal automatically unsubscribes. The application must issue its own release action.

## Confidence

**High**. The absent pending record makes the release a no-op; the late assignment is unconditional.
