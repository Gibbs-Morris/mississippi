#pragma warning disable S1133 // Intentional staged deprecation pending issue #237.
using System;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Refraction.Client.StateManagement.Infrastructure;

/// <summary>
///     Extension methods for registering Refraction.Client.StateManagement services.
/// </summary>
[Obsolete(
    "Legacy client composition entrypoint. Will be removed once GitHub issue #237 (Host/Sub-Builder Composition Model) is fully implemented. Migrate to ClientBuilder via UseMississippi() once available (see issue #237, in progress). See: https://github.com/Gibbs-Morris/mississippi/issues/237",
    false)]
public static class RefractionPagesServiceCollectionExtensions
{
    /// <summary>
    ///     Adds Refraction.Client.StateManagement services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     This method registers scene-level services. Call this in addition to
    ///     <c>AddRefraction()</c> and <c>AddReservoir()</c> in your application startup.
    /// </remarks>
    [Obsolete(
        "Legacy client composition entrypoint. Will be removed once GitHub issue #237 (Host/Sub-Builder Composition Model) is fully implemented. Migrate to ClientBuilder via UseMississippi() once available (see issue #237, in progress). See: https://github.com/Gibbs-Morris/mississippi/issues/237",
        false)]
    public static IServiceCollection AddRefractionPages(
        this IServiceCollection services
    ) =>
        services;
}

#pragma warning restore S1133