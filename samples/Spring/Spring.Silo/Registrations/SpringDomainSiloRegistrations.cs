using System;

using Mississippi.Common.Abstractions.Builders;
using Mississippi.Inlet.Generators.Abstractions;


namespace Spring.Silo.Registrations;

/// <summary>
///     Extension methods for registering the Spring domain silo services.
/// </summary>
[PendingSourceGenerator("Generate a silo-domain registration wrapper for Spring.")]
public static class SpringDomainSiloRegistrations
{
    /// <summary>
    ///     Adds Spring domain silo registrations generated from the domain project.
    /// </summary>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiSiloBuilder AddSpringDomain(
        this IMississippiSiloBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddBankAccountAggregate();
        builder.AddTransactionInvestigationQueueAggregate();
        builder.AddMoneyTransferSaga();
        builder.AddBankAccountBalanceProjection();
        builder.AddBankAccountLedgerProjection();
        builder.AddFlaggedTransactionsProjection();
        builder.AddMoneyTransferStatusProjection();
        return builder;
    }
}
