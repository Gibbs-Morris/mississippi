# Failed SignalR startup leaves the displayed connection status at Connecting

## Source location

- Project: `Inlet.Client`.
- Source file: [src/Inlet.Client/ActionEffects/HubConnectionProvider.cs lines 81-83](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/HubConnectionProvider.cs#L81-L83).
- Type: `HubConnectionProvider`.
- Member: `EnsureConnectedAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The provider publishes Connecting before starting the connection, then publishes Connected only after success. An initial startup error has no local path that publishes Disconnected. The separate Closed callback may not run for an initial start failure.

## Trigger

The first negotiation/start attempt fails, for example when the configured gateway is unreachable or returns an authentication error.

## Potential impact

The actual connection can be Disconnected while the supplied store feature still says Connecting. A UI using that status can show a connection attempt that has already ended.

## Evidence

- [src/Inlet.Client/ActionEffects/HubConnectionProvider.cs lines 75-85](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/HubConnectionProvider.cs#L75-L85), [src/Inlet.Client/ActionEffects/HubConnectionProvider.cs lines 118-124](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/HubConnectionProvider.cs#L118-L124): The start call is outside a catch/finally; only the later Closed handler publishes Disconnected.
- [src/Inlet.Client/SignalRConnection/SignalRConnectionReducers.cs lines 42-70](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/SignalRConnection/SignalRConnectionReducers.cs#L42-L70): Connecting and Disconnected actions control the stored connection status.
- [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 145-151](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L145-L151): Initial connection failure occurs before any projection branch can emit its own terminal result.
- [docs/Docusaurus/docs/inlet/how-to/subscribe-to-projections.md lines 230-234](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/docs/Docusaurus/docs/inlet/how-to/subscribe-to-projections.md#L230-L234): The documented UI reads SignalRConnectionState.Status as its shared transport indicator; reload is a recovery option, not an active startup attempt.

- [Supporting reference](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client?view=aspnetcore-10.0): Initial start failures require separate handling; automatic reconnect is for an established connection's loss.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Fail only the first StartAsync and compare HubConnection.State with the supplied SignalRConnectionState.Status after the task faults.
- Verify the selected SignalR version's Closed-event behavior for an initial negotiation failure; do not assume automatic reconnect retries initial startup.

## Confidence

**Medium**. The missing terminal dispatch is clear; independent verification should confirm the exact client's initial-failure Closed-event behavior.
