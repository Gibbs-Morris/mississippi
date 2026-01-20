using System;

using Mississippi.Common.Abstractions.Attributes;
using Mississippi.Common.Abstractions.Mapping;

using Spring.Domain.Projections.BankAccountBalance;


namespace Spring.Server.Controllers.Projections.Mappers;

/// <summary>
///     Maps <see cref="BankAccountBalanceProjection" /> to <see cref="BankAccountBalanceDto" />.
/// </summary>
[PendingSourceGenerator]
internal sealed class BankAccountBalanceProjectionMapper : IMapper<BankAccountBalanceProjection, BankAccountBalanceDto>
{
    /// <inheritdoc />
    public BankAccountBalanceDto Map(
        BankAccountBalanceProjection source
    )
    {
        ArgumentNullException.ThrowIfNull(source);
        return new()
        {
            Balance = source.Balance,
            IsOpen = source.IsOpen,
            HolderName = source.HolderName,
        };
    }
}