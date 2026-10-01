# Multiple SignalR hubs share the first hub's streams and connection list

## Source location

- Project: `Aqueduct.Gateway`.
- Source file: [src/Aqueduct.Gateway/AqueductRegistrations.cs lines 43-48](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/AqueductRegistrations.cs#L43-L48).
- Type: `AqueductRegistrations`.
- Member: `AddAqueduct<THub>`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

Each hub gets its own lifetime manager, but those managers share one connection registry and one stream manager. The stream manager initializes only once. A second hub therefore reuses the first hub's broadcast stream and shares its client list, despite the registry's per-hub contract.

## Trigger

Register `AddAqueduct<HubA>`() and `AddAqueduct<HubB>`() in the same service collection, then connect clients to both and send a broadcast through either hub.

## Potential impact

A broadcast callback can include connections belonging to another hub, and a second hub can publish on the first hub's stream. Clients can receive another hub's invocation or miss their expected broadcast.

## Evidence

- [src/Aqueduct.Gateway/AqueductRegistrations.cs lines 43-48](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/AqueductRegistrations.cs#L43-L48), [src/Aqueduct.Gateway/AqueductRegistrations.cs lines 77-82](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/AqueductRegistrations.cs#L77-L82): TryAddSingleton uses shared nongeneric registry, heartbeat and stream manager types; lifetime managers alone are generic per hub.
- [src/Aqueduct.Gateway/IConnectionRegistry.cs lines 18-21](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/IConnectionRegistry.cs#L18-L21): The registry contract explicitly says it is intended to be scoped per hub type.
- [src/Aqueduct.Gateway/StreamSubscriptionManager.cs lines 94-126](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/StreamSubscriptionManager.cs#L94-L126), [src/Aqueduct.Gateway/StreamSubscriptionManager.cs lines 131-147](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/StreamSubscriptionManager.cs#L131-L147): One initialized flag short-circuits later hub names; allStream uses the first hub name and is reused by PublishToAllAsync.
- [src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs lines 157-160](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs#L157-L160), [src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs lines 381-400](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs#L381-L400): Each hub adds to the shared registry; broadcast callback visits every registry connection without a hub filter.
- [src/Aqueduct.Abstractions/IAqueductNotifier.cs lines 25-28](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Abstractions/IAqueductNotifier.cs#L25-L28): The contract explicitly describes correct routing when multiple hubs are registered.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Build a service provider with two distinct Hub types and inspect shared dependencies and stream IDs; do not require colliding hub class names or connection IDs.
- Exercise broadcasts and targeted sends with two distinct clients to separate cross-hub registry leakage from wrong broadcast-channel selection.
- These are manifestations of the same missing per-hub ownership in DI and should stay one consolidated report.

## Confidence

**High**. The public multi-hub API and per-hub registry contract conflict directly with shared singleton storage and a single-hub initialization guard.
