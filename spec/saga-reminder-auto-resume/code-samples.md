# Code Samples: Developer Experience

## Intent

This document shows clear, end-to-end examples of how developers use aggregate scheduling with explicit start/stop lifecycle.

## 1) Aggregate with multiple schedule bindings

```csharp
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions;

[BrookName("world")]
[AggregateScheduleDefaults(
    InitialDelaySeconds = 5,
    IntervalSeconds = 60,
    Backoff = ScheduleBackoff.Exponential,
    MaxAttempts = 0,
    JitterPercent = 10,
    MaxIntervalSeconds = 300)]
[ScheduledCommand(typeof(SpawnUnitsTickCommand), Name = "spawn-units", IntervalSeconds = 60)]
[ScheduledCommand(typeof(DecayTickCommand), Name = "decay", IntervalSeconds = 300)]
public sealed class WorldAggregate
{
    public string WorldId { get; init; } = string.Empty;

    public int Units { get; init; }

    public int TickVersion { get; init; }

    public string? LastAppliedTickToken { get; init; }
}
```

Why this is friendly:

- One place to discover schedule bindings.
- Defaults reduce boilerplate.
- Multiple reminders are obvious and named.
- Nothing starts automatically.

## 2) Explicit start and stop from app/service code

```csharp
public sealed class WorldSchedulingService
{
    private IAggregateScheduleManager ScheduleManager { get; }

    public WorldSchedulingService(IAggregateScheduleManager scheduleManager)
    {
        ScheduleManager = scheduleManager;
    }

    public Task StartWorldSchedulesAsync(string worldId, CancellationToken cancellationToken)
    {
        return Task.WhenAll(
            ScheduleManager.StartScheduleAsync<WorldAggregate>(
                aggregateId: worldId,
                scheduleName: "spawn-units",
                options: new ScheduleStartOptions
                {
                    InitialDelay = TimeSpan.FromSeconds(10),
                },
                cancellationToken: cancellationToken),
            ScheduleManager.StartScheduleAsync<WorldAggregate>(
                aggregateId: worldId,
                scheduleName: "decay",
                cancellationToken: cancellationToken));
    }

    public Task StopWorldSchedulesAsync(string worldId, CancellationToken cancellationToken)
    {
        return Task.WhenAll(
            ScheduleManager.StopScheduleAsync<WorldAggregate>(worldId, "spawn-units", cancellationToken),
            ScheduleManager.StopScheduleAsync<WorldAggregate>(worldId, "decay", cancellationToken));
    }
}
```

## 3) Scheduled command and idempotent handler

> **Naming convention:** `CommandHandlerBase<TCommand, TSnapshot>` uses `TSnapshot` as the second type parameter (not `TAggregate`). The concrete type `WorldAggregate` is the snapshot type here.

```csharp
public sealed record SpawnUnitsTickCommand(
    string ScheduleName,
    DateTimeOffset TickAt,
    string TickToken);

public sealed class SpawnUnitsTickCommandHandler
    : CommandHandlerBase<SpawnUnitsTickCommand, WorldAggregate>,
      IIdempotentScheduledCommandHandler<SpawnUnitsTickCommand, WorldAggregate>
{
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        SpawnUnitsTickCommand command,
        WorldAggregate? state)
    {
        state ??= new WorldAggregate();

        // Idempotency: check if this tick token was already applied.
        // TickToken is derived deterministically from schedule + aggregate + tick window,
        // so duplicate/delayed ticks produce the same token.
        if (state.LastAppliedTickToken == command.TickToken)
        {
            return OperationResult.Ok<IReadOnlyList<object>>(Array.Empty<object>());
        }

        int spawned = ComputeSpawnCount(state.TickVersion, command.TickAt);
        if (spawned <= 0)
        {
            return OperationResult.Ok<IReadOnlyList<object>>(Array.Empty<object>());
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
        [
            new UnitsSpawned
            {
                Count = spawned,
                TickToken = command.TickToken,
                SpawnedAt = command.TickAt,
            },
        ]);
    }

    private static int ComputeSpawnCount(int tickVersion, DateTimeOffset tickAt)
    {
        return (tickVersion + tickAt.Minute) % 3;
    }
}
```

## 4) Audit mode example

```csharp
await scheduleManager.StartScheduleAsync<WorldAggregate>(
    aggregateId: "world-1",
    scheduleName: "spawn-units",
    options: new ScheduleStartOptions
    {
        AuditMode = ScheduleAuditMode.LifecycleOnly,
    },
    cancellationToken);
```

Expected audit events in `ScheduleAuditAggregate` stream:

- `ScheduleStarted`
- `TickTriggered`
- `TickDispatched` or `TickFailed`
- `ScheduleStopped`

## 5) Saga phase 2 usage

```csharp
[AggregateScheduleDefaults(IntervalSeconds = 30, Backoff = ScheduleBackoff.Exponential)]
[ScheduledCommand(typeof(ContinueSagaCommand), Name = "saga-resume", IntervalSeconds = 30)]
public sealed class PaymentSagaState : ISagaState
{
    public Guid SagaId { get; init; }

    public SagaPhase Phase { get; init; }

    public int LastCompletedStepIndex { get; init; }

    public string? CorrelationId { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public string? StepHash { get; init; }
}

await scheduleManager.StartScheduleAsync<PaymentSagaState>(
    aggregateId: paymentSagaId.ToString("N"),
    scheduleName: "saga-resume",
    cancellationToken: cancellationToken);
```

## 6) Practical guidelines

- Keep schedule names stable and human-readable.
- Treat every scheduled command as retriable and duplicate-safe.
- Prefer lifecycle-only audit mode first; upgrade to full tick history only when required.
- Stop schedules explicitly when domain state is terminal.
