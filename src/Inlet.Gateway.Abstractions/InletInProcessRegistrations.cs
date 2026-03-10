#pragma warning disable S1133 // Intentional staged deprecation pending issue #237.
using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;


namespace Mississippi.Inlet.Gateway.Abstractions;

/// <summary>
///     Extension methods for registering Inlet in-process server services.
/// </summary>
[Obsolete(
    "Legacy gateway composition entrypoint. Will be removed once GitHub issue #237 (Host/Sub-Builder Composition Model) is fully implemented. Migrate to GatewayBuilder via UseMississippi() once available (see issue #237, in progress). See: https://github.com/Gibbs-Morris/mississippi/issues/237",
    false)]
public static class InletInProcessRegistrations
{
    /// <summary>
    ///     Adds Inlet in-process server services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This registers the following services:
    ///         <list type="bullet">
    ///             <item><see cref="InProcessProjectionNotifier" /> as singleton</item>
    ///             <item><see cref="IServerProjectionNotifier" /> pointing to the same instance</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Use in combination with Inlet.Blazor.WebAssembly AddInletBlazor extension method
    ///         for Redux-style state management in Blazor Server applications.
    ///     </para>
    /// </remarks>
    [Obsolete(
        "Legacy gateway composition entrypoint. Will be removed once GitHub issue #237 (Host/Sub-Builder Composition Model) is fully implemented. Migrate to GatewayBuilder via UseMississippi() once available (see issue #237, in progress). See: https://github.com/Gibbs-Morris/mississippi/issues/237",
        false)]
    public static IServiceCollection AddInletInProcess(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<InProcessProjectionNotifier>();
        services.TryAddSingleton<IServerProjectionNotifier>(sp => sp.GetRequiredService<InProcessProjectionNotifier>());
        return services;
    }
}

#pragma warning restore S1133