using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.L0Tests.Helpers;
using Mississippi.Inlet.Client.Reducers;
using Mississippi.Inlet.Gateway.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;

using Moq;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects;

/// <summary>
///     Verifies real fetch completion paths reach monotonic projection reducers.
/// </summary>
public sealed class InletSignalRMonotonicCompletionTests
{
    /// <summary>
    ///     A delayed initial, refresh, live or reconnect fetch cannot replace newer state.
    /// </summary>
    /// <param name="path">The effect path under test.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData("initial")]
    [InlineData("refresh")]
    [InlineData("live")]
    [InlineData("reconnect")]
    public async Task DelayedSuccessfulFetchKeepsNewerReducerState(
        string path
    )
    {
        const string entityId = "entity";
        const string projectionPath = "test/projections";
        Mock<HubConnection> hub = new(
            Mock.Of<IConnectionFactory>(),
            new JsonHubProtocol(),
            new IPEndPoint(IPAddress.Loopback, 80),
            Mock.Of<IServiceProvider>(),
            NullLoggerFactory.Instance);
        hub.Setup(connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("subscription");
        Mock<IHubConnectionProvider> provider = new();
        provider.SetupGet(value => value.Connection).Returns(hub.Object);
        provider.Setup(value => value.EnsureConnectedAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        Func<string, string, long, Task>? notification = null;
        Func<string?, Task>? reconnect = null;
        provider.Setup(value => value.RegisterHandler(
                InletHubConstants.ProjectionUpdatedMethod,
                It.IsAny<Func<string, string, long, Task>>()))
            .Callback((string _, Func<string, string, long, Task> callback) => notification = callback)
            .Returns(Mock.Of<IDisposable>());
        provider.Setup(value => value.OnReconnected(It.IsAny<Func<string?, Task>>()))
            .Callback<Func<string?, Task>>(callback => reconnect = callback);
        TaskCompletionSource<ProjectionFetchResult?> delayed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IProjectionFetcher> fetcher = new(MockBehavior.Strict);
        fetcher.Setup(value => value.FetchAsync(typeof(TestProjection), entityId, CancellationToken.None))
            .ReturnsAsync(ProjectionFetchResult.NotFound);
        fetcher.Setup(value => value.FetchAtVersionAsync(typeof(TestProjection), entityId, 8, CancellationToken.None))
            .Returns(delayed.Task);
        ProjectionDtoRegistry registry = new();
        registry.Register(projectionPath, typeof(TestProjection));
        ProjectionsFeatureState state = new();
        List<IAction> completions = [];

        void Apply(
            IAction action
        )
        {
            state = action switch
            {
                ProjectionLoadingAction<TestProjection> loading => ProjectionsReducer.ReduceLoading(state, loading),
                ProjectionLoadedAction<TestProjection> loaded => ProjectionsReducer.ReduceLoaded(state, loaded),
                ProjectionUpdatedAction<TestProjection> updated => ProjectionsReducer.ReduceUpdated(state, updated),
                var _ => state,
            };
            if (action is ProjectionLoadedAction<TestProjection> or ProjectionUpdatedAction<TestProjection>)
            {
                completions.Add(action);
            }
        }

        Mock<IInletStore> store = new();
        store.Setup(value => value.Dispatch(It.IsAny<IAction>())).Callback<IAction>(Apply);
        await using InletSignalRActionEffect effect = new(
            new(() => store.Object),
            provider.Object,
            fetcher.Object,
            registry);

        async Task ConsumeAsync(
            IAction action
        )
        {
            await foreach (IAction emitted in effect.HandleAsync(action, new(), CancellationToken.None))
            {
                Apply(emitted);
            }
        }

        if (path != "initial")
        {
            await ConsumeAsync(new SubscribeToProjectionAction<TestProjection>(entityId));
            Assert.Null(state.GetProjection<TestProjection>(entityId));
            Assert.Equal(0, state.GetProjectionVersion<TestProjection>(entityId));
            Assert.False(state.IsProjectionLoading<TestProjection>(entityId));
        }

        fetcher.Setup(value => value.FetchAsync(typeof(TestProjection), entityId, CancellationToken.None))
            .Returns(delayed.Task);
        Assert.NotNull(notification);
        Assert.NotNull(reconnect);
        Task pending = path switch
        {
            "initial" => ConsumeAsync(new SubscribeToProjectionAction<TestProjection>(entityId)),
            "refresh" => ConsumeAsync(new RefreshProjectionAction<TestProjection>(entityId)),
            "live" => notification(projectionPath, entityId, 8),
            "reconnect" => reconnect("new-connection"),
            var _ => throw new InvalidOperationException("Unknown test path"),
        };
        Assert.False(pending.IsCompleted);
        TestProjection newer = new()
        {
            Name = "version-12",
        };
        Apply(new ProjectionUpdatedAction<TestProjection>(entityId, newer, 12));
        ProjectionEntry<TestProjection>? accepted = state.GetEntry<TestProjection>(entityId);
        TestProjection older = new()
        {
            Name = "version-10",
        };
        delayed.SetResult(ProjectionFetchResult.Create(older, 10));
        await pending;
        if (path == "initial")
        {
            ProjectionLoadedAction<TestProjection> loaded =
                Assert.IsType<ProjectionLoadedAction<TestProjection>>(completions[^1]);
            Assert.Same(older, loaded.Data);
            Assert.Equal(10, loaded.Version);
        }
        else
        {
            ProjectionUpdatedAction<TestProjection> updated =
                Assert.IsType<ProjectionUpdatedAction<TestProjection>>(completions[^1]);
            Assert.Same(older, updated.Data);
            Assert.Equal(10, updated.Version);
        }

        Assert.Same(newer, state.GetProjection<TestProjection>(entityId));
        Assert.Equal(12, state.GetProjectionVersion<TestProjection>(entityId));
        Assert.Same(accepted, state.GetEntry<TestProjection>(entityId));
        fetcher.Verify(
            value => value.FetchAtVersionAsync(typeof(TestProjection), entityId, 8, CancellationToken.None),
            path == "live" ? Times.Once() : Times.Never());
        await hub.Object.DisposeAsync();
    }
}