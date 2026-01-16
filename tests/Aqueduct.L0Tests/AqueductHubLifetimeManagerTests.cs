using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;

using NSubstitute;

using Orleans;


namespace Mississippi.Aqueduct.L0Tests;

/// <summary>
///     Tests for <see cref="AqueductHubLifetimeManager{THub}" />.
/// </summary>
[AllureParentSuite("Aqueduct")]
[AllureSuite("Core")]
[AllureSubSuite("AqueductHubLifetimeManager")]
public sealed class AqueductHubLifetimeManagerTests
{
    private static AqueductHubLifetimeManager<TestAqueductHub> CreateManager(
        IClusterClient? clusterClient = null,
        IAqueductGrainFactory? grainFactory = null,
        IConnectionRegistry? connectionRegistry = null,
        ILocalMessageSender? messageSender = null,
        IOptions<AqueductOptions>? options = null,
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>>? logger = null
    ) =>
        new(
            clusterClient ?? Substitute.For<IClusterClient>(),
            grainFactory ?? Substitute.For<IAqueductGrainFactory>(),
            connectionRegistry ?? Substitute.For<IConnectionRegistry>(),
            messageSender ?? Substitute.For<ILocalMessageSender>(),
            options ?? Options.Create(new AqueductOptions()),
            logger ?? NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance);

    [SuppressMessage(
        "Microsoft.Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "HubConnectionContext manages its own lifetime; caller disposes via using")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test helper creates context that is disposed by caller via using statement")]
    private static HubConnectionContext CreateTestConnection(
        string connectionId
    )
    {
        TestConnectionContext connectionContext = new(connectionId);
        return new(
            connectionContext,
            new()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(30),
                ClientTimeoutInterval = TimeSpan.FromMinutes(1),
            },
            NullLoggerFactory.Instance);
    }

    /// <summary>
    ///     Minimal ConnectionContext implementation for testing.
    /// </summary>
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test helper pipes are short-lived and do not need disposal in tests")]
    private sealed class TestConnectionContext
        : ConnectionContext,
          IDisposable
    {
        private readonly IDuplexPipe transport;

        public TestConnectionContext(
            string connectionId
        )
        {
            ConnectionId = connectionId;
            Pipe applicationPipe = new();
            Pipe transportPipe = new();
            transport = new TestDuplexPipe(transportPipe.Reader, applicationPipe.Writer);
            Items = new Dictionary<object, object?>();
        }

        public override string ConnectionId { get; set; }

        public override IFeatureCollection Features { get; } = new FeatureCollection();

        public override IDictionary<object, object?> Items { get; set; }

        public override IDuplexPipe Transport
        {
            get => transport;
            set => throw new NotSupportedException();
        }

        public void Dispose()
        {
            // Clean up pipes if needed
        }
    }

    /// <summary>
    ///     Simple duplex pipe implementation for testing.
    /// </summary>
    private sealed class TestDuplexPipe : IDuplexPipe
    {
        public TestDuplexPipe(
            PipeReader input,
            PipeWriter output
        )
        {
            Input = input;
            Output = output;
        }

        public PipeReader Input { get; }

        public PipeWriter Output { get; }
    }

    /// <summary>
    ///     AddToGroupAsync should call group grain.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "AddToGroupAsync Calls Group Grain")]
    [AllureFeature("Group Operations")]
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
    [AllureFeature("Argument Validation")]
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
    [AllureFeature("Argument Validation")]
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
    [AllureFeature("Construction")]
    public void ConstructorShouldSucceedWithValidDependencies()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        IConnectionRegistry connectionRegistry = Substitute.For<IConnectionRegistry>();
        ILocalMessageSender messageSender = Substitute.For<ILocalMessageSender>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>> logger =
            NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance;

        // Act
        using AqueductHubLifetimeManager<TestAqueductHub> manager = new(
            clusterClient,
            grainFactory,
            connectionRegistry,
            messageSender,
            options,
            logger);

        // Assert
        Assert.NotNull(manager);
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
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        IConnectionRegistry connectionRegistry = Substitute.For<IConnectionRegistry>();
        ILocalMessageSender messageSender = Substitute.For<ILocalMessageSender>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>> logger =
            NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AqueductHubLifetimeManager<TestAqueductHub>(
            null!,
            grainFactory,
            connectionRegistry,
            messageSender,
            options,
            logger));
    }

    /// <summary>
    ///     Constructor should throw when connectionRegistry is null.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When ConnectionRegistry Is Null")]
    [AllureFeature("Argument Validation")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Test expects exception before object is created")]
    public void ConstructorShouldThrowWhenConnectionRegistryIsNull()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        ILocalMessageSender messageSender = Substitute.For<ILocalMessageSender>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>> logger =
            NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AqueductHubLifetimeManager<TestAqueductHub>(
            clusterClient,
            grainFactory,
            null!,
            messageSender,
            options,
            logger));
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
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IConnectionRegistry connectionRegistry = Substitute.For<IConnectionRegistry>();
        ILocalMessageSender messageSender = Substitute.For<ILocalMessageSender>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>> logger =
            NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AqueductHubLifetimeManager<TestAqueductHub>(
            clusterClient,
            null!,
            connectionRegistry,
            messageSender,
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
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        IConnectionRegistry connectionRegistry = Substitute.For<IConnectionRegistry>();
        ILocalMessageSender messageSender = Substitute.For<ILocalMessageSender>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AqueductHubLifetimeManager<TestAqueductHub>(
            clusterClient,
            grainFactory,
            connectionRegistry,
            messageSender,
            options,
            null!));
    }

    /// <summary>
    ///     Constructor should throw when messageSender is null.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When MessageSender Is Null")]
    [AllureFeature("Argument Validation")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Test expects exception before object is created")]
    public void ConstructorShouldThrowWhenMessageSenderIsNull()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        IConnectionRegistry connectionRegistry = Substitute.For<IConnectionRegistry>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>> logger =
            NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AqueductHubLifetimeManager<TestAqueductHub>(
            clusterClient,
            grainFactory,
            connectionRegistry,
            null!,
            options,
            logger));
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
        IAqueductGrainFactory grainFactory = Substitute.For<IAqueductGrainFactory>();
        IConnectionRegistry connectionRegistry = Substitute.For<IConnectionRegistry>();
        ILocalMessageSender messageSender = Substitute.For<ILocalMessageSender>();
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>> logger =
            NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AqueductHubLifetimeManager<TestAqueductHub>(
            clusterClient,
            grainFactory,
            connectionRegistry,
            messageSender,
            null!,
            logger));
    }

    /// <summary>
    ///     OnConnectedAsync should throw when connection is null.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "OnConnectedAsync Throws When Connection Is Null")]
    [AllureFeature("Argument Validation")]
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
    [AllureFeature("Argument Validation")]
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
    [AllureFeature("Group Operations")]
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
    [AllureFeature("Argument Validation")]
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
    [AllureFeature("Argument Validation")]
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
    [AllureFeature("Message Sending")]
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
    [AllureFeature("Message Sending")]
    public async Task SendConnectionAsyncShouldSendToLocalConnectionIfFound()
    {
        // Arrange
        IConnectionRegistry connectionRegistry = Substitute.For<IConnectionRegistry>();
        ILocalMessageSender messageSender = Substitute.For<ILocalMessageSender>();
        HubConnectionContext connection = CreateTestConnection("conn1");
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
    [AllureFeature("Argument Validation")]
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
    [AllureFeature("Argument Validation")]
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
    [AllureFeature("Argument Validation")]
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
    [AllureFeature("Message Sending")]
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
    [AllureFeature("Argument Validation")]
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
    [AllureFeature("Argument Validation")]
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
    [AllureFeature("Argument Validation")]
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
    [AllureFeature("Argument Validation")]
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
    [AllureFeature("Argument Validation")]
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
    [AllureFeature("Argument Validation")]
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
    [AllureFeature("Argument Validation")]
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
    [AllureFeature("Argument Validation")]
    public async Task SendUsersAsyncShouldThrowWhenUserIdsIsNull()
    {
        // Arrange
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.SendUsersAsync(null!, "method", []));
    }
}