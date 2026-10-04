using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Inlet.Client.Reducers;
using Mississippi.Reservoir.Abstractions;

using MississippiSamples.Spring.Client.Features.BankAccountBalance.Dtos;
using MississippiSamples.Spring.Client.Features.BankAccountLedger.Dtos;
using MississippiSamples.Spring.Client.Features.FlaggedTransactions.Dtos;
using MississippiSamples.Spring.Client.Features.MoneyTransferStatus.Dtos;


namespace MississippiSamples.Spring.Client.Features;

/// <summary>
///     Extension methods for registering projection reducers.
/// </summary>
internal static class ProjectionsFeatureRegistration
{
    /// <summary>
    ///     Adds projection reducers for all known projection DTOs.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IReservoirBuilder AddProjectionsFeature(
        this IReservoirBuilder builder
    )
    {
        builder.AddFeatureState<ProjectionsFeatureState>(feature => feature
            .AddProjectionReducers<BankAccountBalanceProjectionDto>()
            .AddProjectionReducers<BankAccountLedgerProjectionDto>()
            .AddProjectionReducers<FlaggedTransactionsProjectionDto>()
            .AddProjectionReducers<MoneyTransferStatusProjectionDto>());
        return builder;
    }

    private static IReservoirFeatureBuilder<ProjectionsFeatureState> AddProjectionReducers<T>(
        this IReservoirFeatureBuilder<ProjectionsFeatureState> feature
    )
        where T : class =>
        feature.AddReducer<ProjectionLoadingAction<T>>(ProjectionsReducer.ReduceLoading)
            .AddReducer<ProjectionLoadedAction<T>>(ProjectionsReducer.ReduceLoaded)
            .AddReducer<ProjectionUpdatedAction<T>>(ProjectionsReducer.ReduceUpdated)
            .AddReducer<ProjectionErrorAction<T>>(ProjectionsReducer.ReduceError)
            .AddReducer<ProjectionConnectionChangedAction<T>>(ProjectionsReducer.ReduceConnectionChanged);
}