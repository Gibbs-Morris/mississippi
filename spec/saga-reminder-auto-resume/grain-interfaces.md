# Grain Interfaces (Draft)

> Draft proposal only. Source facts were refreshed against main `c8da151e607bcc8f3b253519317a8e7418d26261` on 4 October 2026. Scheduling types shown here are proposed contracts. They are not implemented or validated by this PR. Recovery requirements remain tracked in [#404](https://github.com/Gibbs-Morris/mississippi/issues/404) and [#581](https://github.com/Gibbs-Morris/mississippi/issues/581).

## Intent

This document defines the interface shape for scheduler control-plane grains and related service abstractions.

## 1) Scheduler manager interface (app-facing)

> **Aggregate identity:** Resolve the required `[BrookName]` identity. Reject missing storage identities; a CLR type name fallback can collide and change after refactoring.

```csharp
public interface IAggregateScheduleManager
{
    Task StartScheduleAsync<TAggregate>(
        string aggregateId,
        string scheduleName,
        ScheduleStartOptions? options = null,
        CancellationToken cancellationToken = default)
        where TAggregate : class;

    Task UpdateScheduleAsync<TAggregate>(
        string aggregateId,
        string scheduleName,
        ScheduleUpdateOptions options,
        CancellationToken cancellationToken = default)
        where TAggregate : class;

    Task StopScheduleAsync<TAggregate>(
        string aggregateId,
        string scheduleName,
        CancellationToken cancellationToken = default)
        where TAggregate : class;
}
```

## 2) Scheduler grain interface (control-plane)

```csharp
using Orleans;

public interface IAggregateScheduleGrain : IGrainWithStringKey
{
    Task StartAsync(
        ScheduleRegistration registration,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ScheduleUpdate update,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stops the schedule and unregisters the underlying Orleans reminder.
    ///     Queued callbacks and dispatched commands may still arrive; obsolete generations must be rejected. The persisted state
    ///     is updated to Active = false. This MUST call UnregisterReminder to
    ///     prevent resource leaks and storage cost accumulation.
    /// </summary>
    Task StopAsync(
        CancellationToken cancellationToken = default);

    Task<ScheduleStatus> GetStatusAsync(
        CancellationToken cancellationToken = default);
}
```

Key convention:

- Grain key format: `<AggregateType>|<AggregateId>|<ScheduleName>`

**Concurrency:** Require non-reentrant local execution and durable generation/checkpoint validation at authoritative aggregate execution. Local serialization does not prevent stale commands already sent to another grain.

## 3) Schedule contracts

These are deliberate partial interface sketches. They are not registered or compiled contracts in this PR. Implementations still need serializer aliases/member IDs, storage names for persisted events, complete update-generation semantics and runtime validation.

> The proposed wire contract uses explicit stable command aliases. This is an identity/versioning choice; no claim is made here that `System.Type` cannot be serialized. Registration must validate every mapped command alias.

```csharp
public sealed record ScheduleRegistration(
    string AggregateType,
    string AggregateId,
    string ScheduleName,
    string CommandTypeName,
    long Generation,
    TimeSpan InitialDelay,
    TimeSpan Interval,
    ScheduleBackoff Backoff,
    int MaxAttempts,
    int JitterPercent,
    TimeSpan? MaxInterval,
    ScheduleAuditMode AuditMode);

public sealed record ScheduleUpdate(
    TimeSpan? Interval = null,
    ScheduleBackoff? Backoff = null,
    int? MaxAttempts = null,
    int? JitterPercent = null,
    TimeSpan? MaxInterval = null,
    ScheduleAuditMode? AuditMode = null);

public sealed record ScheduleStatus(
    bool Active,
    long Generation,
    int Attempt,
    DateTimeOffset? LastTriggeredAt,
    DateTimeOffset? NextDueAt,
    long LastAppliedTickSequence,
    string? LastErrorCode,
    string? LastErrorMessage);
```

## 4) Scheduler-to-aggregate dispatch interface

> **No `IServiceProvider` injection.** The dispatcher is an explicit dependency injected into the scheduler grain. It uses a pre-registered mapping of command type names (strings) to factory delegates, built during DI/startup scanning from `[ScheduledCommand]` attribute metadata. This satisfies the shared policy against service-locator patterns.

```csharp
public interface IScheduledCommandDispatcher
{
    Task DispatchAsync(
        string aggregateType,
        string aggregateId,
        string scheduleName,
        string commandTypeName,
        DateTimeOffset tickAt,
        string tickToken,
        long generation,
        long tickSequence,
        int attempt,
        CancellationToken cancellationToken = default);
}
```

## 5) Optional audit aggregate interface

> **Strongly typed audit events.** The previous `object` parameter is replaced with a discriminated union base record. Each audit event type is a concrete record inheriting from `ScheduleAuditEvent`, making the intended event alternatives explicit. These sketches omit serialization attributes/member IDs and storage identities; complete contracts and round-trip tests are still required.

```csharp
/// <summary>
///     Base record for schedule audit events.
/// </summary>
public abstract record ScheduleAuditEvent(
    string AggregateType,
    string AggregateId,
    string ScheduleName,
    string CommandTypeName,
    DateTimeOffset TimestampUtc);

public sealed record ScheduleStartedAuditEvent(
    string AggregateType,
    string AggregateId,
    string ScheduleName,
    string CommandTypeName,
    DateTimeOffset TimestampUtc,
    TimeSpan Interval,
    ScheduleBackoff Backoff)
    : ScheduleAuditEvent(AggregateType, AggregateId, ScheduleName, CommandTypeName, TimestampUtc);

public sealed record TickDispatchedAuditEvent(
    string AggregateType,
    string AggregateId,
    string ScheduleName,
    string CommandTypeName,
    DateTimeOffset TimestampUtc,
    string TickToken,
    int Attempt)
    : ScheduleAuditEvent(AggregateType, AggregateId, ScheduleName, CommandTypeName, TimestampUtc);

public sealed record TickFailedAuditEvent(
    string AggregateType,
    string AggregateId,
    string ScheduleName,
    string CommandTypeName,
    DateTimeOffset TimestampUtc,
    string TickToken,
    int Attempt,
    string ErrorCode,
    string ErrorMessage)
    : ScheduleAuditEvent(AggregateType, AggregateId, ScheduleName, CommandTypeName, TimestampUtc);

public sealed record ScheduleStoppedAuditEvent(
    string AggregateType,
    string AggregateId,
    string ScheduleName,
    string CommandTypeName,
    DateTimeOffset TimestampUtc)
    : ScheduleAuditEvent(AggregateType, AggregateId, ScheduleName, CommandTypeName, TimestampUtc);

public sealed record ScheduleExhaustedAuditEvent(
    string AggregateType,
    string AggregateId,
    string ScheduleName,
    string CommandTypeName,
    DateTimeOffset TimestampUtc,
    int TotalAttempts)
    : ScheduleAuditEvent(AggregateType, AggregateId, ScheduleName, CommandTypeName, TimestampUtc);

public interface IScheduleAuditAggregateGrain : IGrainWithStringKey
{
    Task AppendAsync(
        ScheduleAuditEvent auditEvent,
        CancellationToken cancellationToken = default);
}
```

Audit stream key convention:

- `<AggregateType>|<AggregateId>|<ScheduleName>`

## 6) Attribute contracts (for reference)

> **`MaxAttempts = 0` means unlimited retries.** A value of `1` means "run once, no retries." Negative values are rejected at startup validation.
> **Duplicate `Name` validation:** Multiple `[ScheduledCommand]` attributes on the same aggregate type with the same `Name` cause a startup exception.

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AggregateScheduleDefaultsAttribute : Attribute
{
    public int InitialDelaySeconds { get; init; } = 5;

    public int IntervalSeconds { get; init; } = 60;

    public ScheduleBackoff Backoff { get; init; } = ScheduleBackoff.Constant;

    public int MaxAttempts { get; init; } = 0;

    public int JitterPercent { get; init; } = 0;

    public int MaxIntervalSeconds { get; init; } = 0;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ScheduledCommandAttribute : Attribute
{
    public ScheduledCommandAttribute(Type commandType)
    {
        CommandType = commandType;
    }

    public Type CommandType { get; }

    public string Name { get; init; } = string.Empty;

    public int IntervalSeconds { get; init; } = 0;
}
```

## 7) Interface design notes

- Keep scheduler grain infrastructure-focused.
- Keep domain/business state in business aggregates.
- Persist operational control state in every mode through a proposed `ScheduleControlAggregate`; use `ScheduleAuditAggregate` only for optional historical observations.
- Ensure all scheduled command handlers are idempotent.
