# Reducer handling two event types is called for only one

## Source location

- Project: `Tributary.Runtime`.
- Source file: [src/Tributary.Runtime/RootReducer.cs lines 113-130](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Tributary.Runtime/RootReducer.cs#L113-L130).
- Type: `RootReducer<TProjection>`.
- Member: `ExtractEventType / BuildReducerIndex`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

A reducer can implement `IEventReducer<EventA,State>` and `IEventReducer<EventB,State>`. Index construction stops after the first matching interface and adds the reducer under only that event type. It is not kept in the fallback list, so its other advertised event type cannot reach it.

## Trigger

Register one concrete reducer implementing both typed interfaces and an untyped TryReduce that correctly handles both EventA and EventB. Replay a history containing both types.

## Potential impact

Events for the unindexed type leave state unchanged without calling the registered reducer. A projection can silently omit valid state changes from its event history.

## Evidence

- [src/Tributary.Abstractions/IEventReducer.cs lines 6-23](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Tributary.Abstractions/IEventReducer.cs#L6-L23), [src/Tributary.Abstractions/IEventReducer.cs lines 30-42](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Tributary.Abstractions/IEventReducer.cs#L30-L42): The public typed interface extends the untyped reducer contract. It does not state a one-event-interface restriction for a concrete implementation.
- [src/Tributary.Runtime/RootReducer.cs lines 68-84](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Tributary.Runtime/RootReducer.cs#L68-L84): Each reducer is placed under one extracted type or in fallback, never under multiple extracted types.
- [src/Tributary.Runtime/RootReducer.cs lines 113-130](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Tributary.Runtime/RootReducer.cs#L113-L130): The method returns immediately at the first matching typed interface.
- [src/Tributary.Runtime/RootReducer.cs lines 152-172](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Tributary.Runtime/RootReducer.cs#L152-L172): Dispatch checks only the current event's index and fallback. The second interface has no entry in either path.

## Verification notes

Full source inspection. No fresh multi-interface runtime probe was run.

- Create a concrete class implementing the two interfaces, with one TryReduce dispatching to both typed Reduce methods. Register it once and verify both distinct event types invoke it.
- Check whether maintainers intended an undocumented one-event-type-per-concrete-reducer restriction. No such restriction was found in the consulted Tributary contract.
- Do not apply this report to Reservoir, whose action-reducer contract explicitly requires exactly one action type.

## Confidence

**Medium**. The pipeline deterministically skips a second implemented event interface. The consulted public contract does not state a one-interface restriction, but support for that custom implementation needs independent confirmation.
