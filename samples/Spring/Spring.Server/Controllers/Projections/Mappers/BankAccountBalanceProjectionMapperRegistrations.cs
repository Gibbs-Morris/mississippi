// =============================================================================
// HAND-CRAFTED REFERENCE IMPLEMENTATION
// =============================================================================
// This file contains the original hand-crafted version of this registration,
// created before source generation was automated via Inlet.Server.Generators.
//
// Purpose:
// - Serves as a reference implementation to validate generator output
// - Enables test comparisons between generated and expected code
// - Documents the intended DI registration pattern for the generated mapper
//
// The generator now produces this registration automatically. This file is
// commented out to avoid duplicate type definitions but preserved for testing
// and documentation.
// =============================================================================

#if false
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Inlet.Generators.Abstractions;

using Spring.Domain.Projections.BankAccountBalance;


namespace Spring.Server.Controllers.Projections.Mappers;

/// <summary>
///     Service registration for BankAccountBalance projection mappers.
/// </summary>
[PendingSourceGenerator]
internal static class BankAccountBalanceProjectionMapperRegistrations
{
    /// <summary>
    ///     Adds BankAccountBalance projection mappers to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBankAccountBalanceProjectionMappers(
        this IServiceCollection services
    )
    {
        services.AddMapper<BankAccountBalanceProjection, BankAccountBalanceDto, BankAccountBalanceProjectionMapper>();
        return services;
    }
}
#endif