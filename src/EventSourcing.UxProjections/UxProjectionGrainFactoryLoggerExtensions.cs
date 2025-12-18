using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.UxProjections.Abstractions;


namespace Mississippi.EventSourcing.UxProjections;

/// <summary>
///     Logger extensions for <see cref="UxProjectionGrainFactory" />.
/// </summary>
internal static partial class UxProjectionGrainFactoryLoggerExtensions
{
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Resolving {GrainType} for UX projection cursor '{ProjectionKey}'")]
    public static partial void ResolvingCursorGrain(
        this ILogger logger,
        string grainType,
        UxProjectionKey projectionKey
    );

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Resolving {GrainType} for UX projection '{ProjectionKey}'")]
    public static partial void ResolvingProjectionGrain(
        this ILogger logger,
        string grainType,
        UxProjectionKey projectionKey
    );
}