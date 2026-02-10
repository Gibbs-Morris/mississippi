using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Common.Abstractions.Builders;


namespace Mississippi.Inlet.Server.Abstractions;

/// <summary>
///     Extension methods for registering Inlet in-process server services.
/// </summary>
public static class InletInProcessRegistrations
{
    /// <summary>
    ///     Adds Inlet in-process server services to the server builder.
    /// </summary>
    /// <param name="builder">The Mississippi server builder.</param>
    /// <returns>The builder for chaining.</returns>
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
    public static IMississippiServerBuilder AddInletInProcess(
        this IMississippiServerBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services =>
        {
            services.TryAddSingleton<InProcessProjectionNotifier>();
            services.TryAddSingleton<IServerProjectionNotifier>(sp =>
                sp.GetRequiredService<InProcessProjectionNotifier>());
        });
        return builder;
    }
}