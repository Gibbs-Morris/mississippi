using System;

using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Inlet.Client.Reducers;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Core;

using MississippiSamples.Spring.Client.Features.BankAccountBalance.Dtos;
using MississippiSamples.Spring.Client.Features.BankAccountLedger.Dtos;
using MississippiSamples.Spring.Client.Features.FlaggedTransactions.Dtos;
using MississippiSamples.Spring.Client.Features.MoneyTransferStatus.Dtos;


namespace MississippiSamples.Spring.Client.Features;

/// <summary>
///     Extension methods for registering projection reducers.
/// </summary>
public static class ProjectionsFeatureRegistration
{
    /// <summary>
    ///     Adds projection reducers for all known projection DTOs.
    /// </summary>
    /// <param name="reservoir">The Reservoir builder.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    public static IReservoirBuilder AddProjectionsFeature(
        this IReservoirBuilder reservoir
    )
    {
        ArgumentNullException.ThrowIfNull(reservoir);
        reservoir.AddFeature<ProjectionsFeatureState>(feature =>
        {
            RegisterProjectionReducers<BankAccountBalanceProjectionDto>(feature);
            RegisterProjectionReducers<BankAccountLedgerProjectionDto>(feature);
            RegisterProjectionReducers<FlaggedTransactionsProjectionDto>(feature);
            RegisterProjectionReducers<MoneyTransferStatusProjectionDto>(feature);
        });
        return reservoir;
    }

    private static void RegisterProjectionReducers<T>(
        IFeatureStateBuilder<ProjectionsFeatureState> feature
    )
        where T : class
    {
        feature.AddReducer<ProjectionsFeatureState, ProjectionLoadingAction<T>>(ProjectionsReducer.ReduceLoading);
        feature.AddReducer<ProjectionsFeatureState, ProjectionLoadedAction<T>>(ProjectionsReducer.ReduceLoaded);
        feature.AddReducer<ProjectionsFeatureState, ProjectionUpdatedAction<T>>(ProjectionsReducer.ReduceUpdated);
        feature.AddReducer<ProjectionsFeatureState, ProjectionErrorAction<T>>(ProjectionsReducer.ReduceError);
        feature.AddReducer<ProjectionsFeatureState, ProjectionConnectionChangedAction<T>>(
            ProjectionsReducer.ReduceConnectionChanged);
    }
}