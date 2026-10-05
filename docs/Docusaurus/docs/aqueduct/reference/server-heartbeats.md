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

The default Aqueduct registration adds neither a hosted-service startup adapter nor an `ILifecycleParticipant<ISiloLifecycle>` service mapping, including on a silo-hosted gateway. The [hub lifetime manager](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs) starts heartbeats through backplane setup on `OnConnectedAsync`, `SendAllAsync`, or `SendAllExceptAsync`. Its separate silo lifecycle hook is used only when the host explicitly registers lifecycle participation; see [Orleans lifecycle registration](https://learn.microsoft.com/en-us/dotnet/orleans/host/silo-lifecycle). With default composition, an idle gateway can remain unregistered until one of those operations triggers setup.

The timer schedules its first callback immediately and uses [`AqueductOptions.HeartbeatIntervalMinutes`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Abstractions/AqueductOptions.cs) for its period, defaulting to one minute. Each callback queues a task that samples the connection-count provider and calls the directory's `HeartbeatAsync`.

Gateway option callbacks configure the same unnamed `AqueductOptions` across hub types, and `IHeartbeatManager` is a shared non-generic singleton. Callbacks compose in registration order; a later interval assignment affects the host-wide heartbeat manager rather than only its `THub`.

The interval is read when the timer is created. Changing the option does not reschedule an already-created timer.

Configure gateway options through [`IServiceCollection.AddAqueduct<THub>(Action<AqueductOptions>)`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Gateway/AqueductRegistrations.cs). The runtime builder's separate `AddAqueduct` surface selects provider/namespace settings, not the gateway heartbeat interval. No gateway validator enforces a positive interval: zero produces the immediate one-shot callback without repetition, while a negative minute value fails timer construction after the server was registered. Positive periods are also limited by [.NET 10 timer construction](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Threading/Timer.cs) to 4,294,967,294 milliseconds. The largest accepted integer minute value is 71,582; 71,583 or more throws `ArgumentOutOfRangeException` after registration, before the started flag is set.

Each timer callback starts a detached `Task.Run`. Slow heartbeats can overlap or accumulate; timer disposal does not await those detached tasks.

The start cancellation token controls waiting for the semaphore. Directory registration and heartbeat calls have no token parameter. Registration failures propagate; heartbeat processing catches and logs `OrleansException`, rather than every possible provider or background-task failure.

## Directory Observations

The [directory implementation](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Runtime/Grains/SignalRServerDirectoryGrain.cs) maintains registrations in memory. The no-argument directory factory uses `SignalRServerDirectoryKey.Default`, whose value is `default`. A different key resolves a separate directory with its own registrations; query the same key used by the heartbeat manager. Its entry transitions are:

- Registration adds or replaces a server entry with the current time and connection count zero.
- A heartbeat for a registered server replaces its timestamp and count.
- A heartbeat for an unknown server is logged and ignored; it does not register the server.
- Unregistration removes the entry if present and otherwise completes without adding one.

The [`Mississippi.Aqueduct` meter](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Runtime/Diagnostics/AqueductMetrics.cs) also exposes cumulative `signalr.server.register` and `signalr.server.heartbeat` counters. Registration increments on every accepted register call, including replacement of an existing entry. Heartbeat increments only for known-server updates; ignored unknown-server heartbeats do not increment it. These are event totals, not current server/connection counts.

Directory timestamps use its `TimeProvider`, which defaults to `TimeProvider.System`. They reflect receipt of a directory call, rather than a timestamp supplied by the gateway. Liveness subtraction uses UTC wall-clock readings rather than monotonic elapsed time: a forward clock step can classify recent entries as dead, while a backward step can delay stale-entry detection. `RegisterServerAsync`, `HeartbeatAsync`, and `UnregisterServerAsync` reject a null server ID with `ArgumentNullException` and an empty ID with `ArgumentException`; whitespace-only IDs are accepted. `HeartbeatAsync` does not validate the supplied connection count, so negative counts can be stored. That count is internal activation state: the public `ISignalRServerDirectoryGrain` contract offers no connection-count query. Its dead-server query returns IDs using heartbeat timestamps, without using the count; reporting it does not establish a supported load-balancing/query surface. These public grain mutators do not authenticate the caller or check ownership of the supplied server ID. Any Orleans caller able to reach the directory can alter another server's entry; the directory relies on trusted client/cluster access rather than per-server authorization.

Directory reactivation or a silo restart loses its in-memory registrations. Heartbeats from an already-started manager are then unknown and ignored; repeated `StartAsync` does not register again while its started flag remains true. Registration recovery needs an explicit stop/start or manager restart. The default singleton [`ServerIdProvider`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Aqueduct.Gateway/ServerIdProvider.cs) creates `Guid.NewGuid().ToString("N")`, a 32-character ID for that instance. A custom `IServerIdProvider` must return an ID unique among concurrently active gateways and stable for the manager's lifetime. Shared IDs replace the same directory entry; one gateway stopping can remove the other's registration, after which its heartbeats are ignored. A process restart creates a new ID; its heartbeats do not refresh the old registration. A successfully awaited `StopAsync` removes the old entry before restart. If shutdown unregistration fails or does not complete, that entry can remain until explicit removal or directory activation loss; synchronous disposal initiates removal but does not await it.

## Timeout Queries And Shutdown

`GetDeadServersAsync(timeout)` computes current time minus the caller's timeout and returns IDs whose heartbeat timestamp is strictly earlier than that cutoff. The query does not remove the entries or sort the returned IDs. Under meter `Mississippi.Aqueduct`, `signalr.server.dead` is a cumulative counter: each nonempty query adds the number of IDs returned. Repeated queries can count the same stale entries again; it is not a gauge of current dead servers or a count of unique dead IDs. Enumeration follows the directory dictionary, so list position does not establish age, registration order, or a stable cleanup priority.

The timeout is not validated. Zero selects entries older than the query instant; a negative value moves the cutoff into the future and can classify fresh servers as dead. Extreme values can make the date subtraction throw `ArgumentOutOfRangeException`.

`DeadServerTimeoutMultiplier` defaults to three in `AqueductOptions`, but the current production implementation does not read it. The directory does not schedule automatic timeout queries or eviction. Configuring that multiplier therefore does not establish an automatic expiry or cleanup window.

`StopAsync` disposes the timer and awaits directory unregistration. Its cancellation-token parameter is not used by this implementation. If unregistration fails, the timer has already been disposed and removed, but the started flag remains true. A following `StartAsync` can then return without recreating the timer. Complete a successful stop retry before starting again, or recreate the manager, to recover from that partial shutdown. Synchronous `Dispose` disposes local resources and discards the unregistration task, so disposal alone does not await its remote completion.

The startup semaphore does not serialize `StopAsync` against startup. Do not overlap start/stop/dispose calls: a racing stop can unregister before startup installs its timer, leaving subsequent heartbeats unknown. `StopAsync` also does not wait for detached heartbeat tasks already queued.

Default DI disposal calls synchronous `Dispose`; there is no hosted-service adapter that automatically awaits `StopAsync`. The hub lifetime manager's disposal only removes its lifecycle subscription. Applications needing confirmed unregistration must arrange an awaited stop before disposal.

The [gateway tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Aqueduct.Gateway.L0Tests/HeartbeatManagerTests.cs) cover startup registration, repeated starts, the provider guard, and stop unregistration. The [runtime directory tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Aqueduct.Runtime.L0Tests/SignalRServerDirectoryGrainTests.cs) cover registration, known/unknown heartbeats, and idempotent removal.

## Summary

Use heartbeat observations to query server age with an explicit timeout. Do not infer automatic eviction from the interval or multiplier defaults; use awaited shutdown when unregistration completion matters.

## Next Steps

- Read [Aqueduct Reference](./reference.md) for composition and gateway options.
- Read [Aqueduct Operations](../operations/operations.md) for provider and rollout guidance.
