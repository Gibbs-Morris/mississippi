using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Logger extensions for <see cref="ReplicaSinkRuntimeOperator" />.
/// </summary>
internal static partial class ReplicaSinkRuntimeOperatorLoggerExtensions
{
    [LoggerMessage(
        EventId = 31,
        Level = LogLevel.Error,
        Message =
            "Replica sink '{SinkKey}' quarantined lane '{DeliveryKey}' after dead-letter clear persistence failed during a re-drive request.")]
    public static partial void DeadLetterClearStoreQuarantined(
        this ILogger logger,
        string sinkKey,
        string deliveryKey,
        Exception exception
    );
}