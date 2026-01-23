// NOTE: This file has been replaced by source generation.
// The generated version is in: obj/{Configuration}/net10.0/generated/Mississippi.Inlet.Server.Generators/
//     Mississippi.Inlet.Server.Generators.CommandServerDtoGenerator/WithdrawFundsDtoMapper.g.cs
// Keeping this file commented out for reference during the generator development phase.

#if false // Replaced by generated code
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Inlet.Generators.Abstractions;

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
#endif