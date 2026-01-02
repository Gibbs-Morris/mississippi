using System;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;
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
        Message =
            "UX projection cursor grain activated with key '{PrimaryKey}' for brook '{BrookName}' entity '{EntityId}'")]
    public static partial void CursorGrainActivated(
        this ILogger logger,
        string primaryKey,
        string brookName,
        string entityId
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
        Message = "UX projection cursor position updated to {Position} for '{CursorKey}'")]
    public static partial void PositionUpdated(
        this ILogger logger,
        UxProjectionCursorKey cursorKey,
        BrookPosition position
    );

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Stream completed for UX projection cursor '{CursorKey}'")]
    public static partial void StreamCompleted(
        this ILogger logger,
        UxProjectionCursorKey cursorKey
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Stream error occurred for UX projection cursor '{CursorKey}'")]
    public static partial void StreamError(
        this ILogger logger,
        UxProjectionCursorKey cursorKey,
        Exception exception
    );
}