using System;
using System.Threading.Tasks;

using Orleans;


namespace Mississippi.Aqueduct.Runtime.Grains;

/// <summary>
///     Reads registration freshness from the existing server directory for owned-state cleanup.
/// </summary>
[Alias("Mississippi.Aqueduct.Runtime.Grains.ISignalRServerLivenessGrain")]
internal interface ISignalRServerLivenessGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Determines whether a registered server's last heartbeat is within the timeout.
    /// </summary>
    /// <param name="serverId">The server owning the connection.</param>
    /// <param name="timeout">The positive heartbeat timeout; the exact cutoff remains alive.</param>
    /// <returns>Whether the server is registered and its heartbeat is current.</returns>
    [Alias("IsServerAliveAsync")]
    Task<bool> IsServerAliveAsync(
        string serverId,
        TimeSpan timeout
    );
}