using System.Net.Http;

using Mississippi.Common.Abstractions.Attributes;
using Mississippi.Common.Abstractions.Mapping;

using Spring.Client.Features.BankAccountAggregate.Actions;
using Spring.Client.Features.BankAccountAggregate.Dtos;


namespace Spring.Client.Features.BankAccountAggregate.Effects;

/// <summary>
///     Effect that handles depositing funds into a bank account.
/// </summary>
[PendingSourceGenerator]
internal sealed class DepositFundsEffect : CommandEffectBase<DepositFundsAction, DepositFundsRequestDto>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DepositFundsEffect" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for API calls.</param>
    /// <param name="mapper">The mapper for action-to-DTO conversion.</param>
    public DepositFundsEffect(
        HttpClient httpClient,
        IMapper<DepositFundsAction, DepositFundsRequestDto> mapper
    )
        : base(httpClient, mapper)
    {
    }

    /// <inheritdoc />
    protected override string GetEndpoint(
        DepositFundsAction action
    ) =>
        $"/api/aggregates/bankaccount/{action.AccountId}/deposit";
}
