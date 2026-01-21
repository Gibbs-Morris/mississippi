using System.Net.Http;

using Mississippi.Common.Abstractions.Attributes;
using Mississippi.Common.Abstractions.Mapping;

using Spring.Client.Features.BankAccountAggregate.Actions;
using Spring.Client.Features.BankAccountAggregate.Dtos;


namespace Spring.Client.Features.BankAccountAggregate.Effects;

/// <summary>
///     Effect that handles opening a bank account.
/// </summary>
[PendingSourceGenerator]
internal sealed class OpenAccountEffect : CommandEffectBase<OpenAccountAction, OpenAccountRequestDto>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="OpenAccountEffect" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for API calls.</param>
    /// <param name="mapper">The mapper for action-to-DTO conversion.</param>
    public OpenAccountEffect(
        HttpClient httpClient,
        IMapper<OpenAccountAction, OpenAccountRequestDto> mapper
    )
        : base(httpClient, mapper)
    {
    }

    /// <inheritdoc />
    protected override string GetEndpoint(
        OpenAccountAction action
    ) =>
        $"/api/aggregates/bankaccount/{action.EntityId}/open";
}