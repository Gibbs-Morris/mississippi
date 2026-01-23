// =============================================================================
// HAND-CRAFTED REFERENCE IMPLEMENTATION
// =============================================================================
// This file contains the original hand-crafted version of this registration,
// created before source generation was automated via Sdk.Silo.Generators.
//
// Purpose:
// - Serves as a reference implementation to validate generator output
// - Enables test comparisons between generated and expected code
// - Documents the intended DI registration pattern for the generated registration
//
// The generator now produces this registration automatically. This file is
// commented out to avoid duplicate type definitions but preserved for testing
// and documentation.
// =============================================================================

#if false
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
#endif