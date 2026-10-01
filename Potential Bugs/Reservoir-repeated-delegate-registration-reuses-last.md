# Repeated delegate reducer registration makes earlier entries use the last delegate

## Source location

- Project: `Reservoir.Core`, `Tributary.Runtime`.
- Source file: [src/Reservoir.Core/ReservoirBuilderRegistrations.cs lines 86-90](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Reservoir.Core/ReservoirBuilderRegistrations.cs#L86-L90).
- Type: `ReservoirBuilderRegistrations / ReducerRegistrations`.
- Member: `AddReducer delegate overloads`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

Each delegate registration uses the same concrete service type. Every wrapper later asks dependency injection for that type, which returns the last registration. Earlier wrappers therefore point to the last delegate rather than their own delegate. Tributary repeats the pattern.

## Trigger

Register two delegate reducers for the same action/state pair, in a feature callback, with distinct transformations. Likewise register two Tributary delegates for one event/projection pair.

## Potential impact

Reservoir runs the last delegate twice and never runs the first. Tributary's first-match pipeline instead lets the later delegate replace the earlier matching one, changing the calculated state.

## Evidence

- [src/Reservoir.Core/ReservoirBuilderRegistrations.cs lines 75-92](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Reservoir.Core/ReservoirBuilderRegistrations.cs#L75-L92): Concrete delegate registrations share one service key; each untyped/typed factory resolves that key without capturing the corresponding registration.
- [src/Reservoir.Core/RootReducer.cs lines 121-129](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Reservoir.Core/RootReducer.cs#L121-L129): Every resolved matching reducer contributes to the resulting state.
- [docs/Docusaurus/docs/reservoir/concepts/state-flow.md lines 38](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/docs/Docusaurus/docs/reservoir/concepts/state-flow.md#L38): Every matching exact-action reducer runs in registration order; this is not the separately documented indexed/fallback ordering.
- [src/Tributary.Runtime/ReducerRegistrations.cs lines 25-38](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Tributary.Runtime/ReducerRegistrations.cs#L25-L38): The corresponding DelegateEventReducer registrations and resolving factories have the same concrete-service collision.
- [src/Tributary.Runtime/RootReducer.cs lines 194-204](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Tributary.Runtime/RootReducer.cs#L194-L204): Tributary returns after its first successful TryReduce, so the wrong first resolved delegate determines its result.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Use two Reservoir delegates x=>x+1 then x=>x*2 from initial 2: registration-preserving behavior produces 6, while two copies of the last delegate produce 8.
- For Tributary use clearly different matching delegates and verify the first intended transform rather than the last transform wins. Inspect the resolved untyped collection directly if needed.
- Do not merge this cause with indexed/fallback dispatch ordering or multi-interface indexing. No fresh DI probe was executed.

## Confidence

**High**. Default DI resolves one service with the last descriptor, and all earlier wrapper factories resolve that same key.
