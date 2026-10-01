# Projection HTTP timeout leaves its loading flag set

## Source location

- Project: `Inlet.Client`.
- Source file: [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 191-210](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L191-L210).
- Type: `InletSignalRActionEffect`.
- Member: `HandleRefreshAsync / HandleSubscribeAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The effect sets loading before the HTTP request. It treats every OperationCanceledException as cancellation and exits without a result or error action. HttpClient also uses this exception family for its own timeout, even when the caller's token was not cancelled.

## Trigger

A registered projection's HTTP request exceeds HttpClient.Timeout while waiting for response headers, with CancellationToken.None as supplied by the live store. HttpClient reports that timeout as a TaskCanceledException/OperationCanceledException.

## Potential impact

The request has ended, but the projection stays marked as loading without a timeout error. A quiet projection can leave its loading indicator stuck until another action changes the entry.

## Evidence

- [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 191-210](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L191-L210), [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 262-263](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L262-L263), [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 303-319](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L303-L319): Both loading paths swallow every OperationCanceledException and yield no terminal action.
- [src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs lines 114-118](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs#L114-L118): The automatic fetcher invokes HttpClient.SendAsync with response-headers completion and the supplied token.
- [src/Reservoir.Core/Store.cs lines 411-426](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Reservoir.Core/Store.cs#L411-L426): Live effects are invoked with CancellationToken.None, so a transport timeout is not a caller-requested shutdown.
- [src/Inlet.Client/Reducers/ProjectionsReducer.cs lines 54-68](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/Reducers/ProjectionsReducer.cs#L54-L68), [src/Inlet.Client/Reducers/ProjectionsReducer.cs lines 78-94](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/Reducers/ProjectionsReducer.cs#L78-L94), [src/Inlet.Client/Reducers/ProjectionsReducer.cs lines 104-118](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/Reducers/ProjectionsReducer.cs#L104-L118), [src/Inlet.Client/Reducers/ProjectionsReducer.cs lines 128-144](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/Reducers/ProjectionsReducer.cs#L128-L144): Loading sets IsLoading; error and result actions clear it. No cancellation action is emitted by these branches.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Use a short HttpClient.Timeout and delay the response headers, keeping the effect token uncancelled. Observe loading followed by iterator completion with no terminal action.
- Distinguish real timeout from explicitly cancelling the test-harness token; the trigger occurs through normal live-store behavior.

## Confidence

**High**. The actual timeout exception is caught by the cancellation branch after the loading action was already emitted.
