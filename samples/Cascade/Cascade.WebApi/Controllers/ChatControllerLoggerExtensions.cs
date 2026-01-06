using Microsoft.Extensions.Logging;


namespace Cascade.WebApi.Controllers;

/// <summary>
///     High-performance logger extensions for <see cref="ChatController" />.
/// </summary>
internal static partial class ChatControllerLoggerExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Channel created: {ChannelId} with name '{ChannelName}'")]
    public static partial void ChannelCreated(
        ILogger logger,
        string channelId,
        string channelName
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Failed to create channel '{ChannelName}': {Error}")]
    public static partial void ChannelCreationFailed(
        ILogger logger,
        string channelName,
        string error
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Joined channel: {ChannelId}")]
    public static partial void ChannelJoined(
        ILogger logger,
        string channelId
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Failed to join channel '{ChannelId}': {Error}")]
    public static partial void ChannelJoinFailed(
        ILogger logger,
        string channelId,
        string error
    );

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Left channel: {ChannelId}")]
    public static partial void ChannelLeft(
        ILogger logger,
        string channelId
    );

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message = "Failed to leave channel '{ChannelId}': {Error}")]
    public static partial void ChannelLeaveFailed(
        ILogger logger,
        string channelId,
        string error
    );

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Debug,
        Message = "Message sent to channel: {ChannelId}")]
    public static partial void MessageSent(
        ILogger logger,
        string channelId
    );

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Warning,
        Message = "Failed to send message to channel '{ChannelId}': {Error}")]
    public static partial void MessageSendFailed(
        ILogger logger,
        string channelId,
        string error
    );
}
