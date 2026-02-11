using System;

using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Inlet.Client.Reducers;
using Mississippi.Reservoir.Abstractions.Builders;

using Spring.Client.Features.BankAccountBalance.Dtos;
using Spring.Client.Features.BankAccountLedger.Dtos;
using Spring.Client.Features.FlaggedTransactions.Dtos;
using Spring.Client.Features.MoneyTransferStatus.Dtos;


namespace Spring.Client.Features;

/// <summary>
///     Extension methods for registering projection reducers.
/// </summary>
public static class ProjectionsFeatureRegistration
{
    /// <summary>
    ///     Adds projection reducers for all known projection DTOs.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    public static IReservoirBuilder AddProjectionsFeature(
        this IReservoirBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddFeature<ProjectionsFeatureState>(featureBuilder =>
        {
            RegisterProjectionReducers<BankAccountBalanceProjectionDto>(featureBuilder);
            RegisterProjectionReducers<BankAccountLedgerProjectionDto>(featureBuilder);
            RegisterProjectionReducers<FlaggedTransactionsProjectionDto>(featureBuilder);
            RegisterProjectionReducers<MoneyTransferStatusProjectionDto>(featureBuilder);
        });
        return builder;
    }

    private static void RegisterProjectionReducers<T>(
        IReservoirFeatureBuilder<ProjectionsFeatureState> featureBuilder
    )
        where T : class
    {
        featureBuilder.AddReducer<ProjectionLoadingAction<T>>(ProjectionsReducer.ReduceLoading);
        featureBuilder.AddReducer<ProjectionLoadedAction<T>>(ProjectionsReducer.ReduceLoaded);
        featureBuilder.AddReducer<ProjectionUpdatedAction<T>>(ProjectionsReducer.ReduceUpdated);
        featureBuilder.AddReducer<ProjectionErrorAction<T>>(ProjectionsReducer.ReduceError);
        featureBuilder.AddReducer<ProjectionConnectionChangedAction<T>>(ProjectionsReducer.ReduceConnectionChanged);
    }
}