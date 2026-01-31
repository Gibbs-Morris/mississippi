using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Inlet.Client.Reducers;
using Mississippi.Reservoir;

using Spring.Client.Features.BankAccountBalance.Dtos;
using Spring.Client.Features.BankAccountLedger.Dtos;
using Spring.Client.Features.FlaggedTransactions.Dtos;


namespace Spring.Client.Features;

/// <summary>
///     Extension methods for registering projection reducers.
/// </summary>
public static class ProjectionsFeatureRegistration
{
    /// <summary>
    ///     Adds projection reducers for all known projection DTOs.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddProjectionsFeature(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        RegisterProjectionReducers<BankAccountBalanceProjectionDto>(services);
        RegisterProjectionReducers<BankAccountLedgerProjectionDto>(services);
        RegisterProjectionReducers<FlaggedTransactionsProjectionDto>(services);
        return services;
    }

    private static void RegisterProjectionReducers<T>(
        IServiceCollection services
    )
        where T : class
    {
        services.AddReducer<ProjectionLoadingAction<T>, ProjectionsFeatureState>(ProjectionsReducer.ReduceLoading);
        services.AddReducer<ProjectionLoadedAction<T>, ProjectionsFeatureState>(ProjectionsReducer.ReduceLoaded);
        services.AddReducer<ProjectionUpdatedAction<T>, ProjectionsFeatureState>(ProjectionsReducer.ReduceUpdated);
        services.AddReducer<ProjectionErrorAction<T>, ProjectionsFeatureState>(ProjectionsReducer.ReduceError);
        services.AddReducer<ProjectionConnectionChangedAction<T>, ProjectionsFeatureState>(
            ProjectionsReducer.ReduceConnectionChanged);
    }
}