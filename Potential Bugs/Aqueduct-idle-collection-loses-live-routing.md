# Idle grain collection can remove routing for an open connection

## Source location

- Project: `Aqueduct.Runtime`, `Inlet.Runtime`.
- Source file: [src/Aqueduct.Runtime/Grains/SignalRClientGrain.cs lines 44-51](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Runtime/Grains/SignalRClientGrain.cs#L44-L51).
- Type: `SignalRClientGrain / SignalRGroupGrain / InletSubscriptionGrain`.
- Member: `In-memory activation state and activation lifecycle`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

Connection routes, group members, and projection interests exist only in each grain's current in-memory instance. Normal Orleans idle collection can replace that instance while the external browser connection stays open. These grains neither restore the state nor keep the activation alive for that connection.

## Trigger

A connected browser and its gateways/silos remain healthy, but its client/group/subscription grains receive no calls or stream events longer than the configured Orleans collection age. The ordinary collector deactivates them, then a later targeted send or brook event activates fresh instances.

## Potential impact

A later send or update can reach a fresh grain with no route, members, or projection interests and be skipped. The browser remains connected, so its normal connection callback does not rebuild the missing state. This concerns healthy-host idle collection; durable recovery after a silo failure is not promised.

## Evidence

- [src/Aqueduct.Runtime/Grains/SignalRClientGrain.cs lines 44-51](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Runtime/Grains/SignalRClientGrain.cs#L44-L51), [src/Aqueduct.Runtime/Grains/SignalRClientGrain.cs lines 117-135](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Runtime/Grains/SignalRClientGrain.cs#L117-L135), [src/Aqueduct.Runtime/Grains/SignalRClientGrain.cs lines 217-221](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Runtime/Grains/SignalRClientGrain.cs#L217-L221): Fields start empty; ConnectAsync records routing once; an empty route makes SendMessageAsync return.
- [src/Aqueduct.Runtime/Grains/SignalRGroupGrain.cs lines 37-42](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Runtime/Grains/SignalRGroupGrain.cs#L37-L42), [src/Aqueduct.Runtime/Grains/SignalRGroupGrain.cs lines 109-115](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Runtime/Grains/SignalRGroupGrain.cs#L109-L115), [src/Aqueduct.Runtime/Grains/SignalRGroupGrain.cs lines 162-172](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Runtime/Grains/SignalRGroupGrain.cs#L162-L172): Membership is fresh in-memory state; activation logs without restoring it; fanout uses that set.
- [src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs lines 44-48](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs#L44-L48), [src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs lines 88-94](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs#L88-L94), [src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs lines 178-183](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs#L178-L183): Subscription mappings start empty; an incoming event for an unmapped brook returns without notifying.
- [src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs lines 150-165](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs#L150-L165): Routing is registered by the actual transport OnConnected callback, not every targeted send.
- [docs/Docusaurus/docs/aqueduct/operations/operations.md lines 19-21](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/docs/Docusaurus/docs/aqueduct/operations/operations.md#L19-L21), [docs/Docusaurus/docs/aqueduct/operations/operations.md lines 32-35](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/docs/Docusaurus/docs/aqueduct/operations/operations.md#L32-L35), [docs/Docusaurus/docs/aqueduct/operations/operations.md lines 90-93](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/docs/Docusaurus/docs/aqueduct/operations/operations.md#L90-L93): State is deliberately volatile and replay/silo-failure recovery is not promised. The trigger here is healthy-host idle collection, not an unsupported durable recovery requirement.

- [Supporting reference](https://learn.microsoft.com/en-us/dotnet/orleans/host/configuration-guide/activation-collection): Idle collection deactivates a grain after the configured age; receiving calls/events counts as activity, whereas merely having an open external browser connection does not.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Set a short class-specific collection age in an isolated host, establish a client/group/projection interest, leave the transport open and grains quiet, then observe new activation and late-send/event routing.
- Check the application's actual global/class collection settings before concluding it is affected. Hosts explicitly keeping these classes alive avoid the trigger. Do not claim loss after silo restart violates a durable-delivery guarantee.

## Confidence

**Medium**. State/lifecycle loss is clear under ordinary collection, but confirming the configured collection policy and explicit stream observer reactivation requires an isolated integration check.
