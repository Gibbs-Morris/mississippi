#if false
using System;
using System.Net.Http;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Inlet.Blazor.WebAssembly.Abstractions.ActionEffects;
using Mississippi.Inlet.Generators.Abstractions;

using Spring.Client.Features.BankAccountAggregate.Actions;
using Spring.Client.Features.BankAccountAggregate.Dtos;
using Spring.Client.Features.BankAccountAggregate.State;


namespace Spring.Client.Features.BankAccountAggregate.ActionEffects;

/// <summary>
///     Action effect that handles depositing funds into a bank account.
/// </summary>
[PendingSourceGenerator]
internal sealed class DepositFundsActionEffect
    : CommandActionEffectBase<DepositFundsAction, DepositFundsRequestDto, BankAccountAggregateState,
        DepositFundsExecutingAction, DepositFundsSucceededAction, DepositFundsFailedAction>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DepositFundsActionEffect" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for API calls.</param>
    /// <param name="mapper">The mapper for action-to-DTO conversion.</param>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    public DepositFundsActionEffect(
        HttpClient httpClient,
        IMapper<DepositFundsAction, DepositFundsRequestDto> mapper,
        TimeProvider? timeProvider = null
    )
        : base(httpClient, mapper, timeProvider)
    {
    }

    /// <inheritdoc />
    protected override string AggregateRoutePrefix => "/api/aggregates/bank-account";

    /// <inheritdoc />
    protected override string Route => "deposit";
}
#endif