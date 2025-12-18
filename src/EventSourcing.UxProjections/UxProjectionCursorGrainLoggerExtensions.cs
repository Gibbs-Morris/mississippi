using System;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;


namespace Mississippi.EventSourcing.UxProjections;

/// <summary>
///     Logger extensions for <see cref="UxProjectionCursorGrain" />.
/// </summary>
internal static partial class UxProjectionCursorGrainLoggerExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "UX projection cursor grain activated with key '{PrimaryKey}' for projection '{ProjectionTypeName}' on brook '{BrookKey}'")]
    public static partial void CursorGrainActivated(
        this ILogger logger,
        string primaryKey,
        string projectionTypeName,
        BrookKey brookKey
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Invalid primary key format for cursor grain: '{PrimaryKey}'")]
    public static partial void CursorGrainInvalidPrimaryKey(
        this ILogger logger,
        string primaryKey,
        Exception exception
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "UX projection cursor position updated to {Position} for '{ProjectionKey}'")]
    public static partial void PositionUpdated(
        this ILogger logger,
        UxProjectionKey projectionKey,
        BrookPosition position
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Stream error occurred for UX projection cursor '{ProjectionKey}'")]
    public static partial void StreamError(
        this ILogger logger,
        UxProjectionKey projectionKey,
        Exception exception
    );

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Stream completed for UX projection cursor '{ProjectionKey}'")]
    public static partial void StreamCompleted(
        this ILogger logger,
        UxProjectionKey projectionKey
    );
}
