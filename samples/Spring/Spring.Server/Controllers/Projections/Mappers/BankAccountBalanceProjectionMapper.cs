// =============================================================================
// HAND-CRAFTED REFERENCE IMPLEMENTATION
// =============================================================================
// This file contains the original hand-crafted version of this mapper, created
// before source generation was automated via Inlet.Server.Generators.
//
// Purpose:
// - Serves as a reference implementation to validate generator output
// - Enables test comparisons between generated and expected code
// - Documents the intended structure and behavior of the generated mapper
//
// The generator now produces this mapper automatically. This file is commented
// out to avoid duplicate type definitions but preserved for testing and
// documentation.
// =============================================================================

#if false
using System;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Inlet.Generators.Abstractions;

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
#endif