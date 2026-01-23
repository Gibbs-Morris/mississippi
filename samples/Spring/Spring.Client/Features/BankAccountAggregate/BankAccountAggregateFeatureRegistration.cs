#if false
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Reservoir;
using Mississippi.Sdk.Generators.Abstractions;

using Spring.Client.Features.BankAccountAggregate.Actions;
using Spring.Client.Features.BankAccountAggregate.Dtos;
using Spring.Client.Features.BankAccountAggregate.Effects;
using Spring.Client.Features.BankAccountAggregate.Mappers;
using Spring.Client.Features.BankAccountAggregate.Reducers;
using Spring.Client.Features.BankAccountAggregate.State;


namespace Spring.Client.Features.BankAccountAggregate;

/// <summary>
///     Extension methods for registering the BankAccountAggregate feature.
/// </summary>
/// <remarks>
///     <para>
///         This feature handles the write side (command execution) for the BankAccount aggregate.
///         Derived from domain aggregate: <c>Spring.Domain.Aggregates.BankAccount</c>.
///     </para>
///     <para>
///         Includes application-specific <see cref="SetEntityIdAction" /> registration for
///         tracking which entity is selected in the UI.
///     </para>
/// </remarks>
[PendingSourceGenerator]
internal static class BankAccountAggregateFeatureRegistration
{
    /// <summary>
    ///     Adds the BankAccountAggregate feature to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBankAccountAggregateFeature(
        this IServiceCollection services
    )
    {
        // Mappers (Action → DTO)
        services.AddMapper<OpenAccountAction, OpenAccountRequestDto, OpenAccountActionMapper>();
        services.AddMapper<DepositFundsAction, DepositFundsRequestDto, DepositFundsActionMapper>();
        services.AddMapper<WithdrawFundsAction, WithdrawFundsRequestDto, WithdrawFundsActionMapper>();

        // Reducers - EntityId
        services.AddReducer<SetEntityIdAction, BankAccountAggregateState>(BankAccountAggregateReducers.SetEntityId);

        // Reducers - OpenAccount
        services.AddReducer<OpenAccountExecutingAction, BankAccountAggregateState>(
            BankAccountAggregateReducers.OpenAccountExecuting);
        services.AddReducer<OpenAccountSucceededAction, BankAccountAggregateState>(
            BankAccountAggregateReducers.OpenAccountSucceeded);
        services.AddReducer<OpenAccountFailedAction, BankAccountAggregateState>(
            BankAccountAggregateReducers.OpenAccountFailed);

        // Reducers - DepositFunds
        services.AddReducer<DepositFundsExecutingAction, BankAccountAggregateState>(
            BankAccountAggregateReducers.DepositFundsExecuting);
        services.AddReducer<DepositFundsSucceededAction, BankAccountAggregateState>(
            BankAccountAggregateReducers.DepositFundsSucceeded);
        services.AddReducer<DepositFundsFailedAction, BankAccountAggregateState>(
            BankAccountAggregateReducers.DepositFundsFailed);

        // Reducers - WithdrawFunds
        services.AddReducer<WithdrawFundsExecutingAction, BankAccountAggregateState>(
            BankAccountAggregateReducers.WithdrawFundsExecuting);
        services.AddReducer<WithdrawFundsSucceededAction, BankAccountAggregateState>(
            BankAccountAggregateReducers.WithdrawFundsSucceeded);
        services.AddReducer<WithdrawFundsFailedAction, BankAccountAggregateState>(
            BankAccountAggregateReducers.WithdrawFundsFailed);

        // Effects
        services.AddEffect<OpenAccountEffect>();
        services.AddEffect<DepositFundsEffect>();
        services.AddEffect<WithdrawFundsEffect>();
        return services;
    }
}
#endif