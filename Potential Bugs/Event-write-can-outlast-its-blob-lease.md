# A slow event write can continue after its Blob lock expires

## Source location

- Project: `Brooks.Runtime.Storage.Cosmos`.
- Source file: [src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs lines 312-330](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs#L312-L330).
- Type: `EventBrookWriter`.
- Member: `AppendSingleBatchAsync / AppendLargeBatchAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

A batch can make many sequential database calls after its last lease check. There is no renewal loop during those calls or final lease check before the cursor is committed. RenewAsync can itself return without contacting Blob Storage while the lease is young.

## Trigger

Database calls and retry delays make one batch last longer than the configured finite lease, which is 60 seconds by default. Another caller then acquires the expired lock before the first batch finishes.

## Potential impact

The first writer can keep changing storage while another caller owns the lock. The pending-record guard may stop the second writer from appending, but it does not preserve the advertised exclusive access; unexpected conflicts or unsafe overlap with other operations remain possible.

## Evidence

- [src/Brooks.Runtime.Storage.Cosmos/BrookStorageOptions.cs lines 30-40](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/BrookStorageOptions.cs#L30-L40): The lock has a finite lease, with a 60-second default duration.
- [src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs lines 312-330](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs#L312-L330): The single-batch path checks renewal before the pending record and item calls, then commits without another check.
- [src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs lines 243-291](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs#L243-L291): The large-batch path renews only between batches, then commits without an ownership check.
- [src/Brooks.Runtime.Storage.Cosmos/Locking/BlobDistributedLock.cs lines 114-129](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Locking/BlobDistributedLock.cs#L114-L129): Renewal occurs only when requested and after its local threshold; there is no background renewal.
- [src/Brooks.Runtime.Storage.Cosmos/Storage/CosmosRepository.cs lines 130-153](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Storage/CosmosRepository.cs#L130-L153): A batch consists of multiple individually awaited, retried item writes.

## Verification notes

Use slow controlled item calls or a rate-limited Cosmos environment to exceed a valid lease duration, then acquire the same Blob lease from a second caller. Check whether the first writer still saves events or commits. Account for the cursor-pending guard before claiming two appends can both complete or data is corrupted. No timed lease integration scenario was run.

## Confidence

**Medium**. The finite lifetime and lack of in-batch renewal are clear. The pending-record guard limits some overlaps, so the exact failure and any data-integrity effect need runtime verification.
