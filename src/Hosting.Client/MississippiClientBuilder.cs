using System;
using System.ComponentModel;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Client;


namespace Mississippi.Hosting.Client;

/// <summary>
///     Provides the top-level client-role builder for Mississippi applications.
/// </summary>
public sealed class MississippiClientBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MississippiClientBuilder" /> class.
    /// </summary>
    /// <param name="builder">The host builder used for client startup.</param>
    public MississippiClientBuilder(
        WebAssemblyHostBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        Builder = builder;
        Services = builder.Services;
    }

    /// <summary>
    ///     Gets the underlying service collection for advanced composition scenarios.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public IServiceCollection Services { get; }

    private WebAssemblyHostBuilder Builder { get; }

    private IReservoirBuilder? ReservoirBuilder { get; set; }

    /// <summary>
    ///     Composes Reservoir registrations within the Mississippi client builder flow.
    /// </summary>
    /// <param name="configure">The Reservoir composition callback.</param>
    /// <returns>The Mississippi client builder for chaining.</returns>
    public MississippiClientBuilder Reservoir(
        Action<IReservoirBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(GetOrCreateReservoirBuilder());
        return this;
    }

    private IReservoirBuilder GetOrCreateReservoirBuilder()
    {
        ReservoirBuilder ??= Builder.AddReservoir();
        return ReservoirBuilder;
    }
}