namespace Mississippi.Ripples.Abstractions;

/// <summary>
///     Constants for SignalR hub method names used by Ripples.
/// </summary>
/// <remarks>
///     <para>
///         These constants ensure consistency between server-side hub implementations
///         and client-side SignalR connections. Using shared constants prevents
///         runtime errors from typos in method name strings.
///     </para>
/// </remarks>
public static class RippleHubConstants
{
    /// <summary>
    ///     The name of the Ripple SignalR hub.
    /// </summary>
    public const string HubName = "RippleHub";

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