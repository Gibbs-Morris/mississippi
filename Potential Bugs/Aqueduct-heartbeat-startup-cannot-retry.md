# Failed heartbeat startup is never retried after stream setup succeeds

## Source location

- Project: `Aqueduct.Gateway`.
- Source file: [src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs lines 354-373](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs#L354-L373).
- Type: `AqueductHubLifetimeManager<THub>`.
- Member: `EnsureStreamSetupAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The lifetime manager treats completed stream setup as proof that all startup is complete. Heartbeat registration runs afterward. If that registration fails, later calls return early because the streams are already initialized.

## Trigger

Initial stream setup succeeds but the directory grain's RegisterServerAsync fails. A later connection or broadcast invokes EnsureStreamSetupAsync again.

## Potential impact

The gateway can handle messaging calls without registering in the server directory or starting its heartbeat timer. The evidence does not show that automatic orphan cleanup currently depends on this registry.

## Evidence

- [src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs lines 354-373](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs#L354-L373): Early return uses stream state only, while heartbeat startup occurs afterward.
- [src/Aqueduct.Gateway/StreamSubscriptionManager.cs lines 123-127](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/StreamSubscriptionManager.cs#L123-L127): Sets initialized=true after stream subscriptions, before the lifetime manager starts heartbeats.
- [src/Aqueduct.Gateway/HeartbeatManager.cs lines 103-120](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/HeartbeatManager.cs#L103-L120), [src/Aqueduct.Gateway/HeartbeatManager.cs lines 135](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/HeartbeatManager.cs#L135): RegisterServerAsync is awaited before timer creation; started remains false if registration throws.
- [src/Aqueduct.Abstractions/IHeartbeatManager.cs lines 14-19](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Abstractions/IHeartbeatManager.cs#L14-L19): Manager owns periodic server liveness registration and scheduling.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Complete stream setup, fail the first directory registration, then call OnConnectedAsync or SendAllAsync again and verify HeartbeatManager.StartAsync is not invoked.
- Impact is loss of the declared liveness registry/timer; do not claim automatic orphan cleanup currently exists, because production code does not call GetDeadServersAsync.

## Confidence

**High**. The initialization flag covers only the first half of the setup operation and deterministically suppresses the second half's retry.
