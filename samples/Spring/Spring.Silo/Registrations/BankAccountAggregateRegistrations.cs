// =============================================================================
// HAND-CRAFTED REFERENCE IMPLEMENTATION
// =============================================================================
// This file contains the original hand-crafted version of this registration,
// created before source generation was automated via Inlet.Silo.Generators.
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

using Mississippi.EventSourcing.Aggregates;
using Mississippi.EventSourcing.Reducers;
using Mississippi.EventSourcing.Snapshots;
using Mississippi.Inlet.Generators.Abstractions;

using Spring.Domain.Aggregates.BankAccount;
using Spring.Domain.Aggregates.BankAccount.Commands;
using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Aggregates.BankAccount.Handlers;
using Spring.Domain.Aggregates.BankAccount.Reducers;


namespace Spring.Silo.Registrations;

/// <summary>
///     Extension methods for registering bank account aggregate services.
/// </summary>
[PendingSourceGenerator]
public static class BankAccountAggregateRegistrations
{
    /// <summary>
    ///     Adds the bank account aggregate services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBankAccountAggregate(
        this IServiceCollection services
    )
    {
        // Add aggregate infrastructure
        services.AddAggregateSupport();

        // Register event types for hydration
        services.AddEventType<AccountOpened>();
        services.AddEventType<FundsDeposited>();
        services.AddEventType<FundsWithdrawn>();

        // Register command handlers
        services.AddCommandHandler<OpenAccount, BankAccountAggregate, OpenAccountHandler>();
        services.AddCommandHandler<DepositFunds, BankAccountAggregate, DepositFundsHandler>();
        services.AddCommandHandler<WithdrawFunds, BankAccountAggregate, WithdrawFundsHandler>();

        // Register reducers for state computation
        services.AddReducer<AccountOpened, BankAccountAggregate, AccountOpenedReducer>();
        services.AddReducer<FundsDeposited, BankAccountAggregate, FundsDepositedReducer>();
        services.AddReducer<FundsWithdrawn, BankAccountAggregate, FundsWithdrawnReducer>();

        // Add snapshot state converter for BankAccountAggregate (required for aggregate snapshots)
        services.AddSnapshotStateConverter<BankAccountAggregate>();
        return services;
    }
}
#endif