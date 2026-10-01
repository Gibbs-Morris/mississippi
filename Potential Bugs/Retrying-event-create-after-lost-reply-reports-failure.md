# Retrying event creation after a lost reply can report a saved event as failed

## Source location

- Project: `Brooks.Runtime.Storage.Cosmos`.
- Source file: [src/Brooks.Runtime.Storage.Cosmos/Storage/CosmosRepository.cs lines 146-152](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Storage/CosmosRepository.cs#L146-L152).
- Type: `CosmosRepository`.
- Member: `AppendEventBatchAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The retry repeats CreateItemAsync with the same position-based document ID. It does not inspect an existing document when a repeated create returns a conflict. A retryable failure with an unknown write outcome can therefore become an already-exists failure.

## Trigger

Cosmos accepts an event document, but the caller receives a retryable timeout or service failure before it knows that creation succeeded. The configured retry policy invokes the same create again.

## Potential impact

The append can fail even though its event was saved. The caller can enter cleanup or retain an unfinished write, and a later retry can encounter the same document again. The exact recovery effect depends on which item and batch were involved.

## Evidence

- [src/Brooks.Runtime.Storage.Cosmos/Storage/CosmosRepository.cs lines 132-152](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Storage/CosmosRepository.cs#L132-L152): The document ID is the fixed event position, and the retry delegate calls CreateItemAsync again without an existing-item check.
- [src/Common.Runtime.Storage.Cosmos/Retry/CosmosRetryPolicy.cs lines 38-45](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Common.Runtime.Storage.Cosmos/Retry/CosmosRetryPolicy.cs#L38-L45): Timeout and several service-failure status codes are treated as retryable.
- [src/Common.Runtime.Storage.Cosmos/Retry/CosmosRetryPolicy.cs lines 80-94](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Common.Runtime.Storage.Cosmos/Retry/CosmosRetryPolicy.cs#L80-L94): An eventual conflict is not transient and is converted to InvalidOperationException.
- [src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs lines 268-284](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime.Storage.Cosmos/Brooks/EventBrookWriter.cs#L268-L284): The batch caller treats a failed repository call as an append failure and starts rollback.

## Verification notes

Simulate the first create saving the document and then returning a retryable failure. Make the repeated create return the actual duplicate status. Observe the append and recovery records. Verify how the deployed Cosmos SDK represents a lost response and whether its internal retries already reconcile this exact operation; an SDK guarantee that prevents the shown sequence would reject or narrow this finding. No network fault-injection run was performed.

## Confidence

**High**. The code explicitly retries non-idempotent create calls using a fixed ID and has no conflict reconciliation. The precise SDK/transport sequence remains an independent verification point.
