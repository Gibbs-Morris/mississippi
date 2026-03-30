using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

internal interface IReplicaSinkOperatorAuditSink
{
    Task RecordDeadLetterReadAsync(
        ReplicaSinkDeadLetterQuery query,
        int effectivePageSize,
        int resultCount,
        bool includedFailureSummary,
        bool redactedFailureSummary,
        CancellationToken cancellationToken = default
    );

    Task RecordReDriveAsync(
        ReplicaSinkDeadLetterReDriveRequest request,
        ReplicaSinkDeadLetterReDriveResult result,
        CancellationToken cancellationToken = default
    );
}

internal sealed class LoggerReplicaSinkOperatorAuditSink : IReplicaSinkOperatorAuditSink
{
    public LoggerReplicaSinkOperatorAuditSink(
        ILogger<LoggerReplicaSinkOperatorAuditSink> logger
    ) =>
        Logger = logger;

    private ILogger<LoggerReplicaSinkOperatorAuditSink> Logger { get; }

    public Task RecordDeadLetterReadAsync(
        ReplicaSinkDeadLetterQuery query,
        int effectivePageSize,
        int resultCount,
        bool includedFailureSummary,
        bool redactedFailureSummary,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        Logger.DeadLetterPageRead(
            query.Context.ActorId,
            query.Context.AccessLevel.ToString(),
            query.PageSize,
            effectivePageSize,
            resultCount,
            includedFailureSummary,
            redactedFailureSummary,
            !string.IsNullOrWhiteSpace(query.ContinuationToken));
        return Task.CompletedTask;
    }

    public Task RecordReDriveAsync(
        ReplicaSinkDeadLetterReDriveRequest request,
        ReplicaSinkDeadLetterReDriveResult result,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        Logger.DeadLetterReDrive(
            request.Context.ActorId,
            request.DeliveryKey,
            result.Outcome,
            result.WasQueued,
            result.TargetSourcePosition);
        return Task.CompletedTask;
    }
}

internal static partial class LoggerReplicaSinkOperatorAuditSinkLoggerExtensions
{
    [LoggerMessage(
        EventId = 20,
        Level = LogLevel.Information,
        Message =
            "Replica sink operator '{ActorId}' read a dead-letter page at access level '{AccessLevel}' with requested size {RequestedPageSize}, effective size {EffectivePageSize}, result count {ResultCount}, details included {IncludedFailureSummary}, redacted {RedactedFailureSummary}, continuation {HasContinuationToken}.")]
    public static partial void DeadLetterPageRead(
        this ILogger logger,
        string actorId,
        string accessLevel,
        int requestedPageSize,
        int effectivePageSize,
        int resultCount,
        bool includedFailureSummary,
        bool redactedFailureSummary,
        bool hasContinuationToken
    );

    [LoggerMessage(
        EventId = 21,
        Level = LogLevel.Information,
        Message =
            "Replica sink operator '{ActorId}' issued dead-letter re-drive for '{DeliveryKey}' with outcome '{Outcome}', queued {WasQueued}, target source position {TargetSourcePosition}.")]
    public static partial void DeadLetterReDrive(
        this ILogger logger,
        string actorId,
        string deliveryKey,
        string outcome,
        bool wasQueued,
        long? targetSourcePosition
    );
}
