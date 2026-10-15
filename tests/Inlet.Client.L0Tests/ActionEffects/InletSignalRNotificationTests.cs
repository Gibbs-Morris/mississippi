using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.L0Tests.Helpers;
using Mississippi.Inlet.Client.Reducers;
using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Inlet.Gateway.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;

using Moq;


namespace MississippiTests.Inlet.Client.L0Tests.ActionEffects;

/// <summary>
///     Verifies that notification fetches keep projection data and its returned version together.
/// </summary>
/// <remarks>
///     This class is public so xUnit can discover its tests.
/// </remarks>
public sealed class InletSignalRNotificationTests
{
    private const string EntityId = "entity-1";

    private const string ProjectionPath = "test/projection";

    /// <summary>
    ///     Verifies the dispatched action and reducer state keep the returned data and version together.
    /// </summary>
    /// <param name="actions">The actions dispatched by the notification handler.</param>
    /// <param name="expectedData">The fetched projection data.</param>
    /// <param name="expectedVersion">The version returned with the data.</param>
    private static void AssertUpdatedState(
        List<IAction> actions,
        TestProjection expectedData,
        long expectedVersion
    )
    {
        Assert.Equal(2, actions.Count);
        Assert.IsType<SignalRMessageReceivedAction>(actions[0]);
        ProjectionUpdatedAction<TestProjection> update =
            Assert.IsType<ProjectionUpdatedAction<TestProjection>>(actions[1]);
        Assert.Equal(EntityId, update.EntityId);
        Assert.Same(expectedData, update.Data);
        ProjectionsFeatureState state = ProjectionsReducer.ReduceUpdated(new(), update);
        Assert.Same(expectedData, state.GetProjection<TestProjection>(EntityId));
        Assert.Equal(expectedVersion, state.GetProjectionVersion<TestProjection>(EntityId));
        Assert.Equal(expectedVersion, update.Version);
    }

    /// <summary>
    ///     Creates an established subscription and captures the real notification callback without network IO.
    /// </summary>
    /// <param name="fetcher">The fetcher used by the notification handler.</param>
    /// <param name="actions">The destination for dispatched actions.</param>
    /// <returns>The effect and its registered notification callback.</returns>
    private static async Task<(InletSignalRActionEffect Effect, Func<string, string, long, Task> Notification)>
        CreateSubscribedEffectAsync(
            IProjectionFetcher fetcher,
            List<IAction> actions
        )
    {
        Mock<IInletStore> store = new();
        store.Setup(value => value.Dispatch(It.IsAny<IAction>())).Callback<IAction>(actions.Add);
        Mock<IProjectionDtoRegistry> registry = new();
        registry.Setup(value => value.GetDtoType(ProjectionPath)).Returns(typeof(TestProjection));
        registry.Setup(value => value.GetPath(typeof(TestProjection))).Returns(ProjectionPath);
        Mock<HubConnection> hub = new(
            Mock.Of<IConnectionFactory>(),
            new JsonHubProtocol(),
            new IPEndPoint(IPAddress.Loopback, 80),
            Mock.Of<IServiceProvider>(),
            NullLoggerFactory.Instance);
        hub.Setup(value => value.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("subscription-1");
        Mock<IHubConnectionProvider> provider = new();
        provider.SetupGet(value => value.Connection).Returns(hub.Object);
        provider.Setup(value => value.EnsureConnectedAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        Func<string, string, long, Task>? handler = null;
        provider.Setup(value => value.RegisterHandler(
                InletHubConstants.ProjectionUpdatedMethod,
                It.IsAny<Func<string, string, long, Task>>()))
            .Callback<string, Func<string, string, long, Task>>((_, callback) => handler = callback)
            .Returns(Mock.Of<IDisposable>());
        Mock<IProjectionFetcher> subscriptionFetcher = new();
        subscriptionFetcher.Setup(value => value.FetchAsync(
                It.IsAny<Type>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectionFetchResult.NotFound);
        subscriptionFetcher.Setup(value => value.FetchAtVersionAsync(
                It.IsAny<Type>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns((Type type, string entityId, long version, CancellationToken token) =>
                fetcher.FetchAtVersionAsync(type, entityId, version, token));
        InletSignalRActionEffect effect = new(
            new(() => store.Object),
            provider.Object,
            subscriptionFetcher.Object,
            registry.Object,
            new FakeTimeProvider());
        List<IAction> subscriptionActions = new();
        await foreach (IAction action in effect.HandleAsync(
                           new SubscribeToProjectionAction<TestProjection>(EntityId),
                           new(),
                           CancellationToken.None))
        {
            subscriptionActions.Add(action);
        }

        Assert.Collection(
            subscriptionActions,
            action => Assert.IsType<ProjectionLoadingAction<TestProjection>>(action),
            action => Assert.IsType<ProjectionLoadedAction<TestProjection>>(action));
        Assert.NotNull(handler);
        return (effect, handler);
    }

    /// <summary>
    ///     An exact-version fetch retains the notification request and its matching result.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ExactVersionNotificationPreservesRequestAndResult()
    {
        const long Version = 10;
        TestProjection data = new()
        {
            Name = "version-10",
        };
        Mock<IProjectionFetcher> fetcher = new(MockBehavior.Strict);
        fetcher.Setup(value => value.FetchAtVersionAsync(
                typeof(TestProjection),
                EntityId,
                Version,
                CancellationToken.None))
            .ReturnsAsync(ProjectionFetchResult.Create(data, Version));
        List<IAction> actions = new();
        (InletSignalRActionEffect effect, Func<string, string, long, Task> notification) =
            await CreateSubscribedEffectAsync(fetcher.Object, actions);
        await using InletSignalRActionEffect subscribedEffect = effect;
        await notification(ProjectionPath, EntityId, Version);
        AssertUpdatedState(actions, data, Version);
        fetcher.Verify(
            value => value.FetchAtVersionAsync(typeof(TestProjection), EntityId, Version, CancellationToken.None),
            Times.Once);
        fetcher.Verify(
            value => value.FetchAsync(It.IsAny<Type>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     The actual default versioned-read fallback preserves the version returned with the latest data.
    /// </summary>
    /// <param name="notificationVersion">The version announced by the server.</param>
    /// <param name="returnedVersion">The version returned by the latest-only fetcher.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(10L, 12L)]
    [InlineData(10L, 10L)]
    [InlineData(10L, 8L)]
    public async Task LatestOnlyNotificationPreservesReturnedDataAndVersion(
        long notificationVersion,
        long returnedVersion
    )
    {
        TestProjection data = new()
        {
            Name = $"version-{returnedVersion}",
        };
        LatestOnlyProjectionFetcher fetcher = new()
        {
            Result = ProjectionFetchResult.Create(data, returnedVersion),
        };
        List<IAction> actions = new();
        (InletSignalRActionEffect effect, Func<string, string, long, Task> notification) =
            await CreateSubscribedEffectAsync(fetcher, actions);
        await using InletSignalRActionEffect subscribedEffect = effect;
        await notification(ProjectionPath, EntityId, notificationVersion);
        Assert.Equal(1, fetcher.FetchCount);
        Assert.Equal(typeof(TestProjection), fetcher.LastProjectionType);
        Assert.Equal(EntityId, fetcher.LastEntityId);
        AssertUpdatedState(actions, data, returnedVersion);
    }

    /// <summary>
    ///     Overlapping notification fetches keep each completed result paired with its own version.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task OverlappingNotificationsPreserveEachReturnedDataAndVersion()
    {
        TestProjection olderData = new()
        {
            Name = "version-12",
        };
        TestProjection newerData = new()
        {
            Name = "version-15",
        };
        TaskCompletionSource<ProjectionFetchResult?> olderFetch = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ProjectionFetchResult?> newerFetch = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IProjectionFetcher> fetcher = new(MockBehavior.Strict);
        fetcher.Setup(value => value.FetchAtVersionAsync(typeof(TestProjection), EntityId, 10, CancellationToken.None))
            .Returns(olderFetch.Task);
        fetcher.Setup(value => value.FetchAtVersionAsync(typeof(TestProjection), EntityId, 11, CancellationToken.None))
            .Returns(newerFetch.Task);
        List<IAction> actions = new();
        (InletSignalRActionEffect effect, Func<string, string, long, Task> notification) =
            await CreateSubscribedEffectAsync(fetcher.Object, actions);
        await using InletSignalRActionEffect subscribedEffect = effect;
        Task olderNotification = notification(ProjectionPath, EntityId, 10);
        Task newerNotification = notification(ProjectionPath, EntityId, 11);
        Assert.False(olderNotification.IsCompleted);
        Assert.False(newerNotification.IsCompleted);
        Assert.Equal(2, actions.Count);
        Assert.All(actions, action => Assert.IsType<SignalRMessageReceivedAction>(action));
        newerFetch.SetResult(ProjectionFetchResult.Create(newerData, 15));
        await newerNotification;
        AssertUpdatedState([actions[1], actions[2]], newerData, 15);
        Assert.False(olderNotification.IsCompleted);
        olderFetch.SetResult(ProjectionFetchResult.Create(olderData, 12));
        await olderNotification;
        Assert.Equal(4, actions.Count);
        AssertUpdatedState([actions[0], actions[3]], olderData, 12);
        fetcher.VerifyAll();
    }
}