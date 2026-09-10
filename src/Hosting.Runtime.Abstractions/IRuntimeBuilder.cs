using System;

using Mississippi.Hosting.Abstractions;

using Orleans.Hosting;


namespace Mississippi.Hosting.Runtime.Abstractions;

/// <summary>
///     Defines runtime-specific composition and Orleans integration for Mississippi extensions.
/// </summary>
/// <remarks>Public so runtime and storage contracts can extend composition without depending on its implementation.</remarks>
public interface IRuntimeBuilder : IMississippiBuilder
{
    /// <summary>
    ///     Applies queued Orleans configuration to the staged graph for the owning silo.
    /// </summary>
    /// <param name="siloBuilder">The silo receiving terminal Mississippi attachment.</param>
    /// <returns>This runtime builder for chaining.</returns>
    IRuntimeBuilder ApplyToSilo(
        ISiloBuilder siloBuilder
    );

    /// <summary>
    ///     Queues synchronous Orleans configuration for explicit or terminal application.
    /// </summary>
    /// <param name="configure">The configuration callback, invoked with staged services.</param>
    /// <returns>This runtime builder for chaining.</returns>
    IRuntimeBuilder ConfigureSilo(
        Action<ISiloBuilder> configure
    );
}