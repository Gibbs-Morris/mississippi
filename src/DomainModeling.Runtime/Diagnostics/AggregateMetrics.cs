using System.Diagnostics;
using System.Diagnostics.Metrics;


namespace Mississippi.EventSourcing.Aggregates.Diagnostics;

/// <summary>
///     OpenTelemetry metrics for aggregate command execution.
/// </summary>
internal static class AggregateMetrics
{
    /// <summary>
    ///     The meter name for aggregate metrics.
    /// </summary>
    internal const string MeterName = "Mississippi.EventSourcing.Aggregates";

    private const string AggregateTypeTag = "aggregate.type";

    private const string CommandTypeTag = "command.type";

    private static readonly Meter AggregateMeter = new(MeterName);

    // Command metrics
    private static readonly Counter<long> CommandCount = AggregateMeter.CreateCounter<long>(
        "aggregate.command.count",
        "commands",
        "Number of commands executed.");

    private static readonly Histogram<double> CommandDuration = AggregateMeter.CreateHistogram<double>(
        "aggregate.command.duration",
        "ms",
        "Time to execute a command.");

    private static readonly Counter<long> CommandErrors = AggregateMeter.CreateCounter<long>(
        "aggregate.command.errors",
        "errors",
        "Number of command failures.");

    private static readonly Counter<long> ConcurrencyConflicts = AggregateMeter.CreateCounter<long>(
        "aggregate.concurrency.conflicts",
        "conflicts",
        "Number of optimistic concurrency failures.");

    private static readonly Counter<long> EventsProduced = AggregateMeter.CreateCounter<long>(
        "aggregate.events.produced",
        "events",
        "Number of events produced by commands.");

    // State metrics
    private static readonly Histogram<double> StateFetchDuration = AggregateMeter.CreateHistogram<double>(
        "aggregate.state.fetch.duration",
        "ms",
        "Time to fetch current state.");

    /// <summary>
    ///     Record a command failure.
    /// </summary>
    /// <param name="aggregateType">The aggregate type name.</param>
    /// <param name="commandType">The command type name.</param>
    /// <param name="durationMs">The duration of the command execution in milliseconds.</param>
    /// <param name="errorCode">The error code.</param>
    internal static void RecordCommandFailure(
        string aggregateType,
        string commandType,
        double durationMs,
        string errorCode
    )
    {
        TagList tags = default;
        tags.Add(AggregateTypeTag, aggregateType);
        tags.Add(CommandTypeTag, commandType);
        tags.Add("result", "failure");
        CommandCount.Add(1, tags);
        CommandDuration.Record(durationMs, tags);
        TagList errorTags = default;
        errorTags.Add(AggregateTypeTag, aggregateType);
        errorTags.Add(CommandTypeTag, commandType);
        errorTags.Add("error.code", errorCode);
        CommandErrors.Add(1, errorTags);
    }

    /// <summary>
    ///     Record a successful command execution.
    /// </summary>
    /// <param name="aggregateType">The aggregate type name.</param>
    /// <param name="commandType">The command type name.</param>
    /// <param name="durationMs">The duration of the command execution in milliseconds.</param>
    /// <param name="eventCount">The number of events produced.</param>
    internal static void RecordCommandSuccess(
        string aggregateType,
        string commandType,
        double durationMs,
        int eventCount
    )
    {
        TagList tags = default;
        tags.Add(AggregateTypeTag, aggregateType);
        tags.Add(CommandTypeTag, commandType);
        tags.Add("result", "success");
        CommandCount.Add(1, tags);
        CommandDuration.Record(durationMs, tags);
        if (eventCount > 0)
        {
            TagList eventTags = default;
            eventTags.Add(AggregateTypeTag, aggregateType);
            eventTags.Add(CommandTypeTag, commandType);
            EventsProduced.Add(eventCount, eventTags);
        }
    }

    /// <summary>
    ///     Record an optimistic concurrency conflict.
    /// </summary>
    /// <param name="aggregateType">The aggregate type name.</param>
    internal static void RecordConcurrencyConflict(
        string aggregateType
    )
    {
        TagList tags = default;
        tags.Add(AggregateTypeTag, aggregateType);
        ConcurrencyConflicts.Add(1, tags);
    }

    /// <summary>
    ///     Record state fetch duration.
    /// </summary>
    /// <param name="aggregateType">The aggregate type name.</param>
    /// <param name="durationMs">The duration of the state fetch in milliseconds.</param>
    internal static void RecordStateFetch(
        string aggregateType,
        double durationMs
    )
    {
        TagList tags = default;
        tags.Add(AggregateTypeTag, aggregateType);
        StateFetchDuration.Record(durationMs, tags);
    }
}