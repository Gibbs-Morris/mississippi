using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Aggregates;
using Mississippi.EventSourcing.Reducers;
using Mississippi.EventSourcing.Snapshots;
using Mississippi.Sdk.Generators.Abstractions;

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