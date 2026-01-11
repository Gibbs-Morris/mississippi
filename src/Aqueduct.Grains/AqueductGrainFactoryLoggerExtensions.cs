using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.Aqueduct.Grains;

/// <summary>
///     Logger extensions for <see cref="AqueductGrainFactory" />.
/// </summary>
internal static partial class AqueductGrainFactoryLoggerExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Resolving {GrainType} for hub '{HubName}' and connection '{ConnectionId}'")]
    public static partial void ResolvingClientGrain(
        this ILogger logger,
        string grainType,
        string hubName,
        string connectionId
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Resolving {GrainType} for hub '{HubName}' and group '{GroupName}'")]
    public static partial void ResolvingGroupGrain(
        this ILogger logger,
        string grainType,
        string hubName,
        string groupName
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Resolving {GrainType} with key '{DirectoryKey}'")]
    public static partial void ResolvingServerDirectoryGrain(
        this ILogger logger,
        string grainType,
        string directoryKey
    );
}
