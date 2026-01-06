// <copyright file="OrleansSignalRGrainObserverLoggerExtensions.cs" company="Gibbs-Morris">
// Proprietary and Confidential.
// All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;


namespace Mississippi.Viaduct;

/// <summary>
///     High-performance logger extensions for <see cref="OrleansSignalRGrainObserver" />.
/// </summary>
internal static partial class OrleansSignalRGrainObserverLoggerExtensions
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Observer sending message '{Method}' to all clients for hub '{HubName}'")]
    internal static partial void ObserverSendingToAll(
        this ILogger logger,
        string hubName,
        string method
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Observer sending message '{Method}' to connection '{ConnectionId}' for hub '{HubName}'")]
    internal static partial void ObserverSendingToConnection(
        this ILogger logger,
        string connectionId,
        string hubName,
        string method
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Observer sending message '{Method}' to group '{GroupName}' for hub '{HubName}'")]
    internal static partial void ObserverSendingToGroup(
        this ILogger logger,
        string groupName,
        string hubName,
        string method
    );
}