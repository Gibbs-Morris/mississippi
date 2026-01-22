#if FALSE // Replaced by source generator: CommandClientMappersGenerator
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Sdk.Generators.Abstractions;

using Spring.Client.Features.BankAccountAggregate.Actions;
using Spring.Client.Features.BankAccountAggregate.Dtos;


namespace Spring.Client.Features.BankAccountAggregate.Mappers;

/// <summary>
///     Maps <see cref="DepositFundsAction" /> to <see cref="DepositFundsRequestDto" />.
/// </summary>
[PendingSourceGenerator]
internal sealed class DepositFundsActionMapper : IMapper<DepositFundsAction, DepositFundsRequestDto>
{
    /// <inheritdoc />
    public DepositFundsRequestDto Map(
        DepositFundsAction input
    ) =>
        new(input.Amount);
}
#endif