// NOTE: This file has been replaced by source generation.
// The generated version is in: obj/{Configuration}/net10.0/generated/Mississippi.Inlet.Server.Generators/
//     Mississippi.Inlet.Server.Generators.CommandServerDtoGenerator/BankAccountAggregateMapperRegistrations.g.cs
// Keeping this file commented out for reference during the generator development phase.

#if false // Replaced by generated code
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Inlet.Generators.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Commands;


namespace Spring.Server.Controllers.Aggregates.Mappers;

/// <summary>
///     Service registration for BankAccount aggregate DTO mappers.
/// </summary>
[PendingSourceGenerator]
internal static class BankAccountAggregateMapperRegistrations
{
    /// <summary>
    ///     Adds BankAccount aggregate DTO to command mappers to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBankAccountAggregateMappers(
        this IServiceCollection services
    )
    {
        services.AddMapper<OpenAccountDto, OpenAccount, OpenAccountDtoMapper>();
        services.AddMapper<DepositFundsDto, DepositFunds, DepositFundsDtoMapper>();
        services.AddMapper<WithdrawFundsDto, WithdrawFunds, WithdrawFundsDtoMapper>();
        return services;
    }
}
#endif