namespace Mississippi.Aqueduct.Abstractions;

/// <summary>
///     Provides a unique server identifier for the current server instance.
/// </summary>
/// <remarks>
///     <para>
///         The server ID is used to identify this server instance across the Orleans
///         cluster for heartbeat tracking, stream subscriptions, and client routing.
///         All components that need a server identity should inject this provider
///         rather than generating their own IDs.
///     </para>
///     <para>
///         The provider is registered as a singleton, ensuring all components
///         within the same process share the same server identity.
///     </para>
/// </remarks>
public interface IServerIdProvider
{
    /// <summary>
    ///     Gets the unique identifier for this server instance.
    /// </summary>
    /// <value>
    ///     A 32-character hexadecimal string (GUID without hyphens) that uniquely
    ///     identifies this server instance across the Orleans cluster.
    /// </value>
    string ServerId { get; }
}