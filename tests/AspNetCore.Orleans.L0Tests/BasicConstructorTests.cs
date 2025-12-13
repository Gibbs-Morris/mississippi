namespace Mississippi.AspNetCore.Orleans.L0Tests;

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mississippi.AspNetCore.Orleans.Authentication;
using Mississippi.AspNetCore.Orleans.Authentication.Options;
using Mississippi.AspNetCore.Orleans.Caching;
using Mississippi.AspNetCore.Orleans.Caching.Options;
using Mississippi.AspNetCore.Orleans.L0Tests.SignalR;
using Mississippi.AspNetCore.Orleans.OutputCaching;
using Mississippi.AspNetCore.Orleans.OutputCaching.Options;
using Mississippi.AspNetCore.Orleans.SignalR;
using Mississippi.AspNetCore.Orleans.SignalR.Options;
using Moq;
using global::Orleans;
using Xunit;

/// <summary>
/// Basic constructor and validation tests for all adapters.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method naming convention")]
public sealed class BasicConstructorTests
{
    /// <summary>
    /// OrleansDistributedCache constructor with null logger should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void OrleansDistributedCache_Constructor_NullLogger_Throws()
    {
        // Arrange
        var mockClient = new Mock<IClusterClient>();
        var options = Options.Create(new DistributedCacheOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OrleansDistributedCache(null!, mockClient.Object, options, TimeProvider.System));
    }

    /// <summary>
    /// OrleansDistributedCache constructor with null cluster client should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void OrleansDistributedCache_Constructor_NullClusterClient_Throws()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OrleansDistributedCache>>();
        var options = Options.Create(new DistributedCacheOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OrleansDistributedCache(mockLogger.Object, null!, options, TimeProvider.System));
    }

    /// <summary>
    /// OrleansOutputCacheStore constructor with null logger should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void OrleansOutputCacheStore_Constructor_NullLogger_Throws()
    {
        // Arrange
        var mockClient = new Mock<IClusterClient>();
        var options = Options.Create(new OrleansOutputCacheOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OrleansOutputCacheStore(null!, mockClient.Object, options, TimeProvider.System));
    }

    /// <summary>
    /// OrleansTicketStore constructor with null logger should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void OrleansTicketStore_Constructor_NullLogger_Throws()
    {
        // Arrange
        var mockClient = new Mock<IClusterClient>();
        var options = Options.Create(new TicketStoreOptions());
        var serializer = new TicketSerializer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OrleansTicketStore(null!, mockClient.Object, options, serializer, TimeProvider.System));
    }

    /// <summary>
    /// OrleansHubLifetimeManager constructor with null logger should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void OrleansHubLifetimeManager_Constructor_NullLogger_Throws()
    {
        // Arrange
        var mockClient = new Mock<IClusterClient>();
        var options = Options.Create(new SignalROptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OrleansHubLifetimeManager<TestHub>(null!, mockClient.Object, options));
    }

    /// <summary>
    /// All adapters should construct successfully with valid parameters.
    /// </summary>
    [Fact]
    public void AllAdapters_Constructor_ValidParameters_Succeeds()
    {
        // Arrange
        var mockClient = new Mock<IClusterClient>();
        var mockDistCacheLogger = new Mock<ILogger<OrleansDistributedCache>>();
        var mockOutputCacheLogger = new Mock<ILogger<OrleansOutputCacheStore>>();
        var mockTicketStoreLogger = new Mock<ILogger<OrleansTicketStore>>();
        var mockHubLogger = new Mock<ILogger<OrleansHubLifetimeManager<TestHub>>>();

        var distCacheOptions = Options.Create(new DistributedCacheOptions());
        var outputCacheOptions = Options.Create(new OrleansOutputCacheOptions());
        var ticketOptions = Options.Create(new TicketStoreOptions());
        var signalROptions = Options.Create(new SignalROptions());
        var serializer = new TicketSerializer();

        // Act
        var distCache = new OrleansDistributedCache(mockDistCacheLogger.Object, mockClient.Object, distCacheOptions, TimeProvider.System);
        var outputCache = new OrleansOutputCacheStore(mockOutputCacheLogger.Object, mockClient.Object, outputCacheOptions, TimeProvider.System);
        var ticketStore = new OrleansTicketStore(mockTicketStoreLogger.Object, mockClient.Object, ticketOptions, serializer, TimeProvider.System);
        var hubManager = new OrleansHubLifetimeManager<TestHub>(mockHubLogger.Object, mockClient.Object, signalROptions);

        // Assert
        Assert.NotNull(distCache);
        Assert.NotNull(outputCache);
        Assert.NotNull(ticketStore);
        Assert.NotNull(hubManager);
    }
}
