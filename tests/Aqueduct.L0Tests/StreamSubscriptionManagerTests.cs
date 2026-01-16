using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Messages;

using NSubstitute;

using Orleans;


namespace Mississippi.Aqueduct.L0Tests;

/// <summary>
///     Tests for <see cref="StreamSubscriptionManager" />.
/// </summary>
[AllureParentSuite("Aqueduct")]
[AllureSuite("Core")]
[AllureSubSuite("StreamSubscriptionManager")]
public sealed class StreamSubscriptionManagerTests
{
    private static IServerIdProvider CreateServerIdProvider(
        string? serverId = null
    )
    {
        IServerIdProvider provider = Substitute.For<IServerIdProvider>();
        provider.ServerId.Returns(serverId ?? Guid.NewGuid().ToString("N"));
        return provider;
    }

    /// <summary>
    ///     Constructor should succeed with valid dependencies.
    /// </summary>
    [Fact(DisplayName = "Constructor Succeeds With Valid Dependencies")]
    [AllureFeature("Construction")]
    public void ConstructorShouldSucceedWithValidDependencies()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<StreamSubscriptionManager> logger = Substitute.For<ILogger<StreamSubscriptionManager>>();

        // Act
        using StreamSubscriptionManager manager = new(CreateServerIdProvider(), clusterClient, options, logger);

        // Assert
        Assert.NotNull(manager);
        Assert.NotNull(manager.ServerId);
        Assert.Equal(32, manager.ServerId.Length); // GUID without hyphens
        Assert.False(manager.IsInitialized);
    }

    /// <summary>
    ///     Constructor should throw when clusterClient is null.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When ClusterClient Is Null")]
    [AllureFeature("Argument Validation")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Test expects exception before object is created")]
    public void ConstructorShouldThrowWhenClusterClientIsNull()
    {
        // Arrange
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<StreamSubscriptionManager> logger = Substitute.For<ILogger<StreamSubscriptionManager>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StreamSubscriptionManager(
            CreateServerIdProvider(),
            null!,
            options,
            logger));
    }

    /// <summary>
    ///     Constructor should throw when logger is null.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When Logger Is Null")]
    [AllureFeature("Argument Validation")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Test expects exception before object is created")]
    public void ConstructorShouldThrowWhenLoggerIsNull()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StreamSubscriptionManager(
            CreateServerIdProvider(),
            clusterClient,
            options,
            null!));
    }

    /// <summary>
    ///     Constructor should throw when options is null.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When Options Is Null")]
    [AllureFeature("Argument Validation")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Test expects exception before object is created")]
    public void ConstructorShouldThrowWhenOptionsIsNull()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        ILogger<StreamSubscriptionManager> logger = Substitute.For<ILogger<StreamSubscriptionManager>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StreamSubscriptionManager(
            CreateServerIdProvider(),
            clusterClient,
            null!,
            logger));
    }

    /// <summary>
    ///     Constructor should throw when serverIdProvider is null.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When ServerIdProvider Is Null")]
    [AllureFeature("Argument Validation")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Test expects exception before object is created")]
    public void ConstructorShouldThrowWhenServerIdProviderIsNull()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<StreamSubscriptionManager> logger = Substitute.For<ILogger<StreamSubscriptionManager>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StreamSubscriptionManager(
            null!,
            clusterClient,
            options,
            logger));
    }

    /// <summary>
    ///     Dispose should be idempotent.
    /// </summary>
    [Fact(DisplayName = "Dispose Is Idempotent")]
    [AllureFeature("Disposal")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP016:Don't use disposed instance",
        Justification = "Testing idempotent disposal behavior")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing explicit Dispose calls for idempotency")]
    public void DisposeShouldBeIdempotent()
    {
        // Arrange
        IServerIdProvider serverIdProvider = CreateServerIdProvider();
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<StreamSubscriptionManager> logger = Substitute.For<ILogger<StreamSubscriptionManager>>();
        StreamSubscriptionManager manager = new(serverIdProvider, clusterClient, options, logger);

        // Act - Dispose multiple times
        manager.Dispose();
        manager.Dispose();
        manager.Dispose();

        // Assert - Should not throw
        Assert.True(true);
    }

    /// <summary>
    ///     EnsureInitializedAsync should throw when hubName is empty.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "EnsureInitializedAsync Throws When HubName Is Empty")]
    [AllureFeature("Argument Validation")]
    public async Task EnsureInitializedAsyncShouldThrowWhenHubNameIsEmpty()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<StreamSubscriptionManager> logger = Substitute.For<ILogger<StreamSubscriptionManager>>();
        using StreamSubscriptionManager manager = new(CreateServerIdProvider(), clusterClient, options, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            manager.EnsureInitializedAsync(string.Empty, _ => Task.CompletedTask, _ => Task.CompletedTask));
    }

    /// <summary>
    ///     EnsureInitializedAsync should throw when hubName is null.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "EnsureInitializedAsync Throws When HubName Is Null")]
    [AllureFeature("Argument Validation")]
    public async Task EnsureInitializedAsyncShouldThrowWhenHubNameIsNull()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<StreamSubscriptionManager> logger = Substitute.For<ILogger<StreamSubscriptionManager>>();
        using StreamSubscriptionManager manager = new(CreateServerIdProvider(), clusterClient, options, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            manager.EnsureInitializedAsync(null!, _ => Task.CompletedTask, _ => Task.CompletedTask));
    }

    /// <summary>
    ///     EnsureInitializedAsync should throw when onAllMessage is null.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "EnsureInitializedAsync Throws When OnAllMessage Is Null")]
    [AllureFeature("Argument Validation")]
    public async Task EnsureInitializedAsyncShouldThrowWhenOnAllMessageIsNull()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<StreamSubscriptionManager> logger = Substitute.For<ILogger<StreamSubscriptionManager>>();
        using StreamSubscriptionManager manager = new(CreateServerIdProvider(), clusterClient, options, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            manager.EnsureInitializedAsync("TestHub", _ => Task.CompletedTask, null!));
    }

    /// <summary>
    ///     EnsureInitializedAsync should throw when onServerMessage is null.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "EnsureInitializedAsync Throws When OnServerMessage Is Null")]
    [AllureFeature("Argument Validation")]
    public async Task EnsureInitializedAsyncShouldThrowWhenOnServerMessageIsNull()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<StreamSubscriptionManager> logger = Substitute.For<ILogger<StreamSubscriptionManager>>();
        using StreamSubscriptionManager manager = new(CreateServerIdProvider(), clusterClient, options, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            manager.EnsureInitializedAsync("TestHub", null!, _ => Task.CompletedTask));
    }

    /// <summary>
    ///     IsInitialized should be false before initialization.
    /// </summary>
    [Fact(DisplayName = "IsInitialized Is False Before Initialization")]
    [AllureFeature("State")]
    public void IsInitializedShouldBeFalseBeforeInitialization()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<StreamSubscriptionManager> logger = Substitute.For<ILogger<StreamSubscriptionManager>>();

        // Act
        using StreamSubscriptionManager manager = new(CreateServerIdProvider(), clusterClient, options, logger);

        // Assert
        Assert.False(manager.IsInitialized);
    }

    /// <summary>
    ///     PublishToAllAsync should throw when message is null.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "PublishToAllAsync Throws When Message Is Null")]
    [AllureFeature("Argument Validation")]
    public async Task PublishToAllAsyncShouldThrowWhenMessageIsNull()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<StreamSubscriptionManager> logger = Substitute.For<ILogger<StreamSubscriptionManager>>();
        using StreamSubscriptionManager manager = new(CreateServerIdProvider(), clusterClient, options, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.PublishToAllAsync(null!));
    }

    /// <summary>
    ///     PublishToAllAsync should throw when stream not initialized.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "PublishToAllAsync Throws When Stream Not Initialized")]
    [AllureFeature("State")]
    public async Task PublishToAllAsyncShouldThrowWhenStreamNotInitialized()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<StreamSubscriptionManager> logger = Substitute.For<ILogger<StreamSubscriptionManager>>();
        using StreamSubscriptionManager manager = new(CreateServerIdProvider(), clusterClient, options, logger);
        AllMessage message = new()
        {
            MethodName = "Test",
            Args = [],
        };

        // Act & Assert
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            manager.PublishToAllAsync(message));
        Assert.Contains("not initialized", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     ServerId should be unique across instances.
    /// </summary>
    [Fact(DisplayName = "ServerId Is Unique Across Instances")]
    [AllureFeature("Construction")]
    public void ServerIdShouldBeUniqueAcrossInstances()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<StreamSubscriptionManager> logger = Substitute.For<ILogger<StreamSubscriptionManager>>();

        // Act
        using StreamSubscriptionManager manager1 = new(CreateServerIdProvider(), clusterClient, options, logger);
        using StreamSubscriptionManager manager2 = new(CreateServerIdProvider(), clusterClient, options, logger);

        // Assert
        Assert.NotEqual(manager1.ServerId, manager2.ServerId);
    }
}