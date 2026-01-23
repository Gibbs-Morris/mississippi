#if false
using System;
using System.Net.Http;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Inlet.Blazor.WebAssembly.Abstractions.Effects;
using Mississippi.Inlet.Generators.Abstractions;

using Spring.Client.Features.BankAccountAggregate.Actions;
using Spring.Client.Features.BankAccountAggregate.Dtos;


namespace Spring.Client.Features.BankAccountAggregate.Effects;

/// <summary>
///     Effect that handles depositing funds into a bank account.
/// </summary>
[PendingSourceGenerator]
internal sealed class DepositFundsEffect
    : CommandEffectBase<DepositFundsAction, DepositFundsRequestDto, DepositFundsExecutingAction,
        DepositFundsSucceededAction, DepositFundsFailedAction>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DepositFundsEffect" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for API calls.</param>
    /// <param name="mapper">The mapper for action-to-DTO conversion.</param>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    public DepositFundsEffect(
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