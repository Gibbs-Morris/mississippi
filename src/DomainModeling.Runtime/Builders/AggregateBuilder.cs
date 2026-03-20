using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Diagnostics;
using Mississippi.DomainModeling.Abstractions.Builders;


namespace Mississippi.DomainModeling.Runtime.Builders;

/// <summary>
///     Default implementation of <see cref="IAggregateBuilder" />.
/// </summary>
public sealed class AggregateBuilder : IAggregateBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AggregateBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection for registering aggregate services.</param>
    public AggregateBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    /// <summary>
    ///     Gets the set of aggregate types that have been registered.
    /// </summary>
    internal HashSet<Type> RegisteredAggregates { get; } = [];

    /// <summary>
    ///     Gets the service collection for aggregate service registration.
    /// </summary>
    internal IServiceCollection Services { get; }

    /// <inheritdoc />
    public void Validate()
    {
        // Aggregate builder is valid even when empty — the generated bundle or
        // manual registrations determine what is required.
    }

    /// <summary>
    ///     Throws if the aggregate type has already been registered.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate type to check.</typeparam>
    internal void EnsureNotDuplicate<TAggregate>()
    {
        if (!RegisteredAggregates.Add(typeof(TAggregate)))
        {
            throw new MississippiBuilderException(
                MississippiDiagnosticCodes.DuplicateRegistration,
                $"Aggregate '{typeof(TAggregate).FullName}' was registered more than once.");
        }
    }
}