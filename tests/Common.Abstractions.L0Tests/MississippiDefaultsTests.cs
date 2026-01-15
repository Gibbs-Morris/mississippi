using Allure.Xunit.Attributes;

using Mississippi.Common.Abstractions;


namespace Mississippi.Common.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="MississippiDefaults" /> constants.
/// </summary>
[AllureParentSuite("Mississippi.Common.Abstractions")]
[AllureSuite("Core")]
[AllureSubSuite("MississippiDefaults")]
public sealed class MississippiDefaultsTests
{
    /// <summary>
    ///     ContainerIds.Brooks should be "brooks".
    /// </summary>
    [Fact]
    [AllureFeature("Container IDs")]
    public void ContainerIdsBrooksHasExpectedValue()
    {
        // Assert
        Assert.Equal("brooks", MississippiDefaults.ContainerIds.Brooks);
    }

    /// <summary>
    ///     ContainerIds.Locks should be "locks".
    /// </summary>
    [Fact]
    [AllureFeature("Container IDs")]
    public void ContainerIdsLocksHasExpectedValue()
    {
        // Assert
        Assert.Equal("locks", MississippiDefaults.ContainerIds.Locks);
    }

    /// <summary>
    ///     ContainerIds.Snapshots should be "snapshots".
    /// </summary>
    [Fact]
    [AllureFeature("Container IDs")]
    public void ContainerIdsSnapshotsHasExpectedValue()
    {
        // Assert
        Assert.Equal("snapshots", MississippiDefaults.ContainerIds.Snapshots);
    }

    /// <summary>
    ///     DatabaseId should be "mississippi".
    /// </summary>
    [Fact]
    [AllureFeature("Database")]
    public void DatabaseIdHasExpectedValue()
    {
        // Assert
        Assert.Equal("mississippi", MississippiDefaults.DatabaseId);
    }

    /// <summary>
    ///     ServiceKeys.BlobLocking should be "mississippi-blob-locking".
    /// </summary>
    [Fact]
    [AllureFeature("Service Keys")]
    public void ServiceKeysBlobLockingHasExpectedValue()
    {
        // Assert
        Assert.Equal("mississippi-blob-locking", MississippiDefaults.ServiceKeys.BlobLocking);
    }

    /// <summary>
    ///     ServiceKeys.CosmosBrooks should be "mississippi-cosmos-brooks".
    /// </summary>
    [Fact]
    [AllureFeature("Service Keys")]
    public void ServiceKeysCosmosBrooksHasExpectedValue()
    {
        // Assert
        Assert.Equal("mississippi-cosmos-brooks", MississippiDefaults.ServiceKeys.CosmosBrooks);
    }

    /// <summary>
    ///     ServiceKeys.CosmosSnapshots should be "mississippi-cosmos-snapshots".
    /// </summary>
    [Fact]
    [AllureFeature("Service Keys")]
    public void ServiceKeysCosmosSnapshotsHasExpectedValue()
    {
        // Assert
        Assert.Equal("mississippi-cosmos-snapshots", MississippiDefaults.ServiceKeys.CosmosSnapshots);
    }

    /// <summary>
    ///     StreamNamespaces.AllClients should be "mississippi-all-clients".
    /// </summary>
    [Fact]
    [AllureFeature("Stream Namespaces")]
    public void StreamNamespacesAllClientsHasExpectedValue()
    {
        // Assert
        Assert.Equal("mississippi-all-clients", MississippiDefaults.StreamNamespaces.AllClients);
    }

    /// <summary>
    ///     StreamNamespaces.Server should be "mississippi-server".
    /// </summary>
    [Fact]
    [AllureFeature("Stream Namespaces")]
    public void StreamNamespacesServerHasExpectedValue()
    {
        // Assert
        Assert.Equal("mississippi-server", MississippiDefaults.StreamNamespaces.Server);
    }

    /// <summary>
    ///     StreamProviderName should be "mississippi-streaming".
    /// </summary>
    [Fact]
    [AllureFeature("Streaming")]
    public void StreamProviderNameHasExpectedValue()
    {
        // Assert
        Assert.Equal("mississippi-streaming", MississippiDefaults.StreamProviderName);
    }
}
