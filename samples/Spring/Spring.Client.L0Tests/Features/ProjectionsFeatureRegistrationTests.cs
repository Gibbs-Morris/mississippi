using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Core;

using MississippiSamples.Spring.Client.Features;
using MississippiSamples.Spring.Client.Features.AuthProof.Dtos;


namespace MississippiSamples.Spring.Client.L0Tests.Features;

/// <summary>Protects Spring's hand-written projection registration against omitted generated DTOs.</summary>
public sealed class ProjectionsFeatureRegistrationTests
{
    /// <summary>Verify that real Auth Proof actions expose loading, denied reads, observed data and its server version.</summary>
    [Fact]
    public void AuthProofProjectionActionsUpdateRegisteredStore()
    {
        ServiceCollection services = new();
        services.AddReservoir().AddProjectionsFeature();
        using ServiceProvider provider = services.BuildServiceProvider();
        IStore store = provider.GetRequiredService<IStore>();
        const string entityId = "registration-proof";
        store.Dispatch(new ProjectionLoadingAction<AuthProofProjectionDto>(entityId));
        Assert.True(store.GetState<ProjectionsFeatureState>().IsProjectionLoading<AuthProofProjectionDto>(entityId));
        InvalidOperationException denied = new("Projection read was denied.");
        store.Dispatch(new ProjectionErrorAction<AuthProofProjectionDto>(entityId, denied));
        Assert.Same(
            denied,
            store.GetState<ProjectionsFeatureState>().GetProjectionError<AuthProofProjectionDto>(entityId));
        Assert.False(store.GetState<ProjectionsFeatureState>().IsProjectionLoading<AuthProofProjectionDto>(entityId));
        store.Dispatch(new ProjectionUpdatedAction<AuthProofProjectionDto>(entityId, new(4), 7));
        ProjectionsFeatureState state = store.GetState<ProjectionsFeatureState>();
        Assert.Equal(4, state.GetProjection<AuthProofProjectionDto>(entityId)?.AuthenticatedAccessCount);
        Assert.Equal(7, state.GetProjectionVersion<AuthProofProjectionDto>(entityId));
        Assert.Null(state.GetProjectionError<AuthProofProjectionDto>(entityId));
        Assert.False(state.IsProjectionLoading<AuthProofProjectionDto>(entityId));
    }
}