using System;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Blazor.WebAssembly.SignalRConnection;

/// <summary>
///     Action dispatched when a message is received from the SignalR hub.
/// </summary>
/// <remarks>
///     <para>
///         This action enables UI components to react to server activity, such as
///         displaying a heartbeat animation or activity indicator when projections
///         are updated.
///     </para>
///     <para>
///         This action is dispatched for any projection update message, not for
///         individual projection types.
///     </para>
/// </remarks>
public sealed record SignalRMessageReceivedAction : IAction
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SignalRMessageReceivedAction" /> class.
    /// </summary>
    /// <param name="timestamp">The timestamp when the message was received.</param>
    public SignalRMessageReceivedAction(
        DateTimeOffset timestamp
    )
    {
        Timestamp = timestamp;
    }

    /// <summary>
    ///     Gets the timestamp when the message was received.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}
