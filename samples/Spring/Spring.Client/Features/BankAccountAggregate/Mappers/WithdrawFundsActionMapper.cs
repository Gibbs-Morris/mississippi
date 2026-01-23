#if false // Replaced by source generator: CommandClientMappersGenerator
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Inlet.Generators.Abstractions;

using Spring.Client.Features.BankAccountAggregate.Actions;
using Spring.Client.Features.BankAccountAggregate.Dtos;


namespace Spring.Client.Features.BankAccountAggregate.Mappers;

/// <summary>
///     Maps <see cref="WithdrawFundsAction" /> to <see cref="WithdrawFundsRequestDto" />.
/// </summary>
[PendingSourceGenerator]
internal sealed class WithdrawFundsActionMapper : IMapper<WithdrawFundsAction, WithdrawFundsRequestDto>
{
    /// <inheritdoc />
    public WithdrawFundsRequestDto Map(
        WithdrawFundsAction input
    ) =>
        new(input.Amount);
}
#endif