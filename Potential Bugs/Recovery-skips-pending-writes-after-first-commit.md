# Recovery skips unfinished writes after the first committed event

## Source location

- Project: `Brooks.Runtime.Storage.Cosmos`.
- Source file: [src/Brooks.Runtime.Storage.Cosmos/Brooks/BrookRecoveryService.cs lines 69-119](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/BrookRecoveryService.cs#L69-L119).
- Type: `BrookRecoveryService`.
- Member: `GetOrRecoverCursorPositionAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The recovery service checks for a pending write only when the main saved position is absent. Once a stream has a committed position, a later unfinished write is skipped even though its pending record still exists.

## Trigger

Save at least one event. Start another append so its cursor-pending document is created, then stop the process or fail before that append commits and removes the document. Restart and read or append to the same stream.

## Potential impact

The reader can keep returning the earlier position. A later append can fail because it tries to create the same cursor-pending document. Ordinary writes to that stream may remain blocked until the unfinished record is dealt with.

## Evidence

- [src/Brooks.Runtime.Storage.Cosmos/Brooks/BrookRecoveryService.cs lines 69-119](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/BrookRecoveryService.cs#L69-L119): The main cursor is read first. The pending lookup and every recovery branch are inside the cursorDocument == null condition.
- [src/Brooks.Runtime.Storage.Cosmos/BrookStorageProvider.cs lines 84-88](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/BrookStorageProvider.cs#L84-L88): The storage provider's normal position-reading entry point calls this recovery method.
- [src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs lines 176-177](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs#L176-L177): Every append asks the same recovery service for its current position.
- [src/Brooks.Runtime.Storage.Cosmos/Storage/CosmosRepository.cs lines 200-221](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Storage/CosmosRepository.cs#L200-L221): A new pending write uses the fixed cursor-pending ID and CreateItemAsync, which does not replace an existing record.
- [src/Brooks.Runtime.Storage.Cosmos/Storage/CosmosRepository.cs lines 179-189](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Storage/CosmosRepository.cs#L179-L189): Successful commit updates the main position and then deletes the pending record; interruption can leave both records present.

## Verification notes

Create a stream with a committed position, seed or interrupt a second write after its pending record is saved, and use a fresh provider to read and append. Inspect the pending record and the create conflict. Check whether any external startup or maintenance component cleans this state before the shown path is reached. This investigation did not run that fault-injection scenario; the conclusion is based on the inspected control flow.

## Confidence

**High**. The guard excludes pending writes whenever an earlier main position exists, and both ordinary read and append paths use that guard. The persistent record identity gives a concrete failure path.
