using Mississippi.Common.Abstractions.Attributes;
using Mississippi.Common.Abstractions.Mapping;

using Spring.Domain.Aggregates.BankAccount.Commands;


namespace Spring.Server.Controllers.Aggregates.Mappers;

/// <summary>
///     Maps <see cref="WithdrawFundsDto" /> to <see cref="WithdrawFunds" /> command.
/// </summary>
[PendingSourceGenerator]
internal sealed class WithdrawFundsDtoMapper : IMapper<WithdrawFundsDto, WithdrawFunds>
{
    /// <inheritdoc />
    public WithdrawFunds Map(
        WithdrawFundsDto input
    ) =>
        new()
        {
            Amount = input.Amount,
        };
}