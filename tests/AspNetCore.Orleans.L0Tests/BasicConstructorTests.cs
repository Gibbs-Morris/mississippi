using System;
using System.Diagnostics.CodeAnalysis;

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

using Orleans;


namespace Mississippi.AspNetCore.Orleans.L0Tests;

/// <summary>
///     Basic constructor and validation tests for all adapters.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1707:Identifiers should not contain underscores",
    Justification = "Test method naming convention")]
public sealed class BasicConstructorTests
{
    /// <summary>
    ///     All adapters should construct successfully with valid parameters.
    /// </summary>
    [Fact]
    public void AllAdapters_Constructor_ValidParameters_Succeeds()
    {
        // Arrange
        Mock<IClusterClient> mockClient = new();
        Mock<ILogger<OrleansDistributedCache>> mockDistCacheLogger = new();
        Mock<ILogger<OrleansOutputCacheStore>> mockOutputCacheLogger = new();
        Mock<ILogger<OrleansTicketStore>> mockTicketStoreLogger = new();
        Mock<ILogger<OrleansHubLifetimeManager<TestHub>>> mockHubLogger = new();
        IOptions<DistributedCacheOptions> distCacheOptions = Options.Create(new DistributedCacheOptions());
        IOptions<OrleansOutputCacheOptions> outputCacheOptions = Options.Create(new OrleansOutputCacheOptions());
        IOptions<TicketStoreOptions> ticketOptions = Options.Create(new TicketStoreOptions());
        IOptions<SignalROptions> signalROptions = Options.Create(new SignalROptions());
        TicketSerializer serializer = new();

        // Act
        OrleansDistributedCache distCache = new(
            mockDistCacheLogger.Object,
            mockClient.Object,
            distCacheOptions,
            TimeProvider.System);
        OrleansOutputCacheStore outputCache = new(
            mockOutputCacheLogger.Object,
            mockClient.Object,
            outputCacheOptions,
            TimeProvider.System);
        OrleansTicketStore ticketStore = new(
            mockTicketStoreLogger.Object,
            mockClient.Object,
            ticketOptions,
            serializer,
            TimeProvider.System);
        OrleansHubLifetimeManager<TestHub> hubManager = new(mockHubLogger.Object, mockClient.Object, signalROptions);

        // Assert
        Assert.NotNull(distCache);
        Assert.NotNull(outputCache);
        Assert.NotNull(ticketStore);
        Assert.NotNull(hubManager);
    }

    /// <summary>
    ///     OrleansDistributedCache constructor with null cluster client should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void OrleansDistributedCache_Constructor_NullClusterClient_Throws()
    {
        // Arrange
        Mock<ILogger<OrleansDistributedCache>> mockLogger = new();
        IOptions<DistributedCacheOptions> options = Options.Create(new DistributedCacheOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OrleansDistributedCache(
            mockLogger.Object,
            null!,
            options,
            TimeProvider.System));
    }

    /// <summary>
    ///     OrleansDistributedCache constructor with null logger should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void OrleansDistributedCache_Constructor_NullLogger_Throws()
    {
        // Arrange
        Mock<IClusterClient> mockClient = new();
        IOptions<DistributedCacheOptions> options = Options.Create(new DistributedCacheOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OrleansDistributedCache(
            null!,
            mockClient.Object,
            options,
            TimeProvider.System));
    }

    /// <summary>
    ///     OrleansHubLifetimeManager constructor with null logger should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void OrleansHubLifetimeManager_Constructor_NullLogger_Throws()
    {
        // Arrange
        Mock<IClusterClient> mockClient = new();
        IOptions<SignalROptions> options = Options.Create(new SignalROptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OrleansHubLifetimeManager<TestHub>(null!, mockClient.Object, options));
    }

    /// <summary>
    ///     OrleansOutputCacheStore constructor with null logger should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void OrleansOutputCacheStore_Constructor_NullLogger_Throws()
    {
        // Arrange
        Mock<IClusterClient> mockClient = new();
        IOptions<OrleansOutputCacheOptions> options = Options.Create(new OrleansOutputCacheOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OrleansOutputCacheStore(
            null!,
            mockClient.Object,
            options,
            TimeProvider.System));
    }

    /// <summary>
    ///     OrleansTicketStore constructor with null logger should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void OrleansTicketStore_Constructor_NullLogger_Throws()
    {
        // Arrange
        Mock<IClusterClient> mockClient = new();
        IOptions<TicketStoreOptions> options = Options.Create(new TicketStoreOptions());
        TicketSerializer serializer = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OrleansTicketStore(
            null!,
            mockClient.Object,
            options,
            serializer,
            TimeProvider.System));
    }
}