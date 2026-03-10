#pragma warning disable S1133 // Intentional staged deprecation pending issue #237.
using System;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Refraction.Client.Infrastructure;

/// <summary>
///     Extension methods for registering Refraction services.
/// </summary>
[Obsolete(
    "Legacy client composition entrypoint. Will be removed once GitHub issue #237 (Host/Sub-Builder Composition Model) is fully implemented. Migrate to ClientBuilder via UseMississippi() once available (see issue #237, in progress). See: https://github.com/Gibbs-Morris/mississippi/issues/237",
    false)]
public static class RefractionServiceCollectionExtensions
{
    /// <summary>
    ///     Adds Refraction component library services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         Refraction components are pure presentation and do not require services for basic usage.
    ///         This method registers optional services for advanced scenarios:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Theme management</description>
    ///         </item>
    ///         <item>
    ///             <description>Focus tracking</description>
    ///         </item>
    ///         <item>
    ///             <description>Motion preferences</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    [Obsolete(
        "Legacy client composition entrypoint. Will be removed once GitHub issue #237 (Host/Sub-Builder Composition Model) is fully implemented. Migrate to ClientBuilder via UseMississippi() once available (see issue #237, in progress). See: https://github.com/Gibbs-Morris/mississippi/issues/237",
        false)]
    public static IServiceCollection AddRefraction(
        this IServiceCollection services
    ) =>
        services;
}

#pragma warning restore S1133