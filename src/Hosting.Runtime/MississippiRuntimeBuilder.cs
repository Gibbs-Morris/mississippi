using System;
using System.ComponentModel;

using Microsoft.Extensions.DependencyInjection;

using Orleans.Hosting;


namespace Mississippi.Hosting.Runtime;

/// <summary>
///     Provides the top-level runtime-role builder for Mississippi applications.
/// </summary>
public sealed class MississippiRuntimeBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MississippiRuntimeBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection used for runtime startup.</param>
    /// <param name="state">The runtime builder state owned by the host registration.</param>
    internal MississippiRuntimeBuilder(
        IServiceCollection services,
        MississippiRuntimeBuilderState state
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(state);
        Services = services;
        State = state;
    }

    /// <summary>
    ///     Gets the underlying service collection for advanced composition scenarios.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public IServiceCollection Services { get; }

    private MississippiRuntimeBuilderState State { get; }

    /// <summary>
    ///     Queues Orleans silo configuration within the Mississippi runtime builder flow.
    /// </summary>
    /// <param name="configure">The Orleans silo configuration callback.</param>
    /// <returns>The Mississippi runtime builder for chaining.</returns>
    public MississippiRuntimeBuilder Orleans(
        Action<ISiloBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        State.QueueOrleansConfiguration(configure);
        return this;
    }
}