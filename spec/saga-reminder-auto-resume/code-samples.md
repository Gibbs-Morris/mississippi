# Code Samples: Proposed Developer Experience

> Draft proposal only. Source facts were refreshed against main `c8da151e607bcc8f3b253519317a8e7418d26261` on 4 October 2026. Scheduling types shown here are proposed contracts. They are not implemented or validated by this PR. Recovery requirements remain tracked in [#404](https://github.com/Gibbs-Morris/mississippi/issues/404) and [#581](https://github.com/Gibbs-Morris/mississippi/issues/581).

These are partial design sketches. They omit complete reducers, serialization aliases/member IDs, registration and runtime validation; they are not compiled examples or current public APIs. `CommandHandlerBase<TCommand, TSnapshot>` is the existing base shape.

## Aggregate bindings

```csharp
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Brooks.Abstractions.Attributes;

[BrookName("GAME", "WORLDS", "WORLD")]
[AggregateScheduleDefaults(
    InitialDelaySeconds = 5,
    IntervalSeconds = 60,
    Backoff = ScheduleBackoff.Exponential,
    MaxAttempts = 5,
    JitterPercent = 10,
    MaxIntervalSeconds = 300)]
[ScheduledCommand(typeof(SpawnUnitsTickCommand), Name = "spawn-units", IntervalSeconds = 60)]
[ScheduledCommand(typeof(DecayTickCommand), Name = "decay", IntervalSeconds = 300)]
public sealed record WorldAggregate
{
    public int Units { get; init; }

    public int TickVersion { get; init; }

    public IReadOnlyDictionary<string, ScheduleCheckpoint> Schedules { get; init; }
        = new Dictionary<string, ScheduleCheckpoint>();
}
```

Attributes register metadata only. Start/stop events must establish the active generation in the aggregate's durable state before ticks can be accepted. The exact cross-grain handshake remains an implementation requirement.

## Explicit lifecycle

```csharp
await scheduleManager.StartScheduleAsync<WorldAggregate>(
    aggregateId: "world-1",
    scheduleName: "spawn-units",
    options: new ScheduleStartOptions
    {
        InitialDelay = TimeSpan.FromSeconds(10),
        AuditMode = ScheduleAuditMode.LifecycleOnly,
    },
    cancellationToken: cancellationToken);

await scheduleManager.StopScheduleAsync<WorldAggregate>(
    aggregateId: "world-1",
    scheduleName: "spawn-units",
    cancellationToken: cancellationToken);
```

Stopping persists disabled intent and an obsolete generation before unregistering the reminder. Queued callbacks and previously dispatched commands may still arrive. The authoritative aggregate must reject them.

## Durable duplicate decisions

A single `LastAppliedTickToken` rejects only the most recent duplicate; an older delayed tick can otherwise run again. This sketch requires an active generation and a contiguous logical sequence for each schedule. Logical tick identity and time are persisted before dispatch and reused on retry.

```csharp
public sealed record ScheduleCheckpoint(
    long Generation,
    bool Active,
    long LastAppliedSequence);

public sealed record SpawnUnitsTickCommand(
    string ScheduleName,
    long Generation,
    long TickSequence,
    DateTimeOffset TickAt,
    string TickToken);

public sealed record UnitsTickApplied(
    string ScheduleName,
    long Generation,
    long TickSequence,
    string TickToken,
    int Count,
    DateTimeOffset TickAt);

public sealed class SpawnUnitsTickCommandHandler
    : CommandHandlerBase<SpawnUnitsTickCommand, WorldAggregate>,
      IIdempotentScheduledCommandHandler<SpawnUnitsTickCommand, WorldAggregate>
{
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        SpawnUnitsTickCommand command,
        WorldAggregate? state)
    {
        if (state is null ||
            !state.Schedules.TryGetValue(command.ScheduleName, out var checkpoint) ||
            !checkpoint.Active ||
            checkpoint.Generation != command.Generation)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "The schedule generation is inactive or obsolete.");
        }

        if (command.TickSequence <= checkpoint.LastAppliedSequence)
        {
            return OperationResult.Ok<IReadOnlyList<object>>(Array.Empty<object>());
        }

        if (command.TickSequence != checkpoint.LastAppliedSequence + 1)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "A prior logical tick must be reconciled first.");
        }

        int spawned = (state.TickVersion + command.TickAt.Minute) % 3;
        return OperationResult.Ok<IReadOnlyList<object>>(
        [
            new UnitsTickApplied(
                command.ScheduleName,
                command.Generation,
                command.TickSequence,
                command.TickToken,
                spawned,
                command.TickAt),
        ]);
    }
}
```

A reducer must persist the sequence checkpoint and domain change together from `UnitsTickApplied`, including when `Count` is zero. Another schedule has its own checkpoint. A future tick received before its predecessor fails closed until reconciled; the dispatcher must retain and retry the same logical sequence after a lost acknowledgement. Tests must cover replay after a later tick, generation changes, no-op ticks and out-of-order delivery.

This only sketches an event-stream domain update. Arbitrary external effects still need downstream deduplication, reconciliation or operator intervention after an unknown result.

## Optional audit history

Operational control state is durable in every audit mode. `ScheduleAuditAggregate` is optional history, not the sole source of active schedule intent.

Lifecycle-only history contains start, update, stop and exhaustion observations. Full tick history can additionally record trigger, dispatch and failure observations. No audit setting can disable restart control state.

## Saga adoption prerequisite

`ContinueSagaCommand` and `SagaResumeRequested` are unmerged #361 contracts. Do not infer safe forward or rollback execution from `Failed` or `Compensating`: current `ISagaState` has no durable compensation cursor or recovery direction. Agree and implement the recovery contract in #404/#581 before binding automatic saga continuation to these proposed schedules.

## Implementation checks

- Validate stable storage identities, command aliases and unique schedule names.
- Persist generation changes and logical tick identities before dispatch.
- Test stale start/update/stop races across grains and process restarts.
- Use finite retry policies for saga recovery; do not inherit unlimited generic retry semantics implicitly.
- Match reminder interval constraints to the selected provider and verify them in integration tests.
