# A reader can delete events from a first write that is still running

## Source location

- Project: `Brooks.Runtime.Storage.Cosmos`.
- Source file: [src/Brooks.Runtime.Storage.Cosmos/Brooks/BrookRecoveryService.cs lines 88-92](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/BrookRecoveryService.cs#L88-L92).
- Type: `BrookRecoveryService`.
- Member: `GetOrRecoverCursorPositionAsync / RecoverFromOrphanedOperationAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

Recovery treats a pending first write as abandoned without waiting for the writer's lock. It acquires a different lock key, so a reader and writer can both change the same stream at the same time.

## Trigger

Start the first multi-event append to a stream. Pause after its pending record and first event are saved, while another event is still missing. Read the stream's position through a separate reader before the append finishes.

## Potential impact

Recovery can delete the events already saved by the active writer. The writer may then save the remaining events and commit a position whose history has a gap. The exact result depends on the timing of database calls.

## Evidence

- [src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs lines 161-165](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs#L161-L165): The writer holds a lock named with brookId.ToString().
- [src/Brooks.Runtime.Storage.Cosmos/Brooks/BrookRecoveryService.cs lines 88-92](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/BrookRecoveryService.cs#L88-L92): Recovery instead holds recovery-{brookId}; it does not acquire the writer's lock.
- [src/Brooks.Runtime.Storage.Cosmos/Locking/BlobDistributedLockManager.cs lines 69-75](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Locking/BlobDistributedLockManager.cs#L69-L75): The lock key forms the blob path, so these two keys refer to different lock blobs.
- [src/Brooks.Runtime.Storage.Cosmos/Brooks/BrookRecoveryService.cs lines 164-176](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/BrookRecoveryService.cs#L164-L176): If not every expected event exists, recovery selects rollback.
- [src/Brooks.Runtime.Storage.Cosmos/Brooks/BrookRecoveryService.cs lines 187-208](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/BrookRecoveryService.cs#L187-L208): Rollback deletes events and the pending record.
- [src/Brooks.Runtime.Storage.Cosmos/Storage/CosmosRepository.cs lines 130-153](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Storage/CosmosRepository.cs#L130-L153): The append saves the events individually, exposing a window in which only part of the pending write exists.
- [src/Brooks.Runtime.Storage.Cosmos/BrookStorageProvider.cs lines 84-88](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/BrookStorageProvider.cs#L84-L88): A normal position read can enter recovery independently of the event-writer call.

## Verification notes

Use controlled database calls to pause the first writer after one event. Invoke a concurrent position read, then release the writer and inspect all event positions and the committed cursor. Check that the deployed caller arrangement permits this independent read/write overlap. No concurrent Cosmos or Blob integration scenario was run in this investigation.

## Confidence

**High**. The inspected lock identities are different and the incomplete-event branch performs deletion. The individual item writes provide a credible overlap window, while the final outcome still needs a controlled runtime check.
