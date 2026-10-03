# Failed reconnect subscription keeps an ID from the old connection

## Source location

- Project: `Inlet.Client`.
- Source file: [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 451-476](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L451-L476).
- Type: `InletSignalRActionEffect`.
- Member: `OnReconnectedAsync / HandleSubscribeAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

Reconnection replaces a subscription ID only after a successful hub reply. If that request fails, the old ID stays in the map. A deliberate later subscribe for the same interest is skipped because the map still contains that key.

## Trigger

A previously subscribed client automatically reconnects with a new ConnectionId, one resubscribe invocation fails transiently, and the owner tries the pair's subscribe action again after access/dependencies recover.

## Potential impact

The client can keep an interest marked active without a live server subscription. Updates stay absent until another reconnect, an unsubscribe/subscribe sequence, or scope reload.

## Evidence

- [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 439-456](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L439-L456), [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 473-476](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L473-L476), [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 245-249](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L245-L249): Failed setup preserves the completed-ID entry and the duplicate check treats that entry as sufficient.
- [src/Inlet.Gateway/InletHub.cs lines 108-118](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway/InletHub.cs#L108-L118): Old connection subscriptions are cleared on disconnect.
- [docs/Docusaurus/docs/inlet/reference/generated-contracts.md lines 141](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/docs/Docusaurus/docs/inlet/reference/generated-contracts.md#L141): Reconnection restores active interests; the failure leaves a retained interest without a live server subscription.

- [Supporting reference](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client?view=aspnetcore-10.0): Automatic reconnection presents a new ConnectionId to the server.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Fail the reconnect SubscribeAsync before it returns an ID, then make the hub healthy and dispatch SubscribeToProjectionAction for the same pair. Verify the action is skipped with the old ID still present.
- Keep the impact bounded: no automatic retry guarantee is asserted, and explicit unsubscribe/subscribe or reload can recover. Confirm whether retaining interest keys while rejecting a deliberate retry is intended before remediation.

## Confidence

**Medium**. The stale-ID behavior is direct, but application recovery semantics for failed reconnect attempts merit independent confirmation.
