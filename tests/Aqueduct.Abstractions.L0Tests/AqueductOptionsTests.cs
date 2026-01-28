
using Mississippi.Common.Abstractions;


namespace Mississippi.Aqueduct.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="AqueductOptions" /> configuration.
/// </summary>
public sealed class AqueductOptionsTests
{
    /// <summary>
    ///     Tests that default all clients stream namespace is set correctly.
    /// </summary>
    [Fact(DisplayName = "AllClientsStreamNamespace Defaults to Unified Value")]
    public void AllClientsStreamNamespaceShouldDefaultToUnifiedValue()
    {
        // Arrange & Act
        AqueductOptions options = new();

        // Assert
        Assert.Equal(MississippiDefaults.StreamNamespaces.AllClients, options.AllClientsStreamNamespace);
    }

    /// <summary>
    ///     Tests that properties can be set to custom values.
    /// </summary>
    [Fact(DisplayName = "All Properties Can Be Customized")]
    public void AllPropertiesShouldBeCustomizable()
    {
        // Arrange
        AqueductOptions options = new()
        {
            StreamProviderName = "CustomProvider",
            ServerStreamNamespace = "CustomServers",
            AllClientsStreamNamespace = "CustomAllClients",
            HeartbeatIntervalMinutes = 5,
        };

        // Assert
        Assert.Equal("CustomProvider", options.StreamProviderName);
        Assert.Equal("CustomServers", options.ServerStreamNamespace);
        Assert.Equal("CustomAllClients", options.AllClientsStreamNamespace);
        Assert.Equal(5, options.HeartbeatIntervalMinutes);
    }

    /// <summary>
    ///     Tests that dead server timeout multiplier can be customized.
    /// </summary>
    [Fact(DisplayName = "DeadServerTimeoutMultiplier Can Be Customized")]
    public void DeadServerTimeoutMultiplierShouldBeCustomizable()
    {
        // Arrange
        AqueductOptions options = new()
        {
            DeadServerTimeoutMultiplier = 5,
        };

        // Assert
        Assert.Equal(5, options.DeadServerTimeoutMultiplier);
    }

    /// <summary>
    ///     Tests that default dead server timeout multiplier is set correctly.
    /// </summary>
    [Fact(DisplayName = "DeadServerTimeoutMultiplier Defaults to 3")]
    public void DeadServerTimeoutMultiplierShouldDefaultToThree()
    {
        // Arrange & Act
        AqueductOptions options = new();

        // Assert
        Assert.Equal(3, options.DeadServerTimeoutMultiplier);
    }

    /// <summary>
    ///     Tests that default heartbeat interval is set correctly.
    /// </summary>
    [Fact(DisplayName = "HeartbeatIntervalMinutes Defaults to 1")]
    public void HeartbeatIntervalMinutesShouldDefaultToOne()
    {
        // Arrange & Act
        AqueductOptions options = new();

        // Assert
        Assert.Equal(1, options.HeartbeatIntervalMinutes);
    }

    /// <summary>
    ///     Tests that default server stream namespace is set correctly.
    /// </summary>
    [Fact(DisplayName = "ServerStreamNamespace Defaults to Unified Value")]
    public void ServerStreamNamespaceShouldDefaultToUnifiedValue()
    {
        // Arrange & Act
        AqueductOptions options = new();

        // Assert
        Assert.Equal(MississippiDefaults.StreamNamespaces.Server, options.ServerStreamNamespace);
    }

    /// <summary>
    ///     Tests that default stream provider name is set correctly.
    /// </summary>
    [Fact(DisplayName = "StreamProviderName Defaults to Unified Value")]
    public void StreamProviderNameShouldDefaultToUnifiedValue()
    {
        // Arrange & Act
        AqueductOptions options = new();

        // Assert
        Assert.Equal(MississippiDefaults.StreamProviderName, options.StreamProviderName);
    }
}