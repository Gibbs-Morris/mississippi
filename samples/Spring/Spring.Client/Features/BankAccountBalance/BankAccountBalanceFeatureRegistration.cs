using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;

using Spring.Client.Features.BankAccountBalance.Actions;
using Spring.Client.Features.BankAccountBalance.Effects;
using Spring.Client.Features.BankAccountBalance.Reducers;
using Spring.Client.Features.BankAccountBalance.State;


namespace Spring.Client.Features.BankAccountBalance;

/// <summary>
///     Extension methods for registering the BankAccountBalance feature.
/// </summary>
/// <remarks>
///     This feature handles the read side (projection queries) for the BankAccountBalance projection.
///     Derived from domain projection: <c>Spring.Domain.Projections.BankAccountBalance.BankAccountBalanceProjection</c>.
/// </remarks>
internal static class BankAccountBalanceFeatureRegistration
{
    /// <summary>
    ///     Adds the BankAccountBalance feature to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBankAccountBalanceFeature(
        this IServiceCollection services
    )
    {
        // Reducers
        services.AddReducer<BankAccountBalanceLoadingAction, BankAccountBalanceState>(
            BankAccountBalanceReducers.Loading);
        services.AddReducer<BankAccountBalanceLoadedAction, BankAccountBalanceState>(
            BankAccountBalanceReducers.Loaded);
        services.AddReducer<BankAccountBalanceFetchFailedAction, BankAccountBalanceState>(
            BankAccountBalanceReducers.FetchFailed);

        // Effects
        services.AddEffect<FetchBankAccountBalanceEffect>();
        return services;
    }
}
