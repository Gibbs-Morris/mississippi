using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Abstractions.Messages;
using Mississippi.Testing.Utilities.SignalR;

using NSubstitute;

using Orleans;
using Orleans.Runtime;


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
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>>? logger = null
    ) =>
        new(
            serverIdProvider ?? CreateServerIdProvider(),
            grainFactory ?? Substitute.For<IAqueductGrainFactory>(),
            connectionRegistry ?? Substitute.For<IConnectionRegistry>(),
            messageSender ?? Substitute.For<ILocalMessageSender>(),
            heartbeatManager ?? Substitute.For<IHeartbeatManager>(),
            streamSubscriptionManager ?? Substitute.For<IStreamSubscriptionManager>(),
            logger ?? NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance);

    private static IServerIdProvider CreateServerIdProvider(
        string? serverId = null
    )
    {
        IServerIdProvider provider = Substitute.For<IServerIdProvider>();
        provider.ServerId.Returns(serverId ?? Guid.NewGuid().ToString("N"));
        return provider;
    }

    private static Task SendBroadcastAsync(
        AqueductHubLifetimeManager<TestAqueductHub> manager,
        bool excludeConnection,
        CancellationToken cancellationToken
    ) =>
        excludeConnection
            ? manager.SendAllExceptAsync("TestMethod", [], ["excluded"], cancellationToken)
            : manager.SendAllAsync("TestMethod", [], cancellationToken);

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
    ///     Broadcast initialization should observe caller cancellation and prevent publication.
    /// </summary>
    /// <param name="excludeConnection">Whether to use the broadcast operation with exclusions.</param>
    /// <returns>A task representing the test operation.</returns>
    [Theory(DisplayName = "Broadcast Initialization Preserves Caller Cancellation")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BroadcastInitializationShouldPreserveCallerCancellation(
        bool excludeConnection
    )
    {
        // Arrange
        using CancellationTokenSource caller = new();
        IStreamSubscriptionManager streamManager = Substitute.For<IStreamSubscriptionManager>();
        using IHeartbeatManager heartbeatManager = Substitute.For<IHeartbeatManager>();
        streamManager.EnsureInitializedAsync(
                "TestAqueductHub",
                Arg.Any<Func<ServerMessage, Task>>(),
                Arg.Any<Func<AllMessage, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                call.Arg<CancellationToken>().ThrowIfCancellationRequested();
                return Task.CompletedTask;
            });
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(
            heartbeatManager: heartbeatManager,
            streamSubscriptionManager: streamManager);

        // Act
        await caller.CancelAsync();
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            SendBroadcastAsync(manager, excludeConnection, caller.Token));

        // Assert
        _ = streamManager.Received(1)
            .EnsureInitializedAsync(
                "TestAqueductHub",
                Arg.Any<Func<ServerMessage, Task>>(),
                Arg.Any<Func<AllMessage, Task>>(),
                caller.Token);
        Assert.Equal(caller.Token, exception.CancellationToken);
        await heartbeatManager.DidNotReceive().StartAsync(Arg.Any<Func<int>>(), Arg.Any<CancellationToken>());
        await streamManager.DidNotReceive().PublishToAllAsync(Arg.Any<AllMessage>());
    }

    /// <summary>
    ///     Broadcast delivery should continue when an earlier recipient aborts during a pending write.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "Broadcast Reaches Healthy Recipient After Connection Abort")]
    public async Task BroadcastShouldReachHealthyRecipientAfterConnectionAbort()
    {
        // Arrange
        using CancellationTokenSource connectionAborted = new();
        TaskCompletionSource pendingWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RecordingHubConnectionContext disconnecting = new("conn-1", pendingWrite.Task, connectionAborted.Token);
        RecordingHubConnectionContext healthy = new("conn-2", Task.CompletedTask, CancellationToken.None);
        IConnectionRegistry registry = Substitute.For<IConnectionRegistry>();
        registry.GetAll().Returns([disconnecting, healthy]);
        IStreamSubscriptionManager streamManager = Substitute.For<IStreamSubscriptionManager>();
        Func<AllMessage, Task>? onAllMessage = null;
        streamManager.EnsureInitializedAsync(
                "TestAqueductHub",
                Arg.Any<Func<ServerMessage, Task>>(),
                Arg.Do<Func<AllMessage, Task>>(callback => onAllMessage = callback),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        LocalMessageSender sender = new(Substitute.For<ILogger<LocalMessageSender>>());
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(
            connectionRegistry: registry,
            messageSender: sender,
            streamSubscriptionManager: streamManager);
        await manager.SendAllAsync("Initialize", [], CancellationToken.None);
        Assert.NotNull(onAllMessage);
        AllMessage message = new()
        {
            MethodName = "TestMethod",
            Args = ["arg1", 42],
        };

        // Act
        Task broadcast = onAllMessage(message);
        Assert.Equal(connectionAborted.Token, disconnecting.LastWriteCancellationToken);
        Assert.Null(healthy.LastMessage);
        await connectionAborted.CancelAsync();
        await broadcast;

        // Assert
        InvocationMessage invocation = Assert.IsType<InvocationMessage>(healthy.LastMessage);
        Assert.Equal(message.MethodName, invocation.Target);
        Assert.Equal(message.Args, invocation.Arguments);
        Assert.False(pendingWrite.Task.IsCompleted);
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
            logger));
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

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AqueductHubLifetimeManager<TestAqueductHub>(
            serverIdProvider,
            grainFactory,
            connectionRegistry,
            messageSender,
            heartbeatManager,
            streamSubscriptionManager,
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
            logger));
    }

    /// <summary>
    ///     Shared initialization should complete even when the initiating connection is aborted.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "OnConnectedAsync Initializes Backplane Despite Connection Abort")]
    public async Task OnConnectedAsyncShouldInitializeBackplaneDespiteConnectionAbort()
    {
        // Arrange
        using CancellationTokenSource connectionAborted = new();
        RecordingHubConnectionContext connection = new("conn-1", Task.CompletedTask, connectionAborted.Token);
        TaskCompletionSource initialization = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IStreamSubscriptionManager streamManager = Substitute.For<IStreamSubscriptionManager>();
        using IHeartbeatManager heartbeatManager = Substitute.For<IHeartbeatManager>();
        IConnectionRegistry registry = Substitute.For<IConnectionRegistry>();
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        ISignalRClientGrain clientGrain = Substitute.For<ISignalRClientGrain>();
        grainFactory.GetClientGrain("TestAqueductHub", connection.ConnectionId).Returns(clientGrain);
        streamManager.EnsureInitializedAsync(
                "TestAqueductHub",
                Arg.Any<Func<ServerMessage, Task>>(),
                Arg.Any<Func<AllMessage, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call => await initialization.Task.WaitAsync(call.Arg<CancellationToken>()));
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(
            grainFactory: grainFactory,
            connectionRegistry: registry,
            heartbeatManager: heartbeatManager,
            streamSubscriptionManager: streamManager);

        // Act
        Task connecting = manager.OnConnectedAsync(connection);
        await connectionAborted.CancelAsync();

        // Assert
        Assert.True(connection.ConnectionAborted.IsCancellationRequested);
        Assert.False(connecting.IsCompleted);
        _ = streamManager.Received(1)
            .EnsureInitializedAsync(
                "TestAqueductHub",
                Arg.Any<Func<ServerMessage, Task>>(),
                Arg.Any<Func<AllMessage, Task>>(),
                CancellationToken.None);
        await heartbeatManager.DidNotReceive().StartAsync(Arg.Any<Func<int>>(), Arg.Any<CancellationToken>());
        initialization.SetResult();
        await connecting;
        await heartbeatManager.Received(1).StartAsync(Arg.Any<Func<int>>(), CancellationToken.None);
        registry.Received(1).TryAdd(connection.ConnectionId, connection);
        await clientGrain.Received(1).ConnectAsync("TestAqueductHub", Arg.Any<string>());
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
    ///     Lifecycle startup should preserve its cancellation token during shared initialization.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "Participate Forwards Lifecycle Cancellation Token")]
    public async Task ParticipateShouldForwardLifecycleCancellationToken()
    {
        // Arrange
        using CancellationTokenSource startup = new();
        ISiloLifecycle lifecycle = Substitute.For<ISiloLifecycle>();
        ILifecycleObserver? observer = null;
        using IDisposable subscription = Substitute.For<IDisposable>();
        using IDisposable configuredSubscription = lifecycle.Subscribe(
            Arg.Any<string>(),
            ServiceLifecycleStage.Active,
            Arg.Do<ILifecycleObserver>(value => observer = value));
        configuredSubscription.Returns(subscription);
        IStreamSubscriptionManager streamManager = Substitute.For<IStreamSubscriptionManager>();
        using IHeartbeatManager heartbeatManager = Substitute.For<IHeartbeatManager>();
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(
            heartbeatManager: heartbeatManager,
            streamSubscriptionManager: streamManager);

        // Act
        manager.Participate(lifecycle);
        Assert.NotNull(observer);
        await observer.OnStart(startup.Token);

        // Assert
        _ = streamManager.Received(1)
            .EnsureInitializedAsync(
                "TestAqueductHub",
                Arg.Any<Func<ServerMessage, Task>>(),
                Arg.Any<Func<AllMessage, Task>>(),
                startup.Token);
        await heartbeatManager.Received(1).StartAsync(Arg.Any<Func<int>>(), startup.Token);
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
        HubConnectionContext connection = HubConnectionContextFactory.Create("conn1");
        connectionRegistry.GetConnection("conn1").Returns(connection);
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(
            connectionRegistry: connectionRegistry,
            messageSender: messageSender);
        object?[] args = ["arg1", 42];

        // Act
        await manager.SendConnectionAsync("conn1", "MethodName", args);

        // Assert
        await messageSender.Received(1).SendAsync(connection, "MethodName", args);
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