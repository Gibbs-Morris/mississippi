---
id: server-heartbeats
title: Aqueduct Server Heartbeats
description: Reference gateway heartbeat scheduling, server registration, shutdown, and explicit dead-server queries.
sidebar_position: 3
sidebar_label: Aqueduct Server Heartbeats
---

# Aqueduct Server Heartbeats

## Overview

The gateway heartbeat manager reports a server ID and current connection count to an in-memory Orleans directory. Heartbeat scheduling and querying older registrations are separate operations.

## Applies To

- `Mississippi.Aqueduct.Abstractions.IHeartbeatManager`
- The gateway's `HeartbeatManager` implementation
- `ISignalRServerDirectoryGrain` and `AqueductOptions`

## Startup And Scheduling

[`StartAsync`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Gateway/HeartbeatManager.cs) requires a non-null connection-count provider and serializes startup through a semaphore. It registers the server before creating the timer. Repeated successful starts return without registering again or replacing the provider.

The timer schedules its first callback immediately and uses [`AqueductOptions.HeartbeatIntervalMinutes`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Abstractions/AqueductOptions.cs) for its period, defaulting to one minute. Each callback queues a task that samples the connection-count provider and calls the directory's `HeartbeatAsync`.

The interval is read when the timer is created. Changing the option does not reschedule an already-created timer.

Configure gateway options through [`IServiceCollection.AddAqueduct<THub>(Action<AqueductOptions>)`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Gateway/AqueductRegistrations.cs). The runtime builder's separate `AddAqueduct` surface selects provider/namespace settings, not the gateway heartbeat interval. No gateway validator enforces a positive interval: zero produces the immediate one-shot callback without repetition, while a negative minute value fails timer construction after the server was registered.

Each timer callback starts a detached `Task.Run`. Slow heartbeats can overlap or accumulate; timer disposal does not await those detached tasks.

The start cancellation token controls waiting for the semaphore. Directory registration and heartbeat calls have no token parameter. Registration failures propagate; heartbeat processing catches and logs `OrleansException`, rather than every possible provider or background-task failure.

## Directory Observations

The [directory implementation](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Runtime/Grains/SignalRServerDirectoryGrain.cs) maintains registrations in memory:

- Registration adds or replaces a server entry with the current time and connection count zero.
- A heartbeat for a registered server replaces its timestamp and count.
- A heartbeat for an unknown server is logged and ignored; it does not register the server.
- Unregistration removes the entry if present and otherwise completes without adding one.

Directory timestamps use its `TimeProvider`, which defaults to `TimeProvider.System`. They reflect receipt of a directory call, rather than a timestamp supplied by the gateway.

Directory reactivation or a silo restart loses its in-memory registrations. Heartbeats from an already-started manager are then unknown and ignored; repeated `StartAsync` does not register again while its started flag remains true. Registration recovery needs an explicit stop/start or manager restart.

## Timeout Queries And Shutdown

`GetDeadServersAsync(timeout)` computes current time minus the caller's timeout and returns IDs whose heartbeat timestamp is strictly earlier than that cutoff. The query does not remove the entries.

The timeout is not validated. Zero selects entries older than the query instant; a negative value moves the cutoff into the future and can classify fresh servers as dead. Extreme values can make the date subtraction throw `ArgumentOutOfRangeException`.

`DeadServerTimeoutMultiplier` defaults to three in `AqueductOptions`, but the current production implementation does not read it. The directory does not schedule automatic timeout queries or eviction. Configuring that multiplier therefore does not establish an automatic expiry or cleanup window.

`StopAsync` disposes the timer and awaits directory unregistration. Its cancellation-token parameter is not used by this implementation. Synchronous `Dispose` disposes local resources and discards the unregistration task, so disposal alone does not await its remote completion.

The startup semaphore does not serialize `StopAsync` against startup. Do not overlap start/stop/dispose calls: a racing stop can unregister before startup installs its timer, leaving subsequent heartbeats unknown. `StopAsync` also does not wait for detached heartbeat tasks already queued.

Default DI disposal calls synchronous `Dispose`; there is no hosted-service adapter that automatically awaits `StopAsync`. The hub lifetime manager's disposal only removes its lifecycle subscription. Applications needing confirmed unregistration must arrange an awaited stop before disposal.

The [gateway tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Aqueduct.Gateway.L0Tests/HeartbeatManagerTests.cs) cover startup registration, repeated starts, the provider guard, and stop unregistration. The [runtime directory tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Aqueduct.Runtime.L0Tests/SignalRServerDirectoryGrainTests.cs) cover registration, known/unknown heartbeats, and idempotent removal.

## Summary

Use heartbeat observations to query server age with an explicit timeout. Do not infer automatic eviction from the interval or multiplier defaults; use awaited shutdown when unregistration completion matters.

## Next Steps

- Read [Aqueduct Reference](./reference.md) for composition and gateway options.
- Read [Aqueduct Operations](../operations/operations.md) for provider and rollout guidance.
