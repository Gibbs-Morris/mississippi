using System;

using Mississippi.Inlet.Generators.Abstractions;
using Mississippi.Reservoir.Abstractions.Builders;

using Spring.Client.Features;
using Spring.Client.Features.BankAccountAggregate;
using Spring.Client.Features.MoneyTransferSaga;


namespace Spring.Client.Registrations;

/// <summary>
///     Extension methods for registering the Spring domain client features.
/// </summary>
[PendingSourceGenerator("Generate a client-domain registration wrapper for Spring.")]
public static class SpringDomainClientRegistrations
{
    /// <summary>
    ///     Adds Spring domain features generated from the domain project.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    public static IReservoirBuilder AddSpringDomain(
        this IReservoirBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddBankAccountAggregateFeature();
        builder.AddMoneyTransferSagaFeature();
        builder.AddProjectionsFeature();
        return builder;
    }
}
