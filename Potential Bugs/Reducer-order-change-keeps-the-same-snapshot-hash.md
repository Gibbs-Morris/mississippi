# Changing reducer priority keeps the same snapshot version hash

## Source location

- Project: `Tributary.Runtime`.
- Source file: [src/Tributary.Runtime/RootReducer.cs lines 96-101](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Tributary.Runtime/RootReducer.cs#L96-L101).
- Type: `RootReducer<TProjection>`.
- Member: `ComputeReducerHash`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The snapshot version hash sorts reducer type names, so it ignores their registration order. The reducer pipeline uses the first matching reducer. Reordering two different reducers for the same event can therefore change freshly calculated state while leaving the hash unchanged.

## Trigger

An application has two typed reducers that can both handle an event but produce different state. It saves a snapshot with registrations A then B, then deploys the same two types registered B then A and reads that snapshot.

## Potential impact

The new application can accept state calculated with the old reducer priority instead of rebuilding it. A fresh replay and a saved-snapshot read can then produce different state for the same event history.

## Evidence

- [src/Tributary.Runtime/RootReducer.cs lines 68-79](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Tributary.Runtime/RootReducer.cs#L68-L79), [src/Tributary.Runtime/RootReducer.cs lines 194-204](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Tributary.Runtime/RootReducer.cs#L194-L204): The typed index retains registration order, and the first successful TryReduce supplies the result.
- [src/Tributary.Runtime/RootReducer.cs lines 96-101](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Tributary.Runtime/RootReducer.cs#L96-L101): Sorting type names makes the hash equal for both registration orders.
- [src/Tributary.Abstractions/IRootReducer.cs lines 10-14](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Tributary.Abstractions/IRootReducer.cs#L10-L14): The hash contract says it identifies reducer logic changes so projections can be rebuilt.
- [src/Tributary.Abstractions/SnapshotEnvelope.cs lines 37-44](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Tributary.Abstractions/SnapshotEnvelope.cs#L37-L44): A mismatched reducer hash marks a stored snapshot as stale.
- [src/Tributary.Runtime/SnapshotCacheGrain.cs lines 152-167](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Tributary.Runtime/SnapshotCacheGrain.cs#L152-L167): The cache accepts the saved state directly when the current and stored hashes match.

## Verification notes

Full source inspection and the existing snapshot invalidation contract. No snapshot integration reproduction was run.

- Construct roots from the same two typed reducer instances in opposite orders. Check that GetReducerHash is equal and Reduce produces different results.
- Save a snapshot with the first order, load it with the second order, and compare its accepted state with a fresh replay.
- Use distinct reducer classes and valid new state instances. This finding concerns ordering; it does not assume that every method-body change must automatically be fingerprinted.

## Confidence

**High**. The hash removes the exact order that determines first-match behaviour, and the cache uses hash equality to accept saved state.
