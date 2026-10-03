# Connection readiness call returns before a pending start has connected

## Source location

- Project: `Inlet.Client`.
- Source file: [src/Inlet.Client/ActionEffects/HubConnectionProvider.cs lines 79-84](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/HubConnectionProvider.cs#L79-L84).
- Type: `HubConnectionProvider`.
- Member: `EnsureConnectedAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

EnsureConnectedAsync waits for StartAsync only when the connection is Disconnected. If another start or reconnect is already pending, it returns successfully while the connection is still not ready. Its caller then tries to subscribe through that connection.

## Trigger

Dispatch two subscription actions for different projection pairs serially while the first action's SignalR startup is still negotiating, or dispatch a subscribe while automatic reconnection is pending.

## Potential impact

The subscription call can fail during negotiation. The interest may never be created even though connection startup later succeeds.

## Evidence

- [src/Inlet.Client/ActionEffects/IHubConnectionProvider.cs lines 31-38](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/IHubConnectionProvider.cs#L31-L38): The public method contract says it ensures the hub connection is started.
- [src/Inlet.Client/ActionEffects/HubConnectionProvider.cs lines 59](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/HubConnectionProvider.cs#L59), [src/Inlet.Client/ActionEffects/HubConnectionProvider.cs lines 75-85](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/HubConnectionProvider.cs#L75-L85): Only the Disconnected branch awaits readiness; there is no wait for an already-running start/reconnect.
- [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 150-166](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L150-L166), [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 269-294](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L269-L294): The effect awaits this method before invoking the hub, then converts the invocation failure to an error without establishing the interest.
- [src/Reservoir.Core/Store.cs lines 296-300](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Reservoir.Core/Store.cs#L296-L300): A later dispatch can run while the earlier effect is awaiting startup.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Delay negotiation in a real or controlled hub connection, start the first ensure, then call ensure again; verify the second does not report success before Connected.
- Use separate projection pairs and serialized dispatches, avoiding duplicate-ownership and concurrent Store reducer assumptions.

## Confidence

**High**. The return path for Connecting/Reconnecting is unconditional and contradicts the readiness assumption of the actual caller.
