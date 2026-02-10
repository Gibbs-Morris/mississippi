using System;

using Mississippi.Common.Abstractions.Builders;
using Mississippi.Inlet.Generators.Abstractions;

using Spring.Server.Controllers.Aggregates.Mappers;
using Spring.Server.Controllers.Projections.Mappers;


namespace Spring.Server.Registrations;

/// <summary>
///     Extension methods for registering the Spring domain server mappers.
/// </summary>
[PendingSourceGenerator("Generate a server-domain registration wrapper for Spring.")]
public static class SpringDomainServerRegistrations
{
    /// <summary>
    ///     Adds Spring domain mappers generated from the domain project.
    /// </summary>
    /// <param name="builder">The Mississippi server builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiServerBuilder AddSpringDomain(
        this IMississippiServerBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services =>
        {
            services.AddBankAccountAggregateMappers();
            services.AddBankAccountBalanceProjectionMappers();
            services.AddBankAccountLedgerProjectionMappers();
            services.AddFlaggedTransactionsProjectionMappers();
            services.AddMoneyTransferStatusProjectionMappers();
        });
        return builder;
    }
}
