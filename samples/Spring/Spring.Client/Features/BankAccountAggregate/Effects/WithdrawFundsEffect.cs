using System.Net.Http;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Sdk.Generators.Abstractions;

using Spring.Client.Features.BankAccountAggregate.Actions;
using Spring.Client.Features.BankAccountAggregate.Dtos;


namespace Spring.Client.Features.BankAccountAggregate.Effects;

/// <summary>
///     Effect that handles withdrawing funds from a bank account.
/// </summary>
[PendingSourceGenerator]
internal sealed class WithdrawFundsEffect : CommandEffectBase<WithdrawFundsAction, WithdrawFundsRequestDto>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="WithdrawFundsEffect" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for API calls.</param>
    /// <param name="mapper">The mapper for action-to-DTO conversion.</param>
    public WithdrawFundsEffect(
        HttpClient httpClient,
        IMapper<WithdrawFundsAction, WithdrawFundsRequestDto> mapper
    )
        : base(httpClient, mapper)
    {
    }

    /// <inheritdoc />
    protected override string GetEndpoint(
        WithdrawFundsAction action
    ) =>
        $"/api/aggregates/bankaccount/{action.EntityId}/withdraw";
}