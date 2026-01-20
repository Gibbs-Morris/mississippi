using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Attributes;
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Reservoir;

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
///     This feature handles the write side (command execution) for the BankAccount aggregate.
///     Derived from domain aggregate: <c>Spring.Domain.Aggregates.BankAccount</c>.
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

        // Reducers
        services.AddReducer<SetAccountIdAction, BankAccountAggregateState>(BankAccountAggregateReducers.SetAccountId);
        services.AddReducer<CommandExecutingAction, BankAccountAggregateState>(
            BankAccountAggregateReducers.CommandExecuting);
        services.AddReducer<CommandSucceededAction, BankAccountAggregateState>(
            BankAccountAggregateReducers.CommandSucceeded);
        services.AddReducer<CommandFailedAction, BankAccountAggregateState>(BankAccountAggregateReducers.CommandFailed);

        // Effects
        services.AddEffect<OpenAccountEffect>();
        services.AddEffect<DepositFundsEffect>();
        services.AddEffect<WithdrawFundsEffect>();
        return services;
    }
}
