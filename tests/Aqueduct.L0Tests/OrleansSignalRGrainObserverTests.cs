using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.L0Tests.Infrastructure;

using NSubstitute;

using Orleans;


namespace Mississippi.Aqueduct.L0Tests;

/// <summary>
///     Tests for <see cref="OrleansSignalRGrainObserver" /> operations.
/// </summary>
[AllureParentSuite("ASP.NET Core")]
[AllureSuite("SignalR Orleans")]
[AllureSubSuite("Grain Observer")]
[Collection(ClusterTestSuite.Name)]
public sealed class OrleansSignalRGrainObserverTests
{
    /// <summary>
    ///     Tests that constructor succeeds with valid parameters.
    /// </summary>
    [Fact(DisplayName = "Constructor Succeeds With Valid Parameters")]
    public void ConstructorShouldSucceedWithValidParameters()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<OrleansSignalROptions> options = Options.Create(new OrleansSignalROptions());
        ILogger<OrleansSignalRGrainObserver> logger = Substitute.For<ILogger<OrleansSignalRGrainObserver>>();

        // Act
        OrleansSignalRGrainObserver observer = new(clusterClient, options, logger);

        // Assert
        Assert.NotNull(observer);
    }

    /// <summary>
    ///     Tests that constructor throws when cluster client is null.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When ClusterClient Is Null")]
    public void ConstructorShouldThrowWhenClusterClientIsNull()
    {
        // Arrange
        IOptions<OrleansSignalROptions> options = Options.Create(new OrleansSignalROptions());
        ILogger<OrleansSignalRGrainObserver> logger = Substitute.For<ILogger<OrleansSignalRGrainObserver>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OrleansSignalRGrainObserver(null!, options, logger));
    }

    /// <summary>
    ///     Tests that constructor throws when logger is null.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When Logger Is Null")]
    public void ConstructorShouldThrowWhenLoggerIsNull()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<OrleansSignalROptions> options = Options.Create(new OrleansSignalROptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OrleansSignalRGrainObserver(clusterClient, options, null!));
    }

    /// <summary>
    ///     Tests that constructor throws when options is null.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When Options Is Null")]
    public void ConstructorShouldThrowWhenOptionsIsNull()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        ILogger<OrleansSignalRGrainObserver> logger = Substitute.For<ILogger<OrleansSignalRGrainObserver>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OrleansSignalRGrainObserver(clusterClient, null!, logger));
    }

    /// <summary>
    ///     Tests that SendToAllAsync throws when hub name is empty.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "SendToAllAsync Throws When HubName Is Empty")]
    public async Task SendToAllAsyncShouldThrowWhenHubNameIsEmpty()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<OrleansSignalROptions> options = Options.Create(new OrleansSignalROptions());
        ILogger<OrleansSignalRGrainObserver> logger = Substitute.For<ILogger<OrleansSignalRGrainObserver>>();
        OrleansSignalRGrainObserver observer = new(clusterClient, options, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => observer.SendToAllAsync(string.Empty, "method", []));
    }

    /// <summary>
    ///     Tests that SendToAllAsync throws when hub name is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "SendToAllAsync Throws When HubName Is Null")]
    public async Task SendToAllAsyncShouldThrowWhenHubNameIsNull()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<OrleansSignalROptions> options = Options.Create(new OrleansSignalROptions());
        ILogger<OrleansSignalRGrainObserver> logger = Substitute.For<ILogger<OrleansSignalRGrainObserver>>();
        OrleansSignalRGrainObserver observer = new(clusterClient, options, logger);

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() => observer.SendToAllAsync(
            null!,
            "method",
            ImmutableArray<object?>.Empty));
    }

    /// <summary>
    ///     Tests that SendToAllAsync throws when method is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "SendToAllAsync Throws When Method Is Null")]
    public async Task SendToAllAsyncShouldThrowWhenMethodIsNull()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<OrleansSignalROptions> options = Options.Create(new OrleansSignalROptions());
        ILogger<OrleansSignalRGrainObserver> logger = Substitute.For<ILogger<OrleansSignalRGrainObserver>>();
        OrleansSignalRGrainObserver observer = new(clusterClient, options, logger);

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() => observer.SendToAllAsync(
            "TestHub",
            null!,
            ImmutableArray<object?>.Empty));
    }

    /// <summary>
    ///     Tests that SendToConnectionAsync calls client grain.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "SendToConnectionAsync Calls Client Grain")]
    public async Task SendToConnectionAsyncShouldCallClientGrain()
    {
        // Arrange
        ISignalRClientGrain mockClientGrain = Substitute.For<ISignalRClientGrain>();
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        clusterClient.GetGrain<ISignalRClientGrain>("TestHub:conn1").Returns(mockClientGrain);
        IOptions<OrleansSignalROptions> options = Options.Create(new OrleansSignalROptions());
        ILogger<OrleansSignalRGrainObserver> logger = Substitute.For<ILogger<OrleansSignalRGrainObserver>>();
        OrleansSignalRGrainObserver observer = new(clusterClient, options, logger);

        // Act
        await observer.SendToConnectionAsync("TestHub", "conn1", "Notify", ImmutableArray.Create<object?>("arg1"));

        // Assert
        await mockClientGrain.Received(1).SendMessageAsync("Notify", Arg.Any<ImmutableArray<object?>>());
    }

    /// <summary>
    ///     Tests that SendToConnectionAsync throws when connection ID is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "SendToConnectionAsync Throws When ConnectionId Is Null")]
    public async Task SendToConnectionAsyncShouldThrowWhenConnectionIdIsNull()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<OrleansSignalROptions> options = Options.Create(new OrleansSignalROptions());
        ILogger<OrleansSignalRGrainObserver> logger = Substitute.For<ILogger<OrleansSignalRGrainObserver>>();
        OrleansSignalRGrainObserver observer = new(clusterClient, options, logger);

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            observer.SendToConnectionAsync("TestHub", null!, "method", ImmutableArray<object?>.Empty));
    }

    /// <summary>
    ///     Tests that SendToConnectionAsync throws when hub name is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "SendToConnectionAsync Throws When HubName Is Null")]
    public async Task SendToConnectionAsyncShouldThrowWhenHubNameIsNull()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<OrleansSignalROptions> options = Options.Create(new OrleansSignalROptions());
        ILogger<OrleansSignalRGrainObserver> logger = Substitute.For<ILogger<OrleansSignalRGrainObserver>>();
        OrleansSignalRGrainObserver observer = new(clusterClient, options, logger);

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            observer.SendToConnectionAsync(null!, "conn1", "method", ImmutableArray<object?>.Empty));
    }

    /// <summary>
    ///     Tests that SendToConnectionAsync throws when method is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "SendToConnectionAsync Throws When Method Is Null")]
    public async Task SendToConnectionAsyncShouldThrowWhenMethodIsNull()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<OrleansSignalROptions> options = Options.Create(new OrleansSignalROptions());
        ILogger<OrleansSignalRGrainObserver> logger = Substitute.For<ILogger<OrleansSignalRGrainObserver>>();
        OrleansSignalRGrainObserver observer = new(clusterClient, options, logger);

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            observer.SendToConnectionAsync("TestHub", "conn1", null!, ImmutableArray<object?>.Empty));
    }

    /// <summary>
    ///     Tests that SendToGroupAsync calls group grain.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "SendToGroupAsync Calls Group Grain")]
    public async Task SendToGroupAsyncShouldCallGroupGrain()
    {
        // Arrange
        ISignalRGroupGrain mockGroupGrain = Substitute.For<ISignalRGroupGrain>();
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        clusterClient.GetGrain<ISignalRGroupGrain>("TestHub:group1").Returns(mockGroupGrain);
        IOptions<OrleansSignalROptions> options = Options.Create(new OrleansSignalROptions());
        ILogger<OrleansSignalRGrainObserver> logger = Substitute.For<ILogger<OrleansSignalRGrainObserver>>();
        OrleansSignalRGrainObserver observer = new(clusterClient, options, logger);

        // Act
        await observer.SendToGroupAsync("TestHub", "group1", "Notify", ImmutableArray.Create<object?>("arg1"));

        // Assert
        await mockGroupGrain.Received(1).SendMessageAsync("Notify", Arg.Any<ImmutableArray<object?>>());
    }

    /// <summary>
    ///     Tests that SendToGroupAsync throws when group name is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "SendToGroupAsync Throws When GroupName Is Null")]
    public async Task SendToGroupAsyncShouldThrowWhenGroupNameIsNull()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<OrleansSignalROptions> options = Options.Create(new OrleansSignalROptions());
        ILogger<OrleansSignalRGrainObserver> logger = Substitute.For<ILogger<OrleansSignalRGrainObserver>>();
        OrleansSignalRGrainObserver observer = new(clusterClient, options, logger);

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() => observer.SendToGroupAsync(
            "TestHub",
            null!,
            "method",
            ImmutableArray<object?>.Empty));
    }

    /// <summary>
    ///     Tests that SendToGroupAsync throws when hub name is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "SendToGroupAsync Throws When HubName Is Null")]
    public async Task SendToGroupAsyncShouldThrowWhenHubNameIsNull()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<OrleansSignalROptions> options = Options.Create(new OrleansSignalROptions());
        ILogger<OrleansSignalRGrainObserver> logger = Substitute.For<ILogger<OrleansSignalRGrainObserver>>();
        OrleansSignalRGrainObserver observer = new(clusterClient, options, logger);

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() => observer.SendToGroupAsync(
            null!,
            "group1",
            "method",
            ImmutableArray<object?>.Empty));
    }

    /// <summary>
    ///     Tests that SendToGroupAsync throws when method is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "SendToGroupAsync Throws When Method Is Null")]
    public async Task SendToGroupAsyncShouldThrowWhenMethodIsNull()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<OrleansSignalROptions> options = Options.Create(new OrleansSignalROptions());
        ILogger<OrleansSignalRGrainObserver> logger = Substitute.For<ILogger<OrleansSignalRGrainObserver>>();
        OrleansSignalRGrainObserver observer = new(clusterClient, options, logger);

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() => observer.SendToGroupAsync(
            "TestHub",
            "group1",
            null!,
            ImmutableArray<object?>.Empty));
    }
}