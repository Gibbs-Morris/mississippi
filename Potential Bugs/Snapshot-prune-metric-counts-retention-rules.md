# Snapshot pruning metric counts retention rules instead of operations

## Source location

- Project: `Tributary.Runtime.Storage.Cosmos`.
- Source file: [src/Tributary.Runtime.Storage.Cosmos/SnapshotStorageProvider.cs lines 70-73](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Tributary.Runtime.Storage.Cosmos/SnapshotStorageProvider.cs#L70-L73).
- Type: `SnapshotStorageProvider`.
- Member: `PruneAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

After one successful prune call, the provider records the number of retention rules supplied. The counter is described as the number of prune operations. Two rules therefore record two operations, while an empty rule list records none even though pruning ran.

## Trigger

Call PruneAsync successfully with two or more retention moduli, or with an empty list while snapshots can be pruned.

## Potential impact

Monitoring reports an incorrect operation count. Changing a retention policy can change the apparent pruning rate even when the number of calls is unchanged. This finding concerns the metric; it does not show incorrect snapshot deletion.

## Evidence

- [src/Tributary.Runtime.Storage.Cosmos/SnapshotStorageProvider.cs lines 64-73](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Tributary.Runtime.Storage.Cosmos/SnapshotStorageProvider.cs#L64-L73): Repository.PruneAsync is awaited once, then retainModuli.Count is passed to RecordPrune.
- [src/Tributary.Runtime.Storage.Cosmos/Diagnostics/SnapshotStorageMetrics.cs lines 26-29](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Tributary.Runtime.Storage.Cosmos/Diagnostics/SnapshotStorageMetrics.cs#L26-L29): cosmos.snapshot.prune.count has unit prunes and description Number of prune operations.
- [src/Tributary.Runtime.Storage.Cosmos/Diagnostics/SnapshotStorageMetrics.cs lines 79-91](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Tributary.Runtime.Storage.Cosmos/Diagnostics/SnapshotStorageMetrics.cs#L79-L91): RecordPrune adds the supplied count and returns without recording anything when it is zero.
- [src/Tributary.Runtime.Storage.Cosmos/Storage/SnapshotCosmosRepository.cs lines 117-157](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Tributary.Runtime.Storage.Cosmos/Storage/SnapshotCosmosRepository.cs#L117-L157): Retention moduli control which versions are kept. They are rules, not the number of operations or snapshots actually deleted.

## Verification notes

Full source inspection. No fresh metrics listener or Cosmos integration probe was run.

- Observe cosmos.snapshot.prune.count using a MeterListener while making one successful call with two retention rules. Check whether it rises by two.
- Repeat with an empty rule list and confirm a completed prune produces no counter increment.
- Confirm whether maintainers want an operation count or a deleted-snapshot count. The supplied retention-rule count measures neither; the current counter description supports the operation interpretation.

## Confidence

**High**. The recorded value comes directly from the rule-list length and conflicts with the counter's explicit operation description.
