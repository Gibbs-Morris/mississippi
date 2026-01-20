using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Attributes;
using Mississippi.Common.Abstractions.Mapping;

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
