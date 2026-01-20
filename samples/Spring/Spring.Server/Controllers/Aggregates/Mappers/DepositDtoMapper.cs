using Mississippi.Common.Abstractions.Attributes;
using Mississippi.Common.Abstractions.Mapping;

using Spring.Domain.Aggregates.BankAccount.Commands;


namespace Spring.Server.Controllers.Aggregates.Mappers;

/// <summary>
///     Maps <see cref="DepositDto" /> to <see cref="DepositFunds" /> command.
/// </summary>
[PendingSourceGenerator]
internal sealed class DepositDtoMapper : IMapper<DepositDto, DepositFunds>
{
    /// <inheritdoc />
    public DepositFunds Map(
        DepositDto input
    ) =>
        new()
        {
            Amount = input.Amount,
        };
}