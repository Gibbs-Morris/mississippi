using System.Threading.Tasks;

using Orleans;


namespace Cascade.Grains.Abstractions;

/// <summary>
///     Grain interface for broadcasting messages via Orleans streams.
/// </summary>
/// <remarks>
///     This grain demonstrates Orleans streaming by publishing messages
///     to a stream that can be consumed by any subscriber (grains or clients).
/// </remarks>
[Alias("Cascade.Web.Contracts.IBroadcasterGrain")]
public interface IBroadcasterGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Broadcasts a message to all stream subscribers.
    /// </summary>
    /// <param name="message">The message to broadcast.</param>
    /// <returns>A task that completes when the message has been published to the stream.</returns>
    [Alias("BroadcastAsync")]
    Task BroadcastAsync(
        string message
    );
}
