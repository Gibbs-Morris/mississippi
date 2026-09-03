using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Abstractions.Messages;
using Mississippi.Testing.Utilities.SignalR;

using NSubstitute;


namespace Mississippi.Aqueduct.Gateway.L0Tests;

/// <summary>
///     Tests for <see cref="AqueductHubLifetimeManager{THub}" />.
/// </summary>
public sealed class AqueductHubLifetimeManagerTests
{
    private static AqueductHubLifetimeManager<TestAqueductHub> CreateManager(
        IServerIdProvider? serverIdProvider = null,
        IAqueductGrainFactory? grainFactory = null,
        IConnectionRegistry? connectionRegistry = null,
        ILocalMessageSender? messageSender = null,
        IHeartbeatManager? heartbeatManager = null,
        IStreamSubscriptionManager? streamSubscriptionManager = null,
        IHostApplicationLifetime? hostApplicationLifetime = null,
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>>? logger = null
    ) =>
        new(
            serverIdProvider ?? CreateServerIdProvider(),
            grainFactory ?? Substitute.For<IAqueductGrainFactory>(),
            connectionRegistry ?? Substitute.For<IConnectionRegistry>(),
            messageSender ?? Substitute.For<ILocalMessageSender>(),
            heartbeatManager ?? Substitute.For<IHeartbeatManager>(),
            streamSubscriptionManager ?? Substitute.For<IStreamSubscriptionManager>(),
            hostApplicationLifetime ?? Substitute.For<IHostApplicationLifetime>(),
            logger ?? NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance);

    private static IServerIdProvider CreateServerIdProvider(
        string? serverId = null
    )
    {
        IServerIdProvider provider = Substitute.For<IServerIdProvider>();
        provider.ServerId.Returns(serverId ?? Guid.NewGuid().ToString("N"));
        return provider;
    }

    /// <summary>
    ///     AddToGroupAsync should call group grain.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "AddToGroupAsync Calls Group Grain")]
    public async Task AddToGroupAsyncShouldCallGroupGrain()
    {
        // Arrange
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        ISignalRGroupGrain groupGrain = Substitute.For<ISignalRGroupGrain>();
        grainFactory.GetGroupGrain("TestAqueductHub", "group1").Returns(groupGrain);
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(grainFactory: grainFactory);

        // Act
        await manager.AddToGroupAsync("conn1", "group1");

        // Assert
        await groupGrain.Received(1).AddConnectionAsync("conn1");
    }

    /// <summary>
    ///     AddToGroupAsync should throw when connectionId is empty.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "AddToGroupAsync Throws When ConnectionId Is Empty")]
    public async Task AddToGroupAsyncShouldThrowWhenConnectionIdIsEmpty()
    {
        // Arrange
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => manager.AddToGroupAsync(string.Empty, "group1"));
    }

    /// <summary>
    ///     AddToGroupAsync should throw when groupName is empty.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "AddToGroupAsync Throws When GroupName Is Empty")]
    public async Task AddToGroupAsyncShouldThrowWhenGroupNameIsEmpty()
    {
        // Arrange
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => manager.AddToGroupAsync("conn1", string.Empty));
    }

    /// <summary>
    ///     Constructor should succeed with valid dependencies.
    /// </summary>
    [Fact(DisplayName = "Constructor Succeeds With Valid Dependencies")]
    public void ConstructorShouldSucceedWithValidDependencies()
    {
        // Arrange
        IServerIdProvider serverIdProvider = CreateServerIdProvider();
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        IConnectionRegistry connectionRegistry = Substitute.For<IConnectionRegistry>();
        ILocalMessageSender messageSender = Substitute.For<ILocalMessageSender>();
        IHeartbeatManager heartbeatManager = Substitute.For<IHeartbeatManager>();
        IStreamSubscriptionManager streamSubscriptionManager = Substitute.For<IStreamSubscriptionManager>();
        IHostApplicationLifetime hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>> logger =
            NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance;

        // Act
        using AqueductHubLifetimeManager<TestAqueductHub> manager = new(
            serverIdProvider,
            grainFactory,
            connectionRegistry,
            messageSender,
            heartbeatManager,
            streamSubscriptionManager,
            hostApplicationLifetime,
            logger);

        // Assert
        Assert.NotNull(manager);
    }

    /// <summary>
    ///     Constructor should throw when connectionRegistry is null.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When ConnectionRegistry Is Null")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Test expects exception before object is created")]
    public void ConstructorShouldThrowWhenConnectionRegistryIsNull()
    {
        // Arrange
        IServerIdProvider serverIdProvider = CreateServerIdProvider();
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        ILocalMessageSender messageSender = Substitute.For<ILocalMessageSender>();
        IHeartbeatManager heartbeatManager = Substitute.For<IHeartbeatManager>();
        IStreamSubscriptionManager streamSubscriptionManager = Substitute.For<IStreamSubscriptionManager>();
        IHostApplicationLifetime hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>> logger =
            NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AqueductHubLifetimeManager<TestAqueductHub>(
            serverIdProvider,
            grainFactory,
            null!,
            messageSender,
            heartbeatManager,
            streamSubscriptionManager,
            hostApplicationLifetime,
            logger));
    }

    /// <summary>
    ///     Constructor should throw when grainFactory is null.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When GrainFactory Is Null")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Test expects exception before object is created")]
    public void ConstructorShouldThrowWhenGrainFactoryIsNull()
    {
        // Arrange
        IServerIdProvider serverIdProvider = CreateServerIdProvider();
        IConnectionRegistry connectionRegistry = Substitute.For<IConnectionRegistry>();
        ILocalMessageSender messageSender = Substitute.For<ILocalMessageSender>();
        IHeartbeatManager heartbeatManager = Substitute.For<IHeartbeatManager>();
        IStreamSubscriptionManager streamSubscriptionManager = Substitute.For<IStreamSubscriptionManager>();
        IHostApplicationLifetime hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>> logger =
            NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AqueductHubLifetimeManager<TestAqueductHub>(
            serverIdProvider,
            null!,
            connectionRegistry,
            messageSender,
            heartbeatManager,
            streamSubscriptionManager,
            hostApplicationLifetime,
            logger));
    }

    /// <summary>
    ///     Constructor should throw when heartbeatManager is null.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When HeartbeatManager Is Null")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Test expects exception before object is created")]
    public void ConstructorShouldThrowWhenHeartbeatManagerIsNull()
    {
        // Arrange
        IServerIdProvider serverIdProvider = CreateServerIdProvider();
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        IConnectionRegistry connectionRegistry = Substitute.For<IConnectionRegistry>();
        ILocalMessageSender messageSender = Substitute.For<ILocalMessageSender>();
        IStreamSubscriptionManager streamSubscriptionManager = Substitute.For<IStreamSubscriptionManager>();
        IHostApplicationLifetime hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>> logger =
            NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AqueductHubLifetimeManager<TestAqueductHub>(
            serverIdProvider,
            grainFactory,
            connectionRegistry,
            messageSender,
            null!,
            streamSubscriptionManager,
            hostApplicationLifetime,
            logger));
    }

    /// <summary>
    ///     Constructor should throw when hostApplicationLifetime is null.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When Host Application Lifetime Is Null")]
    public void ConstructorShouldThrowWhenHostApplicationLifetimeIsNull()
    {
        // Arrange
        IServerIdProvider serverIdProvider = CreateServerIdProvider();
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        IConnectionRegistry connectionRegistry = Substitute.For<IConnectionRegistry>();
        ILocalMessageSender messageSender = Substitute.For<ILocalMessageSender>();
        IHeartbeatManager heartbeatManager = Substitute.For<IHeartbeatManager>();
        IStreamSubscriptionManager streamSubscriptionManager = Substitute.For<IStreamSubscriptionManager>();
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>> logger =
            NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AqueductHubLifetimeManager<TestAqueductHub>(
            serverIdProvider,
            grainFactory,
            connectionRegistry,
            messageSender,
            heartbeatManager,
            streamSubscriptionManager,
            null!,
            logger).Dispose());
    }

    /// <summary>
    ///     Constructor should throw when logger is null.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When Logger Is Null")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Test expects exception before object is created")]
    public void ConstructorShouldThrowWhenLoggerIsNull()
    {
        // Arrange
        IServerIdProvider serverIdProvider = CreateServerIdProvider();
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        IConnectionRegistry connectionRegistry = Substitute.For<IConnectionRegistry>();
        ILocalMessageSender messageSender = Substitute.For<ILocalMessageSender>();
        IHeartbeatManager heartbeatManager = Substitute.For<IHeartbeatManager>();
        IStreamSubscriptionManager streamSubscriptionManager = Substitute.For<IStreamSubscriptionManager>();
        IHostApplicationLifetime hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AqueductHubLifetimeManager<TestAqueductHub>(
            serverIdProvider,
            grainFactory,
            connectionRegistry,
            messageSender,
            heartbeatManager,
            streamSubscriptionManager,
            hostApplicationLifetime,
            null!));
    }

    /// <summary>
    ///     Constructor should throw when messageSender is null.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When MessageSender Is Null")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Test expects exception before object is created")]
    public void ConstructorShouldThrowWhenMessageSenderIsNull()
    {
        // Arrange
        IServerIdProvider serverIdProvider = CreateServerIdProvider();
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        IConnectionRegistry connectionRegistry = Substitute.For<IConnectionRegistry>();
        IHeartbeatManager heartbeatManager = Substitute.For<IHeartbeatManager>();
        IStreamSubscriptionManager streamSubscriptionManager = Substitute.For<IStreamSubscriptionManager>();
        IHostApplicationLifetime hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>> logger =
            NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AqueductHubLifetimeManager<TestAqueductHub>(
            serverIdProvider,
            grainFactory,
            connectionRegistry,
            null!,
            heartbeatManager,
            streamSubscriptionManager,
            hostApplicationLifetime,
            logger));
    }

    /// <summary>
    ///     Constructor should throw when serverIdProvider is null.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When ServerIdProvider Is Null")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Test expects exception before object is created")]
    public void ConstructorShouldThrowWhenServerIdProviderIsNull()
    {
        // Arrange
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        IConnectionRegistry connectionRegistry = Substitute.For<IConnectionRegistry>();
        ILocalMessageSender messageSender = Substitute.For<ILocalMessageSender>();
        IHeartbeatManager heartbeatManager = Substitute.For<IHeartbeatManager>();
        IStreamSubscriptionManager streamSubscriptionManager = Substitute.For<IStreamSubscriptionManager>();
        IHostApplicationLifetime hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>> logger =
            NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AqueductHubLifetimeManager<TestAqueductHub>(
            null!,
            grainFactory,
            connectionRegistry,
            messageSender,
            heartbeatManager,
            streamSubscriptionManager,
            hostApplicationLifetime,
            logger));
    }

    /// <summary>
    ///     Constructor should throw when streamSubscriptionManager is null.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When StreamSubscriptionManager Is Null")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Test expects exception before object is created")]
    public void ConstructorShouldThrowWhenStreamSubscriptionManagerIsNull()
    {
        // Arrange
        IServerIdProvider serverIdProvider = CreateServerIdProvider();
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        IConnectionRegistry connectionRegistry = Substitute.For<IConnectionRegistry>();
        ILocalMessageSender messageSender = Substitute.For<ILocalMessageSender>();
        IHeartbeatManager heartbeatManager = Substitute.For<IHeartbeatManager>();
        IHostApplicationLifetime hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>> logger =
            NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AqueductHubLifetimeManager<TestAqueductHub>(
            serverIdProvider,
            grainFactory,
            connectionRegistry,
            messageSender,
            heartbeatManager,
            null!,
            hostApplicationLifetime,
            logger));
    }

    /// <summary>
    ///     OnAllMessageAsync should continue when a connection aborts during delivery.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "OnAllMessageAsync Continues After Connection Aborts")]
    public async Task OnAllMessageAsyncShouldContinueAfterConnectionAborts()
    {
        // Arrange
        CancellationToken abortedConnectionToken = new(true);
        HubConnectionContext abortedConnection = HubConnectionContextFactory.Create(
            "aborted",
            connectionAborted: abortedConnectionToken);
        Assert.True(abortedConnection.ConnectionAborted.IsCancellationRequested);
        HubConnectionContext healthyConnection = HubConnectionContextFactory.Create("healthy");
        IConnectionRegistry connectionRegistry = Substitute.For<IConnectionRegistry>();
        connectionRegistry.GetAll().Returns([abortedConnection, healthyConnection]);
        ILocalMessageSender messageSender = Substitute.For<ILocalMessageSender>();
        object?[] args = [];
        messageSender.SendAsync(
                abortedConnection,
                "MethodName",
                Arg.Any<IReadOnlyList<object?>>(),
                abortedConnection.ConnectionAborted)
            .Returns(Task.FromCanceled(abortedConnection.ConnectionAborted));
        IStreamSubscriptionManager streamSubscriptionManager = Substitute.For<IStreamSubscriptionManager>();
        Func<AllMessage, Task>? onAllMessage = null;
        streamSubscriptionManager.EnsureInitializedAsync(
                Arg.Any<string>(),
                Arg.Any<Func<ServerMessage, Task>>(),
                Arg.Do<Func<AllMessage, Task>>(callback => onAllMessage = callback),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        IHeartbeatManager heartbeatManager = Substitute.For<IHeartbeatManager>();
        heartbeatManager.StartAsync(Arg.Any<Func<int>>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(
            connectionRegistry: connectionRegistry,
            heartbeatManager: heartbeatManager,
            messageSender: messageSender,
            streamSubscriptionManager: streamSubscriptionManager);

        // Act
        await manager.SendAllAsync("MethodName", args);
        Func<AllMessage, Task> callback = onAllMessage ??
                                          throw new InvalidOperationException("All-message callback was not captured.");
        await callback(
            new()
            {
                MethodName = "MethodName",
                Args = args,
            });

        // Assert
        await messageSender.Received(1)
            .SendAsync(healthyConnection, "MethodName", args, healthyConnection.ConnectionAborted);
    }

    /// <summary>
    ///     OnConnectedAsync should throw when connection is null.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "OnConnectedAsync Throws When Connection Is Null")]
    public async Task OnConnectedAsyncShouldThrowWhenConnectionIsNull()
    {
        // Arrange
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.OnConnectedAsync(null!));
    }

    /// <summary>
    ///     OnConnectedAsync should use the host application stopping token for stream setup.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "OnConnectedAsync Uses Host Application Stopping Token")]
    public async Task OnConnectedAsyncShouldUseHostApplicationStoppingToken()
    {
        // Arrange
        using CancellationTokenSource applicationStoppingSource = new();
        CancellationToken applicationStopping = applicationStoppingSource.Token;
        IHostApplicationLifetime hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        hostApplicationLifetime.ApplicationStopping.Returns(applicationStopping);
        IStreamSubscriptionManager streamSubscriptionManager = Substitute.For<IStreamSubscriptionManager>();
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        ISignalRClientGrain clientGrain = Substitute.For<ISignalRClientGrain>();
        grainFactory.GetClientGrain("TestAqueductHub", "conn1").Returns(clientGrain);
        HubConnectionContext connection = HubConnectionContextFactory.Create("conn1");
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(
            grainFactory: grainFactory,
            hostApplicationLifetime: hostApplicationLifetime,
            streamSubscriptionManager: streamSubscriptionManager);

        // Act
        await manager.OnConnectedAsync(connection);

        // Assert
        await streamSubscriptionManager.Received(1)
            .EnsureInitializedAsync(
                "TestAqueductHub",
                Arg.Any<Func<ServerMessage, Task>>(),
                Arg.Any<Func<AllMessage, Task>>(),
                applicationStopping);
    }

    /// <summary>
    ///     OnDisconnectedAsync should throw when connection is null.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "OnDisconnectedAsync Throws When Connection Is Null")]
    public async Task OnDisconnectedAsyncShouldThrowWhenConnectionIsNull()
    {
        // Arrange
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.OnDisconnectedAsync(null!));
    }

    /// <summary>
    ///     OnServerMessageAsync should ignore cancellation after a connection aborts.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "OnServerMessageAsync Ignores Connection Abort")]
    public async Task OnServerMessageAsyncShouldIgnoreConnectionAbort()
    {
        // Arrange
        using CancellationTokenSource abortedConnectionSource = new();
        HubConnectionContext abortedConnection = HubConnectionContextFactory.Create(
            "aborted",
            connectionAborted: abortedConnectionSource.Token);
        IConnectionRegistry connectionRegistry = Substitute.For<IConnectionRegistry>();
        connectionRegistry.GetConnection("aborted").Returns(abortedConnection);
        ILocalMessageSender messageSender = Substitute.For<ILocalMessageSender>();
        object?[] args = [];
        TaskCompletionSource sendCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        messageSender.SendAsync(
                abortedConnection,
                "MethodName",
                args,
                abortedConnection.ConnectionAborted)
            .Returns(sendCompletion.Task);
        IStreamSubscriptionManager streamSubscriptionManager = Substitute.For<IStreamSubscriptionManager>();
        Func<ServerMessage, Task>? onServerMessage = null;
        streamSubscriptionManager.EnsureInitializedAsync(
                Arg.Any<string>(),
                Arg.Do<Func<ServerMessage, Task>>(callback => onServerMessage = callback),
                Arg.Any<Func<AllMessage, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        IHeartbeatManager heartbeatManager = Substitute.For<IHeartbeatManager>();
        heartbeatManager.StartAsync(Arg.Any<Func<int>>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(
            connectionRegistry: connectionRegistry,
            heartbeatManager: heartbeatManager,
            messageSender: messageSender,
            streamSubscriptionManager: streamSubscriptionManager);

        // Act
        await manager.SendAllAsync("MethodName", args);
        Func<ServerMessage, Task> callback = onServerMessage ??
                                             throw new InvalidOperationException(
                                                 "Server-message callback was not captured.");
        Task callbackTask = callback(
            new()
            {
                ConnectionId = "aborted",
                MethodName = "MethodName",
                Args = args,
            });

        // Complete the send as the connection aborts, so the cancellation filter handles the write cancellation.
        await abortedConnectionSource.CancelAsync();
        sendCompletion.SetCanceled(abortedConnectionSource.Token);
        await callbackTask;

        // Assert
        await messageSender.Received(1)
            .SendAsync(abortedConnection, "MethodName", args, abortedConnection.ConnectionAborted);
    }

    /// <summary>
    ///     OnServerMessageAsync should propagate cancellation unrelated to the connection.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "OnServerMessageAsync Propagates Unrelated Cancellation")]
    public async Task OnServerMessageAsyncShouldPropagateUnrelatedCancellation()
    {
        // Arrange
        HubConnectionContext connection = HubConnectionContextFactory.Create("connection");
        IConnectionRegistry connectionRegistry = Substitute.For<IConnectionRegistry>();
        connectionRegistry.GetConnection("connection").Returns(connection);
        ILocalMessageSender messageSender = Substitute.For<ILocalMessageSender>();
        object?[] args = [];
        messageSender.SendAsync(connection, "MethodName", args, connection.ConnectionAborted)
            .Returns(Task.FromCanceled(new CancellationToken(true)));
        IStreamSubscriptionManager streamSubscriptionManager = Substitute.For<IStreamSubscriptionManager>();
        Func<ServerMessage, Task>? onServerMessage = null;
        streamSubscriptionManager.EnsureInitializedAsync(
                Arg.Any<string>(),
                Arg.Do<Func<ServerMessage, Task>>(callback => onServerMessage = callback),
                Arg.Any<Func<AllMessage, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        IHeartbeatManager heartbeatManager = Substitute.For<IHeartbeatManager>();
        heartbeatManager.StartAsync(Arg.Any<Func<int>>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(
            connectionRegistry: connectionRegistry,
            heartbeatManager: heartbeatManager,
            messageSender: messageSender,
            streamSubscriptionManager: streamSubscriptionManager);

        // Act
        await manager.SendAllAsync("MethodName", args);
        Func<ServerMessage, Task> callback = onServerMessage ??
                                             throw new InvalidOperationException(
                                                 "Server-message callback was not captured.");

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => callback(
            new()
            {
                ConnectionId = "connection",
                MethodName = "MethodName",
                Args = args,
            }));
    }

    /// <summary>
    ///     RemoveFromGroupAsync should call group grain.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "RemoveFromGroupAsync Calls Group Grain")]
    public async Task RemoveFromGroupAsyncShouldCallGroupGrain()
    {
        // Arrange
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        ISignalRGroupGrain groupGrain = Substitute.For<ISignalRGroupGrain>();
        grainFactory.GetGroupGrain("TestAqueductHub", "group1").Returns(groupGrain);
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(grainFactory: grainFactory);

        // Act
        await manager.RemoveFromGroupAsync("conn1", "group1");

        // Assert
        await groupGrain.Received(1).RemoveConnectionAsync("conn1");
    }

    /// <summary>
    ///     RemoveFromGroupAsync should throw when connectionId is empty.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "RemoveFromGroupAsync Throws When ConnectionId Is Empty")]
    public async Task RemoveFromGroupAsyncShouldThrowWhenConnectionIdIsEmpty()
    {
        // Arrange
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => manager.RemoveFromGroupAsync(string.Empty, "group1"));
    }

    /// <summary>
    ///     RemoveFromGroupAsync should throw when groupName is empty.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "RemoveFromGroupAsync Throws When GroupName Is Empty")]
    public async Task RemoveFromGroupAsyncShouldThrowWhenGroupNameIsEmpty()
    {
        // Arrange
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => manager.RemoveFromGroupAsync("conn1", string.Empty));
    }

    /// <summary>
    ///     SendConnectionAsync should route via client grain if not local.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendConnectionAsync Routes Via Client Grain If Not Local")]
    public async Task SendConnectionAsyncShouldRouteViaClientGrainIfNotLocal()
    {
        // Arrange
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        IConnectionRegistry connectionRegistry = Substitute.For<IConnectionRegistry>();
        ISignalRClientGrain clientGrain = Substitute.For<ISignalRClientGrain>();
        connectionRegistry.GetConnection("conn1").Returns((HubConnectionContext?)null);
        grainFactory.GetClientGrain("TestAqueductHub", "conn1").Returns(clientGrain);
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(
            grainFactory: grainFactory,
            connectionRegistry: connectionRegistry);
        object?[] args = ["arg1", 42];

        // Act
        await manager.SendConnectionAsync("conn1", "MethodName", args);

        // Assert
        await clientGrain.Received(1).SendMessageAsync("MethodName", Arg.Any<ImmutableArray<object?>>());
    }

    /// <summary>
    ///     SendConnectionAsync should send to local connection if found.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendConnectionAsync Sends To Local Connection If Found")]
    public async Task SendConnectionAsyncShouldSendToLocalConnectionIfFound()
    {
        // Arrange
        IConnectionRegistry connectionRegistry = Substitute.For<IConnectionRegistry>();
        ILocalMessageSender messageSender = Substitute.For<ILocalMessageSender>();
        using CancellationTokenSource callerCancellationSource = new();
        using CancellationTokenSource connectionAbortSource = new();
        HubConnectionContext connection = HubConnectionContextFactory.Create(
            "conn1",
            connectionAborted: connectionAbortSource.Token);
        connectionRegistry.GetConnection("conn1").Returns(connection);
        TaskCompletionSource sendCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken forwardedCancellationToken = default;
        messageSender.SendAsync(
                connection,
                "MethodName",
                Arg.Any<IReadOnlyList<object?>>(),
                Arg.Do<CancellationToken>(token => forwardedCancellationToken = token))
            .Returns(sendCompletion.Task);
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(
            connectionRegistry: connectionRegistry,
            messageSender: messageSender);
        object?[] args = ["arg1", 42];

        // Act
        Task sendTask = manager.SendConnectionAsync("conn1", "MethodName", args, callerCancellationSource.Token);

        // Assert
        Assert.False(forwardedCancellationToken.IsCancellationRequested);
        await connectionAbortSource.CancelAsync();
        Assert.True(forwardedCancellationToken.IsCancellationRequested);
        await callerCancellationSource.CancelAsync();
        Assert.True(forwardedCancellationToken.IsCancellationRequested);
        sendCompletion.SetResult();
        await sendTask;
    }

    /// <summary>
    ///     SendConnectionAsync should throw when connectionId is empty.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendConnectionAsync Throws When ConnectionId Is Empty")]
    public async Task SendConnectionAsyncShouldThrowWhenConnectionIdIsEmpty()
    {
        // Arrange
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => manager.SendConnectionAsync(string.Empty, "method", []));
    }

    /// <summary>
    ///     SendConnectionAsync should throw when methodName is empty.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendConnectionAsync Throws When MethodName Is Empty")]
    public async Task SendConnectionAsyncShouldThrowWhenMethodNameIsEmpty()
    {
        // Arrange
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => manager.SendConnectionAsync("conn1", string.Empty, []));
    }

    /// <summary>
    ///     SendConnectionsAsync should throw when connectionIds is null.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendConnectionsAsync Throws When ConnectionIds Is Null")]
    public async Task SendConnectionsAsyncShouldThrowWhenConnectionIdsIsNull()
    {
        // Arrange
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.SendConnectionsAsync(null!, "method", []));
    }

    /// <summary>
    ///     SendGroupAsync should call group grain.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendGroupAsync Calls Group Grain")]
    public async Task SendGroupAsyncShouldCallGroupGrain()
    {
        // Arrange
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        ISignalRGroupGrain groupGrain = Substitute.For<ISignalRGroupGrain>();
        grainFactory.GetGroupGrain("TestAqueductHub", "group1").Returns(groupGrain);
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(grainFactory: grainFactory);
        object?[] args = ["arg1"];

        // Act
        await manager.SendGroupAsync("group1", "MethodName", args);

        // Assert
        await groupGrain.Received(1).SendMessageAsync("MethodName", Arg.Any<ImmutableArray<object?>>());
    }

    /// <summary>
    ///     SendGroupAsync should throw when groupName is empty.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendGroupAsync Throws When GroupName Is Empty")]
    public async Task SendGroupAsyncShouldThrowWhenGroupNameIsEmpty()
    {
        // Arrange
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => manager.SendGroupAsync(string.Empty, "method", []));
    }

    /// <summary>
    ///     SendGroupAsync should throw when methodName is empty.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendGroupAsync Throws When MethodName Is Empty")]
    public async Task SendGroupAsyncShouldThrowWhenMethodNameIsEmpty()
    {
        // Arrange
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => manager.SendGroupAsync("group1", string.Empty, []));
    }

    /// <summary>
    ///     SendGroupExceptAsync should throw when groupName is empty.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendGroupExceptAsync Throws When GroupName Is Empty")]
    public async Task SendGroupExceptAsyncShouldThrowWhenGroupNameIsEmpty()
    {
        // Arrange
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => manager.SendGroupExceptAsync(string.Empty, "method", [], []));
    }

    /// <summary>
    ///     SendGroupExceptAsync should throw when methodName is empty.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendGroupExceptAsync Throws When MethodName Is Empty")]
    public async Task SendGroupExceptAsyncShouldThrowWhenMethodNameIsEmpty()
    {
        // Arrange
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => manager.SendGroupExceptAsync("group1", string.Empty, [], []));
    }

    /// <summary>
    ///     SendGroupsAsync should throw when groupNames is null.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendGroupsAsync Throws When GroupNames Is Null")]
    public async Task SendGroupsAsyncShouldThrowWhenGroupNamesIsNull()
    {
        // Arrange
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.SendGroupsAsync(null!, "method", []));
    }

    /// <summary>
    ///     SendUserAsync should throw when methodName is empty.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendUserAsync Throws When MethodName Is Empty")]
    public async Task SendUserAsyncShouldThrowWhenMethodNameIsEmpty()
    {
        // Arrange
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => manager.SendUserAsync("user1", string.Empty, []));
    }

    /// <summary>
    ///     SendUserAsync should throw when userId is empty.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendUserAsync Throws When UserId Is Empty")]
    public async Task SendUserAsyncShouldThrowWhenUserIdIsEmpty()
    {
        // Arrange
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => manager.SendUserAsync(string.Empty, "method", []));
    }

    /// <summary>
    ///     SendUsersAsync should throw when userIds is null.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendUsersAsync Throws When UserIds Is Null")]
    public async Task SendUsersAsyncShouldThrowWhenUserIdsIsNull()
    {
        // Arrange
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.SendUsersAsync(null!, "method", []));
    }
}