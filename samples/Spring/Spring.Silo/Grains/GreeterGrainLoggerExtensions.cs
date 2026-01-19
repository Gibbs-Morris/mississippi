using Microsoft.Extensions.Logging;


namespace Spring.Silo.Grains;

/// <summary>
///     High-performance logger extensions for <see cref="GreeterGrain" />.
/// </summary>
internal static partial class GreeterGrainLoggerExtensions
{
    /// <summary>
    ///     Logs when a greeting is generated.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="name">The name being greeted.</param>
    /// <param name="greeting">The greeting message generated.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Greeting generated for {Name}: {Greeting}")]
    public static partial void LogGreetingGenerated(
        this ILogger<GreeterGrain> logger,
        string name,
        string greeting
    );
}