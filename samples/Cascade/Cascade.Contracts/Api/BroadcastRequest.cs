namespace Cascade.Contracts.Api;

/// <summary>
///     Request to broadcast a message via Orleans streams.
/// </summary>
public sealed record BroadcastRequest
{
    /// <summary>
    ///     Gets the message to broadcast.
    /// </summary>
    public required string Message { get; init; }
}