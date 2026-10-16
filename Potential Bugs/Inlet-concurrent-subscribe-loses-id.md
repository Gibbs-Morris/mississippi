# Overlapping subscription requests lose one server subscription ID

## Source location

- Project: `Inlet.Client`.
- Source file: [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 245-297](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L245-L297).
- Type: `InletSignalRActionEffect`.
- Member: `HandleSubscribeAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The effect checks for a completed subscription before awaiting the hub. It records the returned ID afterward. Two requests can both pass the check while the first is pending, then overwrite the same map entry with their different IDs.

## Trigger

One application owner dispatches the same DTO/entity subscribe intent twice while the first hub invocation is pending, such as repeated initialization or a repeated connect control. The store can dispatch the second action serially while the first effect is awaiting.

## Potential impact

Only one ID remains available to unsubscribe. The other server entry can keep producing duplicate callbacks until the connection is cleared.

## Evidence

- [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 41](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L41), [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 243-249](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L243-L249), [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 271-297](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L271-L297): There is one completed-ID map entry per pair and no pending reservation or per-pair coordination around the await.
- [src/Reservoir.Core/Store.cs lines 283-300](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Reservoir.Core/Store.cs#L283-L300), [src/Reservoir.Core/Store.cs lines 386-438](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Reservoir.Core/Store.cs#L386-L438): Dispatch starts effects without waiting for their asynchronous completion; a later dispatch can enter the same effect.
- [src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs lines 249-275](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs#L249-L275): Each successful SubscribeAsync creates a new GUID rather than deduplicating a projection pair.
- [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 345-372](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L345-L372): Unsubscribe removes and sends the one ID retained in the map.
- [docs/Docusaurus/docs/reservoir/reference/action-effects.md lines 44](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/docs/Docusaurus/docs/reservoir/reference/action-effects.md#L44): Separate dispatches can overlap while asynchronous work awaits completion.
- [docs/Docusaurus/docs/inlet/how-to/subscribe-to-projections.md lines 17-23](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/docs/Docusaurus/docs/inlet/how-to/subscribe-to-projections.md#L17-L23): The trigger retains the documented single-owner boundary; multiple owners are not required.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Delay two hub SubscribeAsync completions for one pair and complete both; then unsubscribe and verify which returned IDs are sent back.
- Use serialized Dispatch calls, not concurrent reducer execution. This is distinct from a late unsubscribe cancelling pending interest.

## Confidence

**High**. The check and assignment are separated by a real asynchronous operation, and server subscription IDs are unique per invocation.
