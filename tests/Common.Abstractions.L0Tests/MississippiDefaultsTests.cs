namespace Mississippi.Common.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="MississippiDefaults" /> constants.
/// </summary>
public sealed class MississippiDefaultsTests
{
    /// <summary>
    ///     ContainerIds.Brooks should be "brooks".
    /// </summary>
    [Fact]
    public void ContainerIdsBrooksHasExpectedValue()
    {
        // Assert
        Assert.Equal("brooks", MississippiDefaults.ContainerIds.Brooks);
    }

    /// <summary>
    ///     ContainerIds.Locks should be "locks".
    /// </summary>
    [Fact]
    public void ContainerIdsLocksHasExpectedValue()
    {
        // Assert
        Assert.Equal("locks", MississippiDefaults.ContainerIds.Locks);
    }

    /// <summary>
    ///     ContainerIds.Snapshots should be "snapshots".
    /// </summary>
    [Fact]
    public void ContainerIdsSnapshotsHasExpectedValue()
    {
        // Assert
        Assert.Equal("snapshots", MississippiDefaults.ContainerIds.Snapshots);
    }

    /// <summary>
    ///     DatabaseId should be "mississippi".
    /// </summary>
    [Fact]
    public void DatabaseIdHasExpectedValue()
    {
        // Assert
        Assert.Equal("mississippi", MississippiDefaults.DatabaseId);
    }

    /// <summary>
    ///     ServiceKeys.BlobLocking should be "mississippi-blob-locking".
    /// </summary>
    [Fact]
    public void ServiceKeysBlobLockingHasExpectedValue()
    {
        // Assert
        Assert.Equal("mississippi-blob-locking", MississippiDefaults.ServiceKeys.BlobLocking);
    }

    /// <summary>
    ///     ServiceKeys.CosmosBrooks should be "mississippi-cosmos-brooks".
    /// </summary>
    [Fact]
    public void ServiceKeysCosmosBrooksHasExpectedValue()
    {
        // Assert
        Assert.Equal("mississippi-cosmos-brooks", MississippiDefaults.ServiceKeys.CosmosBrooks);
    }

    /// <summary>
    ///     ServiceKeys.CosmosSnapshots should be "mississippi-cosmos-snapshots".
    /// </summary>
    [Fact]
    public void ServiceKeysCosmosSnapshotsHasExpectedValue()
    {
        // Assert
        Assert.Equal("mississippi-cosmos-snapshots", MississippiDefaults.ServiceKeys.CosmosSnapshots);
    }

    /// <summary>
    ///     StreamNamespaces.AllClients should be "mississippi-all-clients".
    /// </summary>
    [Fact]
    public void StreamNamespacesAllClientsHasExpectedValue()
    {
        // Assert
        Assert.Equal("mississippi-all-clients", MississippiDefaults.StreamNamespaces.AllClients);
    }

    /// <summary>
    ///     StreamNamespaces.Server should be "mississippi-server".
    /// </summary>
    [Fact]
    public void StreamNamespacesServerHasExpectedValue()
    {
        // Assert
        Assert.Equal("mississippi-server", MississippiDefaults.StreamNamespaces.Server);
    }

    /// <summary>
    ///     StreamProviderName should be "mississippi-streaming".
    /// </summary>
    [Fact]
    public void StreamProviderNameHasExpectedValue()
    {
        // Assert
        Assert.Equal("mississippi-streaming", MississippiDefaults.StreamProviderName);
    }
}