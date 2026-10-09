using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;


namespace Mississippi.Aqueduct.Gateway;

/// <summary>
///     Aggregates hub-local connection counts for the shared gateway heartbeat.
/// </summary>
internal sealed class HubConnectionRegistries
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="HubConnectionRegistries" /> class.
    /// </summary>
    /// <param name="registries">The hub-local registries configured in the service provider.</param>
    public HubConnectionRegistries(
        IEnumerable<IConnectionRegistry> registries
    )
    {
        ArgumentNullException.ThrowIfNull(registries);
        Registries = [.. registries];
    }

    /// <summary>
    ///     Gets the number of local connections across the configured hubs.
    /// </summary>
    public int Count => Registries.Sum(registry => registry.Count);

    private ImmutableArray<IConnectionRegistry> Registries { get; }
}