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
        Message = "Resolving {GrainType} for UX projection cursor '{CursorKey}'")]
    public static partial void ResolvingCursorGrain(
        this ILogger logger,
        string grainType,
        UxProjectionCursorKey cursorKey
    );

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Resolving {GrainType} for UX projection entity '{EntityId}'")]
    public static partial void ResolvingProjectionGrain(
        this ILogger logger,
        string grainType,
        string entityId
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Resolving {GrainType} for versioned UX projection cache '{CacheKey}'")]
    public static partial void ResolvingVersionedCacheGrain(
        this ILogger logger,
        string grainType,
        UxProjectionVersionedCacheKey cacheKey
    );
}