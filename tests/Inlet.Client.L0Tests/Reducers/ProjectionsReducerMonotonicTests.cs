using System;

using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Inlet.Client.L0Tests.Helpers;
using Mississippi.Inlet.Client.Reducers;


namespace Mississippi.Inlet.Client.L0Tests.Reducers;

/// <summary>
///     Verifies version admission for successful projection completions.
/// </summary>
public sealed class ProjectionsReducerMonotonicTests
{
    private static ProjectionsFeatureState Complete(
        ProjectionsFeatureState state,
        string entityId,
        TestProjection? data,
        long version,
        bool updated
    ) =>
        updated
            ? ProjectionsReducer.ReduceUpdated(
                state,
                new ProjectionUpdatedAction<TestProjection>(entityId, data, version))
            : ProjectionsReducer.ReduceLoaded(
                state,
                new ProjectionLoadedAction<TestProjection>(entityId, data, version));

    /// <summary>
    ///     Equal and newer completions retain success semantics without changing connectivity.
    /// </summary>
    /// <param name="updated">Whether the completion is an update.</param>
    /// <param name="version">The returned version.</param>
    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 12)]
    [InlineData(false, 13)]
    [InlineData(true, 0)]
    [InlineData(true, 12)]
    [InlineData(true, long.MaxValue)]
    public void EqualAndNewerCompletionsClearTransientState(
        bool updated,
        long version
    )
    {
        ProjectionEntry<TestProjection> previous = new(
            new()
            {
                Name = "previous",
            },
            Math.Min(version, 12),
            true,
            true,
            new InvalidOperationException());
        ProjectionsFeatureState state = new ProjectionsFeatureState().WithEntry("entity", previous);
        TestProjection data = new()
        {
            Name = "completed",
        };
        ProjectionsFeatureState result = Complete(state, "entity", data, version, updated);
        Assert.Same(data, result.GetProjection<TestProjection>("entity"));
        Assert.Equal(version, result.GetProjectionVersion<TestProjection>("entity"));
        Assert.False(result.IsProjectionLoading<TestProjection>("entity"));
        Assert.Null(result.GetProjectionError<TestProjection>("entity"));
        Assert.True(result.IsProjectionConnected<TestProjection>("entity"));
        Assert.Same(previous, state.GetEntry<TestProjection>("entity"));
    }

    /// <summary>
    ///     First reads accept the empty sentinel and version zero, including not-found data.
    /// </summary>
    /// <param name="updated">Whether the completion is an update.</param>
    /// <param name="version">The returned version.</param>
    [Theory]
    [InlineData(false, -1)]
    [InlineData(false, 0)]
    [InlineData(true, -1)]
    [InlineData(true, 0)]
    public void FirstCompletionAcceptsUninitializedAndZeroVersions(
        bool updated,
        long version
    )
    {
        ProjectionsFeatureState state = ProjectionsReducer.ReduceLoading(
            new(),
            new ProjectionLoadingAction<TestProjection>("entity"));
        Assert.Equal(-1, state.GetProjectionVersion<TestProjection>("entity"));
        ProjectionsFeatureState result = Complete(state, "entity", null, version, updated);
        Assert.Null(result.GetProjection<TestProjection>("entity"));
        Assert.Equal(version, result.GetProjectionVersion<TestProjection>("entity"));
        Assert.False(result.IsProjectionLoading<TestProjection>("entity"));
        Assert.Null(result.GetProjectionError<TestProjection>("entity"));
        Assert.Equal(-1, state.GetProjectionVersion<TestProjection>("entity"));
    }

    /// <summary>
    ///     Older completions cannot replace a newer accepted pair in any action order.
    /// </summary>
    /// <param name="firstUpdated">Whether the newer completion is an update.</param>
    /// <param name="lastUpdated">Whether the older completion is an update.</param>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void OlderCompletionKeepsNewerPair(
        bool firstUpdated,
        bool lastUpdated
    )
    {
        TestProjection newer = new()
        {
            Name = "version-12",
        };
        ProjectionsFeatureState state = Complete(new(), "entity", newer, 12, firstUpdated);
        ProjectionEntry<TestProjection>? accepted = state.GetEntry<TestProjection>("entity");
        ProjectionsFeatureState result = Complete(
            state,
            "entity",
            new()
            {
                Name = "version-10",
            },
            10,
            lastUpdated);
        Assert.Same(newer, result.GetProjection<TestProjection>("entity"));
        Assert.Equal(12, result.GetProjectionVersion<TestProjection>("entity"));
        Assert.Same(accepted, result.GetEntry<TestProjection>("entity"));
    }

    /// <summary>
    ///     Stale success leaves loading, error, connection and nullable data unchanged.
    /// </summary>
    /// <param name="updated">Whether the stale completion is an update.</param>
    /// <param name="isLoading">The current loading flag.</param>
    /// <param name="isConnected">The current connection flag.</param>
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void OlderCompletionPreservesEntireEntry(
        bool updated,
        bool isLoading,
        bool isConnected
    )
    {
        ProjectionEntry<TestProjection> accepted = new(
            null,
            12,
            isLoading,
            isConnected,
            new InvalidOperationException("newer error"));
        ProjectionsFeatureState state = new ProjectionsFeatureState().WithEntry("entity", accepted);
        ProjectionsFeatureState result = Complete(
            state,
            "entity",
            new()
            {
                Name = "obsolete",
            },
            10,
            updated);
        Assert.Same(accepted, result.GetEntry<TestProjection>("entity"));
        Assert.Same(accepted, state.GetEntry<TestProjection>("entity"));
    }

    /// <summary>
    ///     Admission compares only the matching projection type and entity key.
    /// </summary>
    /// <param name="updated">Whether the completion is an update.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OtherTypesAndEntitiesDoNotBlockCompletion(
        bool updated
    )
    {
        ProjectionsFeatureState state = Complete(
            new(),
            "other",
            new()
            {
                Name = "other entity",
            },
            100,
            updated);
        state = ProjectionsReducer.ReduceLoaded(state, new ProjectionLoadedAction<string>("entity", "other type", 100));
        ProjectionEntry<TestProjection>? otherEntity = state.GetEntry<TestProjection>("other");
        ProjectionEntry<string>? otherType = state.GetEntry<string>("entity");
        TestProjection data = new()
        {
            Name = "version-zero",
        };
        ProjectionsFeatureState result = Complete(state, "entity", data, 0, updated);
        Assert.Same(data, result.GetProjection<TestProjection>("entity"));
        Assert.Equal(0, result.GetProjectionVersion<TestProjection>("entity"));
        Assert.Same(otherEntity, result.GetEntry<TestProjection>("other"));
        Assert.Same(otherType, result.GetEntry<string>("entity"));
    }
}