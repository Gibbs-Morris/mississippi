using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Diagnostics;
using Mississippi.DomainModeling.Abstractions.Builders;


namespace Mississippi.DomainModeling.Runtime.Builders;

/// <summary>
///     Default implementation of <see cref="ISagaBuilder" />.
/// </summary>
public sealed class SagaBuilder : ISagaBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection for registering saga services.</param>
    public SagaBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    /// <summary>
    ///     Gets the set of saga types that have been registered.
    /// </summary>
    internal HashSet<Type> RegisteredSagas { get; } = [];

    /// <summary>
    ///     Gets the service collection for saga service registration.
    /// </summary>
    internal IServiceCollection Services { get; }

    /// <inheritdoc />
    public void Validate()
    {
        // Saga builder is valid even when empty — the generated bundle or
        // manual registrations determine what is required.
    }

    /// <summary>
    ///     Throws if the saga type has already been registered.
    /// </summary>
    /// <typeparam name="TSaga">The saga type to check.</typeparam>
    internal void EnsureNotDuplicate<TSaga>()
    {
        if (!RegisteredSagas.Add(typeof(TSaga)))
        {
            throw new MississippiBuilderException(
                MississippiDiagnosticCodes.DuplicateRegistration,
                $"Saga '{typeof(TSaga).FullName}' was registered more than once.");
        }
    }
}