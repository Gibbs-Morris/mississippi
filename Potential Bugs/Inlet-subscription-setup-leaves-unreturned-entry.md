# Failed subscription setup keeps an ID that the caller never receives

## Source location

- Project: `Inlet.Runtime`.
- Source file: [src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs lines 260-275](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs#L260-L275).
- Type: `InletSubscriptionGrain`.
- Member: `SubscribeAsync / SubscribeToBrookStreamAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The grain records a new subscription in both indexes before awaiting its initial cursor read and stream setup. If either step fails, it keeps the records but never returns their ID to the caller.

## Trigger

A valid connection subscribes to a registered projection while ReadCursorPositionAsync or stream.SubscribeAsync fails transiently, then retries the same projection on the same connection after the dependency recovers.

## Potential impact

A successful retry creates another entry. Later updates can notify both entries, while unsubscribing the known ID leaves the failed request's unknown ID behind until connection cleanup.

## Evidence

- [src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs lines 249-275](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs#L249-L275): Creates a fresh GUID and records both indexes before the awaited setup; no rollback/finally surrounds the operation.
- [src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs lines 307-322](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs#L307-L322): Initial cursor reading and stream.SubscribeAsync are awaited dependencies which can fail before a stream handle is recorded.
- [src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs lines 195-213](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs#L195-L213), [src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs lines 279-299](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs#L279-L299): Notifications enumerate every retained ID; normal unsubscribe requires that ID and removes just that entry.
- [src/Inlet.Gateway/InletHub.cs lines 131-144](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway/InletHub.cs#L131-L144): The hub only returns the subscription ID after the grain task completes.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Inject one pre-handle setup failure, then let a retry succeed; inspect the two retained entries and notification count before disconnecting.
- This concerns transactional setup state, not best-effort notification delivery or intentionally swallowed unsubscribe errors. No production or test changes or fresh runtime probe were made.

## Confidence

**High**. The record-before-await order and absence of rollback give a deterministic stale entry when either dependency fails.
