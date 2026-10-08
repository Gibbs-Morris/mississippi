using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

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
    /// <param name="notification">The registered projection update callback.</param>
    /// <returns>The effect that owns the captured callback.</returns>
    private static InletSignalRActionEffect CreateSubscribedEffect(
        IProjectionFetcher fetcher,
        List<IAction> actions,
        out Func<string, string, long, Task> notification
    )
    {
        Mock<IInletStore> store = new();
        store.Setup(value => value.Dispatch(It.IsAny<IAction>())).Callback<IAction>(actions.Add);
        Mock<IProjectionDtoRegistry> registry = new();
        registry.Setup(value => value.GetDtoType(ProjectionPath)).Returns(typeof(TestProjection));
        Mock<IHubConnectionProvider> provider = new();
        Func<string, string, long, Task>? handler = null;
        provider.Setup(value => value.RegisterHandler(
                InletHubConstants.ProjectionUpdatedMethod,
                It.IsAny<Func<string, string, long, Task>>()))
            .Callback<string, Func<string, string, long, Task>>((_, callback) => handler = callback)
            .Returns(Mock.Of<IDisposable>());
        InletSignalRActionEffect effect = new(
            new(() => store.Object),
            provider.Object,
            fetcher,
            registry.Object,
            new FakeTimeProvider());

        // Seed an established subscription to isolate notification handling without network IO.
        FieldInfo? field = typeof(InletSignalRActionEffect).GetField(
            "activeSubscriptions",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        ConcurrentDictionary<(Type ProjectionType, string EntityId), string> subscriptions =
            Assert.IsType<ConcurrentDictionary<(Type ProjectionType, string EntityId), string>>(field.GetValue(effect));
        Assert.True(subscriptions.TryAdd((typeof(TestProjection), EntityId), "subscription-1"));
        Assert.NotNull(handler);
        notification = handler;
        return effect;
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
        await using InletSignalRActionEffect effect = CreateSubscribedEffect(
            fetcher.Object,
            actions,
            out Func<string, string, long, Task> notification);
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
        await using InletSignalRActionEffect effect = CreateSubscribedEffect(
            fetcher,
            actions,
            out Func<string, string, long, Task> notification);
        await notification(ProjectionPath, EntityId, notificationVersion);
        Assert.Equal(1, fetcher.FetchCount);
        Assert.Equal(typeof(TestProjection), fetcher.LastProjectionType);
        Assert.Equal(EntityId, fetcher.LastEntityId);
        AssertUpdatedState(actions, data, returnedVersion);
    }
}