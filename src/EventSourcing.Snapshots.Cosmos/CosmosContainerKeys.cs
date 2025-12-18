namespace Mississippi.EventSourcing.Snapshots.Cosmos;

/// <summary>
///     Keys for keyed service registration of Cosmos DB containers.
/// </summary>
/// <remarks>
///     <para>
///         Multiple Cosmos storage providers (e.g., brooks and snapshots) may coexist in the same
///         application. Each provider requires its own <see cref="Microsoft.Azure.Cosmos.Container" />
///         instance pointing to a different collection. Using keyed services prevents conflicts
///         when both providers are registered in the same DI container.
///     </para>
/// </remarks>
public static class CosmosContainerKeys
{
    /// <summary>
    ///     Key for the snapshots Cosmos container.
    /// </summary>
    public const string Snapshots = "cosmos-container-snapshots";
}