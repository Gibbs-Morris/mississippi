using System;

using Microsoft.Extensions.Logging;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Abstractions.Keys;

using Orleans;


namespace Mississippi.Aqueduct.Grains;

/// <summary>
///     Factory for resolving Aqueduct SignalR grains by key.
/// </summary>
/// <remarks>
///     <para>
///         This factory encapsulates key formatting and provides a type-safe way
///         to obtain SignalR grain references without knowing the internal key structure.
///     </para>
/// </remarks>
internal sealed class AqueductGrainFactory : IAqueductGrainFactory
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AqueductGrainFactory" /> class.
    /// </summary>
    /// <param name="grainFactory">The Orleans grain factory for creating grain instances.</param>
    /// <param name="logger">Logger instance for logging grain factory operations.</param>
    public AqueductGrainFactory(
        IGrainFactory grainFactory,
        ILogger<AqueductGrainFactory> logger
    )
    {
        GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IGrainFactory GrainFactory { get; }

    private ILogger<AqueductGrainFactory> Logger { get; }

    /// <inheritdoc />
    public ISignalRClientGrain GetClientGrain(
        SignalRClientKey clientKey
    )
    {
        Logger.ResolvingClientGrain(nameof(ISignalRClientGrain), clientKey.HubName, clientKey.ConnectionId);
        return GrainFactory.GetGrain<ISignalRClientGrain>(clientKey);
    }

    /// <inheritdoc />
    public ISignalRClientGrain GetClientGrain(
        string hubName,
        string connectionId
    )
    {
        SignalRClientKey clientKey = new(hubName, connectionId);
        return GetClientGrain(clientKey);
    }

    /// <inheritdoc />
    public ISignalRGroupGrain GetGroupGrain(
        SignalRGroupKey groupKey
    )
    {
        Logger.ResolvingGroupGrain(nameof(ISignalRGroupGrain), groupKey.HubName, groupKey.GroupName);
        return GrainFactory.GetGrain<ISignalRGroupGrain>(groupKey);
    }

    /// <inheritdoc />
    public ISignalRGroupGrain GetGroupGrain(
        string hubName,
        string groupName
    )
    {
        SignalRGroupKey groupKey = new(hubName, groupName);
        return GetGroupGrain(groupKey);
    }

    /// <inheritdoc />
    public ISignalRServerDirectoryGrain GetServerDirectoryGrain(
        SignalRServerDirectoryKey directoryKey
    )
    {
        Logger.ResolvingServerDirectoryGrain(nameof(ISignalRServerDirectoryGrain), directoryKey.Value);
        return GrainFactory.GetGrain<ISignalRServerDirectoryGrain>(directoryKey);
    }

    /// <inheritdoc />
    public ISignalRServerDirectoryGrain GetServerDirectoryGrain() =>
        GetServerDirectoryGrain(SignalRServerDirectoryKey.Default);
}