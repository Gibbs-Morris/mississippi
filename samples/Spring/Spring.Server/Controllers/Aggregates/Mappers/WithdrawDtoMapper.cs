using Mississippi.Common.Abstractions.Attributes;
using Mississippi.Common.Abstractions.Mapping;

using Spring.Domain.Aggregates.BankAccount.Commands;


namespace Spring.Server.Controllers.Aggregates.Mappers;

/// <summary>
///     Maps <see cref="WithdrawDto" /> to <see cref="WithdrawFunds" /> command.
/// </summary>
[PendingSourceGenerator]
internal sealed class WithdrawDtoMapper : IMapper<WithdrawDto, WithdrawFunds>
{
    /// <inheritdoc />
    public WithdrawFunds Map(
        WithdrawDto input
    ) =>
        new()
        {
            Amount = input.Amount,
        };
}