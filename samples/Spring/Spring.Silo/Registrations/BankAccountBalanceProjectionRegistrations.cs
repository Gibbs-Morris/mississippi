using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Reducers;
using Mississippi.EventSourcing.Snapshots;
using Mississippi.EventSourcing.UxProjections;
using Mississippi.Sdk.Generators.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Projections.BankAccountBalance;
using Spring.Domain.Projections.BankAccountBalance.Reducers;


namespace Spring.Silo.Registrations;

/// <summary>
///     Extension methods for registering bank account balance projection services.
/// </summary>
[PendingSourceGenerator]
public static class BankAccountBalanceProjectionRegistrations
{
    /// <summary>
    ///     Adds the bank account balance projection services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBankAccountBalanceProjection(
        this IServiceCollection services
    )
    {
        // Add UX projection infrastructure
        services.AddUxProjections();

        // Register reducers for BankAccountBalanceProjection
        services.AddReducer<AccountOpened, BankAccountBalanceProjection, AccountOpenedBalanceReducer>();
        services.AddReducer<FundsDeposited, BankAccountBalanceProjection, FundsDepositedBalanceReducer>();
        services.AddReducer<FundsWithdrawn, BankAccountBalanceProjection, FundsWithdrawnBalanceReducer>();

        // Add snapshot state converter for BankAccountBalanceProjection
        services.AddSnapshotStateConverter<BankAccountBalanceProjection>();
        return services;
    }
}