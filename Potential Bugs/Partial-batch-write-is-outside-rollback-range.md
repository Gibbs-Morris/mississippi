# A partly saved event batch is left outside the rollback range

## Source location

- Project: `Brooks.Runtime.Storage.Cosmos`.
- Source file: [src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs lines 268-284](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs#L268-L284).
- Type: `EventBrookWriter`.
- Member: `AppendLargeBatchAsync / RollbackLargeBatchAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The writer increases processedEvents only when AppendEventBatchAsync completes. That repository method saves each event separately. If a later item fails, earlier items from the same batch already exist but are not included in the cleanup range.

## Trigger

Append enough events to select the large-batch path. Let at least one item in a batch be saved, then make the next item fail after its retries. The failure can occur in the first or a later batch.

## Potential impact

Cleanup can remove earlier complete batches while leaving the saved part of the failed batch behind. It can also remove the pending record. Retrying the append may then encounter documents at positions it needs to create, preventing progress.

## Evidence

- [src/Brooks.Runtime.Storage.Cosmos/Storage/CosmosRepository.cs lines 130-153](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Storage/CosmosRepository.cs#L130-L153): Each event is created individually; the method has no transaction or result exposing how many preceding items succeeded.
- [src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs lines 268-273](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs#L268-L273): processedEvents advances only after the whole repository call returns.
- [src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs lines 276-284](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs#L276-L284): Failure passes currentCursor + processedEvents as the last position to remove.
- [src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs lines 369-388](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs#L369-L388): Both deletion and verification use that bounded range, and the pending record is deleted before verification.

## Verification notes

Use a batch with a saved prefix and a controlled failure on a later CreateItemAsync call. Inspect all event documents, the main cursor and the pending record after the exception. Retry with the same expected version and check for position conflicts. Check whether any outer maintenance path removes the uncounted documents. No such runtime scenario was executed in this investigation.

## Confidence

**High**. The repository's individual writes and the caller's whole-batch accounting are directly incompatible on a partial failure. Both implementations were read in full.
