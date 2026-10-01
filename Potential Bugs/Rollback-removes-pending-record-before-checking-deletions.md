# Rollback removes its recovery record before checking failed deletions

## Source location

- Project: `Brooks.Runtime.Storage.Cosmos`.
- Source file: [src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs lines 369-388](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs#L369-L388).
- Type: `EventBrookWriter`.
- Member: `RollbackLargeBatchAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

Rollback records several event-deletion failures and continues. It then deletes the pending write record before checking whether those events remain. Failure information can therefore be removed while storage is still only partly cleaned.

## Trigger

A large append fails after a complete batch was saved. During rollback, at least one event deletion exhausts its retries and throws an exception caught by TryWithRetryAsync, while deletion of the pending record succeeds.

## Potential impact

Leftover event documents can remain without their pending recovery record. The final exception reports incomplete cleanup, but a fresh recovery pass no longer has the record identifying the unfinished range. Later appends can collide with those documents.

## Evidence

- [src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs lines 345-365](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs#L345-L365): InvalidOperationException, TimeoutException and HttpRequestException are recorded rather than stopping rollback.
- [src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs lines 369-381](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs#L369-L381): Event deletion is followed by deletion of the pending cursor regardless of recorded event failures.
- [src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs lines 383-421](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs#L383-L421): Remaining events are checked and reported only after the pending record may be gone.
- [src/Common.Runtime.Storage.Cosmos/Retry/CosmosRetryPolicy.cs lines 80-94](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Common.Runtime.Storage.Cosmos/Retry/CosmosRetryPolicy.cs#L80-L94): A Cosmos failure after retry handling becomes InvalidOperationException, one of the recorded failure types.
- [src/Brooks.Runtime.Storage.Cosmos/Brooks/BrookRecoveryService.cs lines 74-92](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/BrookRecoveryService.cs#L74-L92): The recovery service relies on the pending record to identify an unfinished operation.

## Verification notes

Make deletion of a known appended event fail through the actual Cosmos retry policy, then allow pending-record deletion and existence checks to succeed. Inspect the surviving event and absent pending record. Use a fresh storage provider to test the next append and available recovery. Distinguish this from the separate bug where the failed batch's partial prefix is never included in the deletion range.

## Confidence

**High**. The deletion ordering, recorded-error branch and retry exception translation provide a concrete active path to losing recovery information.
