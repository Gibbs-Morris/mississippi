using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Orleans.Hosting;


namespace Mississippi.Hosting.Runtime;

/// <summary>
///     Stores deferred runtime-builder configuration until Orleans attaches to the host.
/// </summary>
internal sealed class MississippiRuntimeBuilderState
{
    private IServiceCollection HostServices { get; }

    private List<Action<ISiloBuilder>> OrleansConfigurations { get; } = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="MississippiRuntimeBuilderState" /> class.
    /// </summary>
    /// <param name="hostServices">The host service collection that owns runtime composition.</param>
    internal MississippiRuntimeBuilderState(
        IServiceCollection hostServices
    )
    {
        ArgumentNullException.ThrowIfNull(hostServices);
        HostServices = hostServices;
    }

    /// <summary>
    ///     Gets the queued Orleans configuration callbacks for verification scenarios.
    /// </summary>
    internal IReadOnlyList<Action<ISiloBuilder>> QueuedOrleansConfigurations => OrleansConfigurations;

    /// <summary>
    ///     Applies all queued Orleans configuration callbacks to the provided silo builder.
    /// </summary>
    /// <param name="siloBuilder">The silo builder receiving the queued configuration.</param>
    internal void ApplyOrleansConfiguration(
        ISiloBuilder siloBuilder
    )
    {
        ArgumentNullException.ThrowIfNull(siloBuilder);
        MississippiRuntimeCompositionGuards.ThrowIfUnsupportedCompositionExists(HostServices);
        MississippiRuntimeCompositionGuards.ThrowIfUnsupportedCompositionExists(siloBuilder.Services);
        foreach (Action<ISiloBuilder> configure in OrleansConfigurations)
        {
            configure(siloBuilder);
        }

        MississippiRuntimeCompositionGuards.ThrowIfUnsupportedCompositionExists(HostServices);
        MississippiRuntimeCompositionGuards.ThrowIfUnsupportedCompositionExists(siloBuilder.Services);
    }

    /// <summary>
    ///     Queues an Orleans configuration callback for later application.
    /// </summary>
    /// <param name="configure">The callback to queue.</param>
    internal void QueueOrleansConfiguration(
        Action<ISiloBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        OrleansConfigurations.Add(configure);
    }
}