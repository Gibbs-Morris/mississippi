using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Sdk.Generators.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Commands;


namespace Spring.Server.Controllers.Aggregates.Mappers;

/// <summary>
///     Maps <see cref="DepositFundsDto" /> to <see cref="DepositFunds" /> command.
/// </summary>
[PendingSourceGenerator]
internal sealed class DepositFundsDtoMapper : IMapper<DepositFundsDto, DepositFunds>
{
    /// <inheritdoc />
    public DepositFunds Map(
        DepositFundsDto input
    ) =>
        new()
        {
            Amount = input.Amount,
        };
}