# Invalid successful response leaves a completed command marked as executing

## Source location

- Project: `Inlet.Client.Abstractions`.
- Source file: [src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs lines 144-161](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs#L144-L161).
- Type: `CommandActionEffectBase<TAction,TRequestDto,TState,TExecutingAction,TSucceededAction,TFailedAction>`.
- Member: `HandleAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The command effect publishes Executing before making the request. Invalid JSON in a successful response throws JsonException, which is not handled by its HTTP-error or cancellation branches. No terminal action is produced.

## Trigger

A command receives a 2xx response with malformed/empty JSON or a non-JSON body from a proxy, incompatible gateway, or transient response failure.

## Potential impact

The command remains in InFlightCommands and its history still says Executing after the request has ended. The store catches the escaped effect error without producing the missing failure action, so a UI can remain stuck.

## Evidence

- [src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs lines 142-171](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs#L142-L171): The progress action precedes the parse, and parsing exceptions are outside the two caught exception types.
- [src/Inlet.Client.Abstractions/AggregateCommandStateReducers.cs lines 27-44](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Abstractions/AggregateCommandStateReducers.cs#L27-L44), [src/Inlet.Client.Abstractions/AggregateCommandStateReducers.cs lines 53-65](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Abstractions/AggregateCommandStateReducers.cs#L53-L65), [src/Inlet.Client.Abstractions/AggregateCommandStateReducers.cs lines 75-87](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Abstractions/AggregateCommandStateReducers.cs#L75-L87): Executing adds the ID; only terminal failed/succeeded lifecycle reducers remove it.
- [src/Reservoir.Core/RootActionEffect.cs lines 253-270](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Reservoir.Core/RootActionEffect.cs#L253-L270): The normal typed-root path stops enumerating an effect after a noncritical MoveNext exception without creating a feature action.
- [src/Reservoir.Core/Store.cs lines 424-437](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Reservoir.Core/Store.cs#L424-L437): Store catches effect exceptions to preserve dispatch, again without emitting command failure.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Return HTTP 200 with invalid JSON from a controlled handler and collect emitted lifecycle actions. Verify Executing is emitted but neither terminal action follows.
- Distinguish this from a non-2xx response, which the existing code converts into HttpError. No fresh probe or repository mutation was performed.

## Confidence

**High**. System.Text.Json parse failure is uncaught after progress was already emitted, and terminal reducers are the only removal paths.
