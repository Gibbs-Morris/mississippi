using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Messages;

using NSubstitute;

using Orleans;
using Orleans.Runtime;
using Orleans.Streams;


namespace Mississippi.Aqueduct.Gateway.L0Tests;

/// <summary>
///     Tests for <see cref="StreamSubscriptionManager" />.
/// </summary>
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
    ///     EnsureInitializedAsync should cancel while subscribing to a stream.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "EnsureInitializedAsync Cancels Pending Subscription")]
    public async Task EnsureInitializedAsyncShouldCancelPendingSubscription()
    {
        // Arrange
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<AqueductOptions> options = Options.Create(new AqueductOptions());
        ILogger<StreamSubscriptionManager> logger = Substitute.For<ILogger<StreamSubscriptionManager>>();
        IStreamProvider streamProvider = Substitute.For<IStreamProvider>();
        IAsyncStream<ServerMessage> serverStream = Substitute.For<IAsyncStream<ServerMessage>>();
        IAsyncStream<AllMessage> allStream = Substitute.For<IAsyncStream<AllMessage>>();
        using CancellationTokenSource cancellationSource = new();
        StreamSubscriptionHandle<ServerMessage> firstServerSubscription =
            Substitute.For<StreamSubscriptionHandle<ServerMessage>>();
        StreamSubscriptionHandle<ServerMessage> secondServerSubscription =
            Substitute.For<StreamSubscriptionHandle<ServerMessage>>();
        StreamSubscriptionHandle<AllMessage> allSubscription = Substitute.For<StreamSubscriptionHandle<AllMessage>>();
        TaskCompletionSource<StreamSubscriptionHandle<ServerMessage>> firstSubscriptionCompletion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        ServiceCollection services = new();
        services.AddKeyedSingleton(options.Value.StreamProviderName, streamProvider);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        clusterClient.ServiceProvider.Returns(serviceProvider);
        streamProvider.GetStream<ServerMessage>(Arg.Any<StreamId>()).Returns(serverStream);
        streamProvider.GetStream<AllMessage>(Arg.Any<StreamId>()).Returns(allStream);
        serverStream.SubscribeAsync(Arg.Any<IAsyncObserver<ServerMessage>>())
            .Returns(firstSubscriptionCompletion.Task, Task.FromResult(secondServerSubscription));
        allStream.SubscribeAsync(Arg.Any<IAsyncObserver<AllMessage>>()).Returns(Task.FromResult(allSubscription));
        using StreamSubscriptionManager manager = new(CreateServerIdProvider(), clusterClient, options, logger);

        // Act
        Task initializationTask = manager.EnsureInitializedAsync(
            "TestHub",
            _ => Task.CompletedTask,
            _ => Task.CompletedTask,
            cancellationSource.Token);
        await cancellationSource.CancelAsync();
        await Assert.ThrowsAsync<TaskCanceledException>(() => initializationTask.WaitAsync(CancellationToken.None));
        firstSubscriptionCompletion.SetResult(firstServerSubscription);
        await manager.EnsureInitializedAsync("TestHub", _ => Task.CompletedTask, _ => Task.CompletedTask);

        // Assert
        await firstServerSubscription.Received(1).UnsubscribeAsync();
        _ = serverStream.Received(2).SubscribeAsync(Arg.Any<IAsyncObserver<ServerMessage>>());
        _ = allStream.Received(1).SubscribeAsync(Arg.Any<IAsyncObserver<AllMessage>>());
    }

    /// <summary>
    ///     EnsureInitializedAsync should throw when hubName is empty.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "EnsureInitializedAsync Throws When HubName Is Empty")]
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
    ///     EnsureInitializedAsync should use the server ID from the provider when creating streams.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "EnsureInitializedAsync Uses Provider ServerId")]
    public async Task EnsureInitializedAsyncShouldUseProviderServerId()
    {
        // Arrange
        string serverId = "server-123";
        IServerIdProvider serverIdProvider = CreateServerIdProvider(serverId);
        IClusterClient clusterClient = Substitute.For<IClusterClient>();
        IOptions<AqueductOptions> options = Options.Create(
            new AqueductOptions
            {
                StreamProviderName = "Provider",
                ServerStreamNamespace = "servers",
                AllClientsStreamNamespace = "all",
            });
        ILogger<StreamSubscriptionManager> logger = Substitute.For<ILogger<StreamSubscriptionManager>>();
        IStreamProvider streamProvider = Substitute.For<IStreamProvider>();
        IAsyncStream<ServerMessage> serverStream = Substitute.For<IAsyncStream<ServerMessage>>();
        IAsyncStream<AllMessage> allStream = Substitute.For<IAsyncStream<AllMessage>>();
        StreamSubscriptionHandle<ServerMessage> serverSubscription =
            Substitute.For<StreamSubscriptionHandle<ServerMessage>>();
        StreamSubscriptionHandle<AllMessage> allSubscription = Substitute.For<StreamSubscriptionHandle<AllMessage>>();
        ServiceCollection services = new();
        services.AddKeyedSingleton(options.Value.StreamProviderName, streamProvider);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        clusterClient.ServiceProvider.Returns(serviceProvider);
        streamProvider.GetStream<ServerMessage>(Arg.Any<StreamId>()).Returns(serverStream);
        streamProvider.GetStream<AllMessage>(Arg.Any<StreamId>()).Returns(allStream);
        serverStream.SubscribeAsync(Arg.Any<IAsyncObserver<ServerMessage>>())
            .Returns(Task.FromResult(serverSubscription));
        allStream.SubscribeAsync(Arg.Any<IAsyncObserver<AllMessage>>()).Returns(Task.FromResult(allSubscription));
        using StreamSubscriptionManager manager = new(serverIdProvider, clusterClient, options, logger);

        // Act
        await manager.EnsureInitializedAsync("TestHub", _ => Task.CompletedTask, _ => Task.CompletedTask);

        // Assert
        StreamId expectedServerStreamId = StreamId.Create(options.Value.ServerStreamNamespace, serverId);
        StreamId expectedAllStreamId = StreamId.Create(options.Value.AllClientsStreamNamespace, "TestHub");
        _ = streamProvider.Received(1).GetStream<ServerMessage>(expectedServerStreamId);
        _ = streamProvider.Received(1).GetStream<AllMessage>(expectedAllStreamId);
    }

    /// <summary>
    ///     Cancellation while cleanup is pending must leave the retained handles available to a later retry.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task InitializationShouldAllowCancelingCleanupWait()
    {
        using StreamSubscriptionFixture fixture = new();
        using CancellationTokenSource first = new();
        using CancellationTokenSource second = new();
        TaskCompletionSource<StreamSubscriptionHandle<ServerMessage>> subscription = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.ServerStream.SubscribeAsync(Arg.Any<IAsyncObserver<ServerMessage>>())
            .Returns(subscription.Task, Task.FromResult(fixture.ServerHandle));
        Task initialization = fixture.InitializeAsync(first.Token);
        await first.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            initialization.WaitAsync(TimeSpan.FromSeconds(5)));
        Task retry = fixture.InitializeAsync(second.Token);
        await second.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => retry.WaitAsync(TimeSpan.FromSeconds(5)));
        subscription.SetResult(fixture.ServerHandle);
        await fixture.InitializeAsync();
        await fixture.ServerHandle.Received(1).UnsubscribeAsync();
        Assert.True(fixture.Manager.IsInitialized);
    }

    /// <summary>
    ///     Persistent cleanup failure must prevent duplicate subscriptions while allowing later recovery.
    /// </summary>
    /// <param name="failBroadcastCleanup">Whether the failed handle belongs to the broadcast stream.</param>
    /// <returns>A task representing the test operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InitializationShouldBlockResubscriptionUntilCleanupSucceeds(
        bool failBroadcastCleanup
    )
    {
        using StreamSubscriptionFixture fixture = new();
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource<StreamSubscriptionHandle<AllMessage>> subscription = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.AllStream.SubscribeAsync(Arg.Any<IAsyncObserver<AllMessage>>())
            .Returns(subscription.Task, Task.FromResult(fixture.AllHandle));
        if (failBroadcastCleanup)
        {
            fixture.AllHandle.UnsubscribeAsync()
                .Returns(Task.FromException(new InvalidOperationException("Cleanup failed")));
        }
        else
        {
            fixture.ServerHandle.UnsubscribeAsync()
                .Returns(Task.FromException(new InvalidOperationException("Cleanup failed")));
        }

        Task initialization = fixture.InitializeAsync(cancellation.Token);
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            initialization.WaitAsync(TimeSpan.FromSeconds(5)));
        subscription.SetResult(fixture.AllHandle);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.InitializeAsync());
        Assert.False(fixture.Manager.IsInitialized);
        _ = fixture.ServerStream.Received(1).SubscribeAsync(Arg.Any<IAsyncObserver<ServerMessage>>());
        _ = fixture.AllStream.Received(1).SubscribeAsync(Arg.Any<IAsyncObserver<AllMessage>>());
        fixture.ServerHandle.UnsubscribeAsync().Returns(Task.CompletedTask);
        fixture.AllHandle.UnsubscribeAsync().Returns(Task.CompletedTask);
        await fixture.InitializeAsync();
        Assert.True(fixture.Manager.IsInitialized);
    }

    /// <summary>
    ///     Retry must wait for late broadcast subscription cleanup before creating any new subscriptions.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task InitializationShouldCleanBothSubscriptionsAfterBroadcastCancellation()
    {
        using StreamSubscriptionFixture fixture = new();
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource<StreamSubscriptionHandle<AllMessage>> subscription = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource unsubscribe = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.AllStream.SubscribeAsync(Arg.Any<IAsyncObserver<AllMessage>>())
            .Returns(subscription.Task, Task.FromResult(fixture.AllHandle));
        fixture.AllHandle.UnsubscribeAsync().Returns(unsubscribe.Task);
        Task initialization = fixture.InitializeAsync(cancellation.Token);
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            initialization.WaitAsync(TimeSpan.FromSeconds(5)));
        Task retry = fixture.InitializeAsync();
        Assert.False(retry.IsCompleted);
        subscription.SetResult(fixture.AllHandle);
        Assert.False(retry.IsCompleted);
        _ = fixture.ServerStream.Received(1).SubscribeAsync(Arg.Any<IAsyncObserver<ServerMessage>>());
        unsubscribe.SetResult();
        await retry;
        await fixture.ServerHandle.Received(1).UnsubscribeAsync();
        await fixture.AllHandle.Received(1).UnsubscribeAsync();
        Assert.True(fixture.Manager.IsInitialized);
    }

    /// <summary>
    ///     Broadcast setup faults must clean up an already active server subscription before retry.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task InitializationShouldCleanServerSubscriptionAfterBroadcastFailure()
    {
        using StreamSubscriptionFixture fixture = new();
        fixture.AllStream.SubscribeAsync(Arg.Any<IAsyncObserver<AllMessage>>())
            .Returns(
                Task.FromException<StreamSubscriptionHandle<AllMessage>>(
                    new InvalidOperationException("Broadcast failed")),
                Task.FromResult(fixture.AllHandle));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.InitializeAsync());
        await fixture.InitializeAsync();
        await fixture.ServerHandle.Received(1).UnsubscribeAsync();
        Assert.True(fixture.Manager.IsInitialized);
    }

    /// <summary>
    ///     Faulted or canceled subscription creation must be observed, logged, and permit a clean retry.
    /// </summary>
    /// <param name="subscriptionCanceled">Whether the underlying subscription itself was canceled.</param>
    /// <returns>A task representing the test operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InitializationShouldRecoverFromLateSubscriptionFailure(
        bool subscriptionCanceled
    )
    {
        using StreamSubscriptionFixture fixture = new();
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource<StreamSubscriptionHandle<ServerMessage>> subscription = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.ServerStream.SubscribeAsync(Arg.Any<IAsyncObserver<ServerMessage>>())
            .Returns(subscription.Task, Task.FromResult(fixture.ServerHandle));
        Task initialization = fixture.InitializeAsync(cancellation.Token);
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            initialization.WaitAsync(TimeSpan.FromSeconds(5)));
        if (subscriptionCanceled)
        {
            subscription.SetCanceled(cancellation.Token);
        }
        else
        {
            subscription.SetException(new InvalidOperationException("Subscription failed"));
        }

        await fixture.InitializeAsync();
        Assert.True(fixture.Manager.IsInitialized);
        int cleanupEventId = subscriptionCanceled ? 4 : 3;
        Assert.Contains(
            fixture.Logger.ReceivedCalls(),
            call => call.GetArguments().OfType<EventId>().Any(eventId => eventId.Id == cleanupEventId) &&
                    call.GetArguments().OfType<Exception>().Any());
        await fixture.ServerHandle.DidNotReceive().UnsubscribeAsync();
    }

    /// <summary>
    ///     Cancellation racing subscription completion must clean the handle without advancing canceled startup.
    /// </summary>
    /// <param name="cancelBroadcast">Whether cancellation coincides with broadcast rather than server subscription.</param>
    /// <returns>A task representing the test operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InitializationShouldRejectCancellationWhenSubscriptionCompletes(
        bool cancelBroadcast
    )
    {
        using StreamSubscriptionFixture fixture = new();
        using CancellationTokenSource cancellation = new();
        if (cancelBroadcast)
        {
            fixture.AllStream.SubscribeAsync(Arg.Any<IAsyncObserver<AllMessage>>())
                .Returns(async _ =>
                {
                    await cancellation.CancelAsync();
                    return fixture.AllHandle;
                });
        }
        else
        {
            fixture.ServerStream.SubscribeAsync(Arg.Any<IAsyncObserver<ServerMessage>>())
                .Returns(async _ =>
                {
                    await cancellation.CancelAsync();
                    return fixture.ServerHandle;
                });
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.InitializeAsync(cancellation.Token));
        Assert.False(fixture.Manager.IsInitialized);
        if (!cancelBroadcast)
        {
            _ = fixture.AllStream.DidNotReceive().SubscribeAsync(Arg.Any<IAsyncObserver<AllMessage>>());
        }

        fixture.ServerStream.SubscribeAsync(Arg.Any<IAsyncObserver<ServerMessage>>())
            .Returns(Task.FromResult(fixture.ServerHandle));
        fixture.AllStream.SubscribeAsync(Arg.Any<IAsyncObserver<AllMessage>>())
            .Returns(Task.FromResult(fixture.AllHandle));
        await fixture.InitializeAsync();
        await fixture.ServerHandle.Received(1).UnsubscribeAsync();
        if (cancelBroadcast)
        {
            await fixture.AllHandle.Received(1).UnsubscribeAsync();
        }
        else
        {
            await fixture.AllHandle.DidNotReceive().UnsubscribeAsync();
        }

        Assert.True(fixture.Manager.IsInitialized);
    }

    /// <summary>
    ///     A failed unsubscribe must retain its handle for retry instead of losing it or poisoning initialization.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task InitializationShouldRetryFailedUnsubscribeBeforeResubscribing()
    {
        using StreamSubscriptionFixture fixture = new();
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource<StreamSubscriptionHandle<AllMessage>> subscription = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.AllStream.SubscribeAsync(Arg.Any<IAsyncObserver<AllMessage>>())
            .Returns(subscription.Task, Task.FromResult(fixture.AllHandle));
        fixture.ServerHandle.UnsubscribeAsync()
            .Returns(Task.FromException(new InvalidOperationException("Cleanup unavailable")), Task.CompletedTask);
        Task initialization = fixture.InitializeAsync(cancellation.Token);
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            initialization.WaitAsync(TimeSpan.FromSeconds(5)));
        subscription.SetResult(fixture.AllHandle);
        await fixture.InitializeAsync();
        await fixture.ServerHandle.Received(2).UnsubscribeAsync();
        await fixture.AllHandle.Received(1).UnsubscribeAsync();
        Assert.True(fixture.Manager.IsInitialized);
    }

    /// <summary>
    ///     IsInitialized should be false before initialization.
    /// </summary>
    [Fact(DisplayName = "IsInitialized Is False Before Initialization")]
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