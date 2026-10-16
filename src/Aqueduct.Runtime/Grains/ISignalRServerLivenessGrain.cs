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
    ///     Determines whether a server has a current heartbeat or is within the bounded directory-recovery window.
    /// </summary>
    /// <param name="serverId">The server owning the connection.</param>
    /// <param name="timeout">The positive heartbeat timeout; the exact cutoff remains alive.</param>
    /// <returns>Whether cleanup should retain its route; recent explicit unregistrations return false.</returns>
    [Alias("IsServerAliveAsync")]
    Task<bool> IsServerAliveAsync(
        string serverId,
        TimeSpan timeout
    );
}