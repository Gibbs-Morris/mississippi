# Manual connection restart does not restore earlier projection subscriptions

## Source location

- Project: `Inlet.Client`.
- Source file: [src/Inlet.Client/ActionEffects/HubConnectionProvider.cs lines 79-83](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/HubConnectionProvider.cs#L79-L83).
- Type: `HubConnectionProvider / InletSignalRActionEffect`.
- Member: `EnsureConnectedAsync / OnReconnectedAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

After Closed, EnsureConnectedAsync can start a new connection and publish Connected. Existing interests are restored only by the automatic Reconnected callback. The old subscription IDs stay in the map and make a later same-interest subscribe look unnecessary.

## Trigger

An established connection has active projection interests, automatic reconnect exhausts its attempts and closes, and a later RequestSignalRConnectionAction or new subscription action starts a new connection without reloading the client scope.

## Potential impact

The new connection appears healthy while earlier interests have no server subscription. Their updates remain absent until another recovery action rebuilds those interests.

## Evidence

- [src/Inlet.Client/ActionEffects/HubConnectionProvider.cs lines 75-85](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/HubConnectionProvider.cs#L75-L85), [src/Inlet.Client/ActionEffects/HubConnectionProvider.cs lines 118-133](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/HubConnectionProvider.cs#L118-L133): Manual startup emits Connected; Closed only updates transport state.
- [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 73-79](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L73-L79), [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 245-249](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L245-L249), [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 432-456](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L432-L456): Only OnReconnected registers restoration, and the retained map makes existing pairs appear subscribed.
- [src/Inlet.Gateway/InletHub.cs lines 108-118](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway/InletHub.cs#L108-L118): Disconnection clears every subscription for the old connection ID.
- [src/Inlet.Client/SignalRConnection/RequestSignalRConnectionAction.cs lines 1-17](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/SignalRConnection/RequestSignalRConnectionAction.cs#L1-L17): The client action requests a connection start through the effect.
- [docs/Docusaurus/docs/inlet/reference/generated-contracts.md lines 141](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/docs/Docusaurus/docs/inlet/reference/generated-contracts.md#L141): Successful transport reconnection is described as re-establishing active subscriptions.

- [Supporting reference](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client?view=aspnetcore-10.0): After exhausted automatic attempts the client enters Disconnected and fires Closed; restarting with StartAsync is a distinct manual start from the automatic Reconnected event.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Establish one interest, force Closed after reconnect exhaustion, then explicitly request a connection. Check the new ConnectionId and hub subscribe invocations for the earlier interest.
- This is a lifecycle split between manual restart and automatic reconnect, distinct from one failed resubscribe invocation.

## Confidence

**High**. Manual StartAsync has no restoration callback in this composition, while Closed removes server subscriptions for the old connection.
