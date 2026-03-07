using Microsoft.Extensions.Logging;


namespace Spring.Gateway;

/// <summary>
///     LoggerExtensions methods for <see cref="SpringLocalDevAuthenticationHandler" />.
/// </summary>
internal static partial class SpringLocalDevAuthenticationHandlerLoggerExtensions
{
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Local development authentication returned anonymous principal for scheme {Scheme}.")]
    public static partial void LocalDevAuthAnonymousRequest(
        this ILogger logger,
        string scheme
    );

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Local development authentication is disabled for scheme {Scheme}.")]
    public static partial void LocalDevAuthDisabled(
        this ILogger logger,
        string scheme
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Local development authentication rejected non-local request for scheme {Scheme}.")]
    public static partial void LocalDevAuthRejectedNonLocalRequest(
        this ILogger logger,
        string scheme
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message =
            "Local development authentication succeeded for scheme {Scheme}; user {UserId}; roles {RoleCount}; claims {ClaimCount}.")]
    public static partial void LocalDevAuthSucceeded(
        this ILogger logger,
        string scheme,
        string userId,
        int roleCount,
        int claimCount
    );
}