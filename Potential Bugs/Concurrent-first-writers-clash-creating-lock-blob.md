# Concurrent first writers can clash while creating the lock blob

## Source location

- Project: `Brooks.Runtime.Storage.Cosmos`.
- Source file: [src/Brooks.Runtime.Storage.Cosmos/Locking/BlobDistributedLockManager.cs lines 72-75](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Locking/BlobDistributedLockManager.cs#L72-L75).
- Type: `BlobDistributedLockManager`.
- Member: `AcquireLockAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

Lock setup first checks whether the blob exists and then uploads it if absent. The create call is outside the bounded lease-conflict retry loop. Two callers can both see an absent blob, but only one upload can create it.

## Trigger

Two callers try to obtain the lock for a new stream before its lock blob exists. Both existence checks complete before either upload completes.

## Potential impact

One caller can fail during lock-file creation instead of reaching the normal lease-contention handling. This can reject an otherwise valid first-use append or read/recovery operation.

## Evidence

- [src/Brooks.Runtime.Storage.Cosmos/Locking/BlobDistributedLockManager.cs lines 69-78](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Locking/BlobDistributedLockManager.cs#L69-L78): ExistsAsync and UploadAsync are separate awaited operations; the upload has no already-exists handling.
- [src/Brooks.Runtime.Storage.Cosmos/Locking/BlobDistributedLockManager.cs lines 80-105](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Locking/BlobDistributedLockManager.cs#L80-L105): The conflict retry protects AcquireAsync only, after blob creation.
- [src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs lines 161-165](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs#L161-L165): Ordinary event append obtains the lock through this manager.
- [Azure SDK upload contract](https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.blobclient.uploadasync?view=azure-dotnet): The BinaryData/CancellationToken upload overload creates a blob and throws if it already exists.

## Verification notes

Use two actual or controlled Blob clients whose ExistsAsync calls both return false, then release both uploads. Check the losing request and whether it ever attempts AcquireAsync. Verify the repository's installed Azure.Storage.Blobs version and selected overload. A concurrent Blob integration test was not run during this investigation.

## Confidence

**High**. The check/create gap is unprotected and the selected upload overload does not accept an existing blob. The later lease retry cannot catch the earlier failure.
