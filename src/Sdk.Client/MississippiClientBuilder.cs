using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Inlet.Client;
using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Core;


namespace Mississippi.Sdk.Client;

/// <summary>
///     Root builder for Mississippi client-side composition, attached from
///     <see cref="WebAssemblyHostBuilderExtensions.UseMississippi" />.
/// </summary>
/// <remarks>
///     <para>
///         This builder owns common full-framework client composition: generated domain
///         client registration, Inlet client composition, Blazor/client integrations,
///         and developer-facing client setup.
///     </para>
///     <para>
///         For advanced or custom app-specific client-side feature state, use the
///         <see cref="Reservoir" /> sub-builder hook. Domain projection-path registrations
///         are handled automatically by generated <c>Add&lt;Domain&gt;Client()</c> extensions.
///     </para>
/// </remarks>
public sealed class MississippiClientBuilder
{
    private readonly IServiceCollection services;

    private bool inletClientAdded;

    private ReservoirBuilder? reservoirBuilder;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MississippiClientBuilder" /> class.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    internal MississippiClientBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        this.services = services;
    }

    /// <summary>
    ///     Connects Inlet to the gateway's SignalR hub for live projection updates.
    /// </summary>
    /// <param name="configure">Action to configure the SignalR builder (hub path, projection fetcher, etc.).</param>
    public void AddInletBlazorSignalR(
        Action<InletBlazorSignalRBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        InletBlazorSignalRBuilder signalRBuilder = new(services);
        configure(signalRBuilder);
        signalRBuilder.Build();
    }

    /// <summary>
    ///     Enables the Inlet real-time projection subscription client.
    ///     Idempotent — safe to call multiple times.
    /// </summary>
    public void AddInletClient()
    {
        if (inletClientAdded)
        {
            return;
        }

        inletClientAdded = true;
        services.TryAddSingleton<IProjectionRegistry, ProjectionRegistry>();
        services.TryAddScoped<IInletStore>(sp => new CompositeInletStore(sp.GetRequiredService<IStore>()));
        services.TryAddScoped<IProjectionUpdateNotifier>(sp => new ProjectionNotifier(sp.GetRequiredService<IStore>()));
    }

    /// <summary>
    ///     Configures advanced or custom client-side feature state through the Reservoir sub-builder.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use this hook only for custom app-specific client-side feature state that
    ///         the source generator cannot infer. Domain projection-path registrations
    ///         are handled automatically by generated <c>Add&lt;Domain&gt;Client()</c>.
    ///     </para>
    ///     <para>
    ///         Generated and manual Reservoir registrations are additive and merge without conflict.
    ///     </para>
    /// </remarks>
    /// <param name="configure">The Reservoir configuration callback.</param>
    public void Reservoir(
        Action<IReservoirBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        reservoirBuilder ??= new(services);
        configure(reservoirBuilder);
    }

    /// <summary>
    ///     Gets the service collection for direct registration by generated extensions.
    /// </summary>
    /// <returns>The underlying service collection.</returns>
    internal IServiceCollection GetServices() => services;

    /// <summary>
    ///     Validates all sub-builder state for correctness.
    /// </summary>
    internal void Validate()
    {
        reservoirBuilder?.Validate();
    }
}