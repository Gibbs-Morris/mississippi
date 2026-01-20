using System.Diagnostics.CodeAnalysis;


namespace Mississippi.Common.Abstractions;

/// <summary>
///     Central defaults for Mississippi framework configuration.
///     All options classes reference these values as their defaults, enabling
///     zero-configuration setup for simple scenarios while allowing per-library
///     customization for advanced use cases.
/// </summary>
/// <remarks>
///     <para>
///         <b>Simple usage:</b> Register a single stream provider named
///         <see cref="StreamProviderName" /> and all Mississippi libraries
///         will use it automatically.
///     </para>
///     <para>
///         <b>Advanced usage:</b> Register multiple stream providers and configure
///         each library's options to specify which provider it should use.
///     </para>
/// </remarks>
public static class MississippiDefaults
{
    /// <summary>
    ///     Default Cosmos database identifier.
    /// </summary>
    /// <value>The value is <c>"mississippi"</c>.</value>
    public const string DatabaseId = "mississippi";

    /// <summary>
    ///     Default Orleans stream provider name used across all Mississippi libraries.
    /// </summary>
    /// <value>The value is <c>"mississippi-streaming"</c>.</value>
    public const string StreamProviderName = "mississippi-streaming";

    /// <summary>
    ///     Default container/collection identifiers for storage.
    /// </summary>
    [SuppressMessage(
        "Design",
        "CA1034:Nested types should not be visible",
        Justification = "Intentional grouping of related constants under MississippiDefaults")]
    public static class ContainerIds
    {
        /// <summary>
        ///     Default Cosmos container name for event brooks.
        /// </summary>
        /// <value>The value is <c>"brooks"</c>.</value>
        public const string Brooks = "brooks";

        /// <summary>
        ///     Default Blob container name for distributed locks.
        /// </summary>
        /// <value>The value is <c>"locks"</c>.</value>
        public const string Locks = "locks";

        /// <summary>
        ///     Default Cosmos container name for snapshots.
        /// </summary>
        /// <value>The value is <c>"snapshots"</c>.</value>
        public const string Snapshots = "snapshots";
    }

    /// <summary>
    ///     Keyed service keys for DI registration.
    ///     Used with <c>[FromKeyedServices]</c> attribute to resolve specific service instances.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Multiple storage providers may coexist in the same application. Using keyed
    ///         services prevents conflicts when multiple clients are registered in the same
    ///         DI container. Host applications must forward their registered clients to these
    ///         keys for Mississippi libraries to resolve them.
    ///     </para>
    /// </remarks>
    [SuppressMessage(
        "Design",
        "CA1034:Nested types should not be visible",
        Justification = "Intentional grouping of related constants under MississippiDefaults")]
    public static class ServiceKeys
    {
        /// <summary>
        ///     Key for the Blob service client used for distributed locking.
        /// </summary>
        /// <value>The value is <c>"mississippi-blob-locking"</c>.</value>
        public const string BlobLocking = "mississippi-blob-locking";

        /// <summary>
        ///     Key for the Cosmos container used for event brooks (event streams).
        /// </summary>
        /// <value>The value is <c>"mississippi-cosmos-brooks"</c>.</value>
        public const string CosmosBrooks = "mississippi-cosmos-brooks";

        /// <summary>
        ///     Key for the <c>CosmosClient</c> used for event brooks storage.
        /// </summary>
        /// <value>The value is <c>"mississippi-cosmos-brooks-client"</c>.</value>
        public const string CosmosBrooksClient = "mississippi-cosmos-brooks-client";

        /// <summary>
        ///     Key for the Cosmos container used for snapshots.
        /// </summary>
        /// <value>The value is <c>"mississippi-cosmos-snapshots"</c>.</value>
        public const string CosmosSnapshots = "mississippi-cosmos-snapshots";

        /// <summary>
        ///     Key for the <c>CosmosClient</c> used for snapshot storage.
        /// </summary>
        /// <value>The value is <c>"mississippi-cosmos-snapshots-client"</c>.</value>
        public const string CosmosSnapshotsClient = "mississippi-cosmos-snapshots-client";
    }

    /// <summary>
    ///     Default Orleans stream namespaces for SignalR integration.
    /// </summary>
    [SuppressMessage(
        "Design",
        "CA1034:Nested types should not be visible",
        Justification = "Intentional grouping of related constants under MississippiDefaults")]
    public static class StreamNamespaces
    {
        /// <summary>
        ///     Stream namespace for hub-wide broadcasts to all connected clients.
        /// </summary>
        /// <value>The value is <c>"mississippi-all-clients"</c>.</value>
        public const string AllClients = "mississippi-all-clients";

        /// <summary>
        ///     Stream namespace for server-targeted messages.
        /// </summary>
        /// <value>The value is <c>"mississippi-server"</c>.</value>
        public const string Server = "mississippi-server";
    }
}