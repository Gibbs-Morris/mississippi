using Microsoft.Extensions.Logging;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos;

/// <summary>
///     Logging helpers for Cosmos replica sink container initialization.
/// </summary>
internal static partial class CosmosReplicaSinkContainerInitializerLoggerExtensions
{
    [LoggerMessage(
        EventId = 4301,
        Level = LogLevel.Information,
        Message = "Initialized Cosmos replica sink containers for {SinkCount} sink registrations.")]
    internal static partial void LogInitializedContainers(
        this ILogger logger,
        int sinkCount
    );

    [LoggerMessage(
        EventId = 4300,
        Level = LogLevel.Information,
        Message = "Initializing Cosmos replica sink containers for {SinkCount} sink registrations.")]
    internal static partial void LogInitializingContainers(
        this ILogger logger,
        int sinkCount
    );
}