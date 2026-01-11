using Microsoft.Extensions.Logging;


namespace Cascade.Web.Silo.Grains;

/// <summary>
///     High-performance logger extensions for <see cref="BroadcasterGrain" />.
/// </summary>
internal static partial class BroadcasterGrainLoggerExtensions
{
    /// <summary>
    ///     Logs when the broadcaster grain is activated.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="grainKey">The grain's primary key.</param>
    [LoggerMessage(
        EventId = 100,
        Level = LogLevel.Information,
        Message = "BroadcasterGrain activated with key: {GrainKey}")]
    public static partial void LogBroadcasterActivated(
        this ILogger<BroadcasterGrain> logger,
        string grainKey
    );

    /// <summary>
    ///     Logs when a message is broadcast to the stream.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="grainKey">The grain's primary key.</param>
    /// <param name="message">The message being broadcast.</param>
    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Information,
        Message = "Broadcasting message from {GrainKey}: {Message}")]
    public static partial void LogBroadcastingSent(
        this ILogger<BroadcasterGrain> logger,
        string grainKey,
        string message
    );
}