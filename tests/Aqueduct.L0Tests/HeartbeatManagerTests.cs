using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;

using NSubstitute;


namespace Mississippi.Aqueduct.L0Tests;

/// <summary>
///     Tests for <see cref="HeartbeatManager" />.
/// </summary>
[AllureParentSuite("Aqueduct")]
[AllureSuite("Core")]
[AllureSubSuite("HeartbeatManager")]
public sealed class HeartbeatManagerTests
{
    /// <summary>
    ///     Constructor should succeed with valid dependencies.
    /// </summary>
    [Fact(DisplayName = "Constructor Succeeds With Valid Dependencies")]
    [AllureFeature("Construction")]
    public void ConstructorShouldSucceedWithValidDependencies()
    {
        // Arrange
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<HeartbeatManager> logger = Substitute.For<ILogger<HeartbeatManager>>();

        // Act
        using HeartbeatManager manager = new(grainFactory, options, logger);

        // Assert
        Assert.NotNull(manager);
        Assert.NotNull(manager.ServerId);
        Assert.Equal(32, manager.ServerId.Length); // GUID without hyphens
    }

    /// <summary>
    ///     Constructor should throw when grainFactory is null.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When GrainFactory Is Null")]
    [AllureFeature("Argument Validation")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Test expects exception before object is created")]
    public void ConstructorShouldThrowWhenGrainFactoryIsNull()
    {
        // Arrange
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<HeartbeatManager> logger = Substitute.For<ILogger<HeartbeatManager>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HeartbeatManager(null!, options, logger));
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
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HeartbeatManager(grainFactory, options, null!));
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
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        ILogger<HeartbeatManager> logger = Substitute.For<ILogger<HeartbeatManager>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HeartbeatManager(grainFactory, null!, logger));
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
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        ISignalRServerDirectoryGrain directoryGrain = Substitute.For<ISignalRServerDirectoryGrain>();
        grainFactory.GetServerDirectoryGrain().Returns(directoryGrain);
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<HeartbeatManager> logger = Substitute.For<ILogger<HeartbeatManager>>();
        HeartbeatManager manager = new(grainFactory, options, logger);

        // Act - Dispose multiple times
        manager.Dispose();
        manager.Dispose();
        manager.Dispose();

        // Assert - Should not throw
        Assert.True(true);
    }

    /// <summary>
    ///     Dispose should unregister server from directory.
    /// </summary>
    [Fact(DisplayName = "Dispose Unregisters Server From Directory")]
    [AllureFeature("Disposal")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing explicit Dispose behavior")]
    public void DisposeShouldUnregisterServerFromDirectory()
    {
        // Arrange
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        ISignalRServerDirectoryGrain directoryGrain = Substitute.For<ISignalRServerDirectoryGrain>();
        grainFactory.GetServerDirectoryGrain().Returns(directoryGrain);
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<HeartbeatManager> logger = Substitute.For<ILogger<HeartbeatManager>>();
        HeartbeatManager manager = new(grainFactory, options, logger);
        string serverId = manager.ServerId;

        // Act
        manager.Dispose();

        // Assert - Verify unregistration was called (fire-and-forget)
        _ = directoryGrain.Received(1).UnregisterServerAsync(serverId);
    }

    /// <summary>
    ///     ServerId should be unique across instances.
    /// </summary>
    [Fact(DisplayName = "ServerId Is Unique Across Instances")]
    [AllureFeature("Construction")]
    public void ServerIdShouldBeUniqueAcrossInstances()
    {
        // Arrange
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        ISignalRServerDirectoryGrain directoryGrain = Substitute.For<ISignalRServerDirectoryGrain>();
        grainFactory.GetServerDirectoryGrain().Returns(directoryGrain);
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<HeartbeatManager> logger = Substitute.For<ILogger<HeartbeatManager>>();

        // Act
        using HeartbeatManager manager1 = new(grainFactory, options, logger);
        using HeartbeatManager manager2 = new(grainFactory, options, logger);

        // Assert
        Assert.NotEqual(manager1.ServerId, manager2.ServerId);
    }

    /// <summary>
    ///     StartAsync should be idempotent.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "StartAsync Is Idempotent")]
    [AllureFeature("Lifecycle")]
    public async Task StartAsyncShouldBeIdempotent()
    {
        // Arrange
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        ISignalRServerDirectoryGrain directoryGrain = Substitute.For<ISignalRServerDirectoryGrain>();
        grainFactory.GetServerDirectoryGrain().Returns(directoryGrain);
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<HeartbeatManager> logger = Substitute.For<ILogger<HeartbeatManager>>();
        using HeartbeatManager manager = new(grainFactory, options, logger);
        Func<int> connectionCountProvider = () => 5;

        // Act - Start multiple times
        await manager.StartAsync(connectionCountProvider);
        await manager.StartAsync(connectionCountProvider);

        // Assert - RegisterServerAsync should only be called once
        await directoryGrain.Received(1).RegisterServerAsync(manager.ServerId);
    }

    /// <summary>
    ///     StartAsync should register server with directory.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "StartAsync Registers Server With Directory")]
    [AllureFeature("Lifecycle")]
    public async Task StartAsyncShouldRegisterServerWithDirectory()
    {
        // Arrange
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        ISignalRServerDirectoryGrain directoryGrain = Substitute.For<ISignalRServerDirectoryGrain>();
        grainFactory.GetServerDirectoryGrain().Returns(directoryGrain);
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<HeartbeatManager> logger = Substitute.For<ILogger<HeartbeatManager>>();
        using HeartbeatManager manager = new(grainFactory, options, logger);
        string serverId = manager.ServerId;
        Func<int> connectionCountProvider = () => 5;

        // Act
        await manager.StartAsync(connectionCountProvider);

        // Assert
        await directoryGrain.Received(1).RegisterServerAsync(serverId);
    }

    /// <summary>
    ///     StartAsync should throw when connectionCountProvider is null.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "StartAsync Throws When ConnectionCountProvider Is Null")]
    [AllureFeature("Argument Validation")]
    public async Task StartAsyncShouldThrowWhenConnectionCountProviderIsNull()
    {
        // Arrange
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<HeartbeatManager> logger = Substitute.For<ILogger<HeartbeatManager>>();
        using HeartbeatManager manager = new(grainFactory, options, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.StartAsync(null!));
    }

    /// <summary>
    ///     StopAsync should unregister server from directory.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "StopAsync Unregisters Server From Directory")]
    [AllureFeature("Lifecycle")]
    public async Task StopAsyncShouldUnregisterServerFromDirectory()
    {
        // Arrange
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        ISignalRServerDirectoryGrain directoryGrain = Substitute.For<ISignalRServerDirectoryGrain>();
        grainFactory.GetServerDirectoryGrain().Returns(directoryGrain);
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<HeartbeatManager> logger = Substitute.For<ILogger<HeartbeatManager>>();
        using HeartbeatManager manager = new(grainFactory, options, logger);
        string serverId = manager.ServerId;
        await manager.StartAsync(() => 5);

        // Act
        await manager.StopAsync();

        // Assert
        await directoryGrain.Received(1).UnregisterServerAsync(serverId);
    }
}