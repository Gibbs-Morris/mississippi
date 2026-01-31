namespace Mississippi.Inlet.Server.Abstractions;

/// <summary>
///     Constants for SignalR hub method names used by Inlet.
/// </summary>
/// <remarks>
///     <para>
///         These constants ensure consistency between server-side hub implementations
///         and client-side SignalR connections. Using shared constants prevents
///         runtime errors from typos in method name strings.
///     </para>
/// </remarks>
public static class InletHubConstants
{
    /// <summary>
    ///     The name of the Inlet SignalR hub.
    /// </summary>
    public const string HubName = "InletHub";

    /// <summary>
    ///     The hub method name for receiving projection update notifications.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Parameters: (string projectionType, string entityId, long newVersion).
    ///     </para>
    /// </remarks>
    public const string ProjectionUpdatedMethod = "ProjectionUpdated";

    /// <summary>
    ///     The hub method name for subscribing to projection updates.
    /// </summary>
    public const string SubscribeMethod = "SubscribeAsync";

    /// <summary>
    ///     The hub method name for unsubscribing from projection updates.
    /// </summary>
    public const string UnsubscribeMethod = "UnsubscribeAsync";
}