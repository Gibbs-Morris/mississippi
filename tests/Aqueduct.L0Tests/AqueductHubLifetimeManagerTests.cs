using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;


using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Testing.Utilities.SignalR;

using NSubstitute;


namespace Mississippi.Aqueduct.L0Tests;

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