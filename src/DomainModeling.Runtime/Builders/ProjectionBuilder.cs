using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Diagnostics;
using Mississippi.DomainModeling.Abstractions.Builders;


namespace Mississippi.DomainModeling.Runtime.Builders;

/// <summary>
///     Default implementation of <see cref="IProjectionBuilder" />.
/// </summary>
public sealed class ProjectionBuilder : IProjectionBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection for registering projection services.</param>
    public ProjectionBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    /// <summary>
    ///     Gets the set of projection types that have been registered.
    /// </summary>
    internal HashSet<Type> RegisteredProjections { get; } = [];

    /// <summary>
    ///     Gets the service collection for projection service registration.
    /// </summary>
    internal IServiceCollection Services { get; }

    /// <inheritdoc />
    public void Validate()
    {
        // Projection builder is valid even when empty — the generated bundle or
        // manual registrations determine what is required.
    }

    /// <summary>
    ///     Throws if the projection type has already been registered.
    /// </summary>
    /// <typeparam name="TProjection">The projection type to check.</typeparam>
    internal void EnsureNotDuplicate<TProjection>()
    {
        if (!RegisteredProjections.Add(typeof(TProjection)))
        {
            throw new MississippiBuilderException(
                MississippiDiagnosticCodes.DuplicateRegistration,
                $"Projection '{typeof(TProjection).FullName}' was registered more than once.");
        }
    }
}