using System;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Blazor.WebAssembly.SignalRConnection;

/// <summary>
///     Action dispatched when the SignalR connection is closed.
/// </summary>
/// <remarks>
///     <para>
///         This action is dispatched when the connection closes, either due to an error,
///         intentional disconnection, or after all automatic reconnect attempts have failed.
///     </para>
///     <para>
///         If <see cref="Error" /> is null, the disconnection was intentional.
///     </para>
/// </remarks>
public sealed record SignalRDisconnectedAction : IAction
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SignalRDisconnectedAction" /> class.
    /// </summary>
    /// <param name="error">The error message that caused the disconnection, if any.</param>
    /// <param name="timestamp">The timestamp when the disconnection occurred.</param>
    public SignalRDisconnectedAction(
        string? error,
        DateTimeOffset timestamp
    )
    {
        Error = error;
        Timestamp = timestamp;
    }

    /// <summary>
    ///     Gets the error message that caused the disconnection, if any.
    /// </summary>
    /// <remarks>
    ///     If null, the disconnection was intentional (e.g., client called <c>StopAsync</c>).
    /// </remarks>
    public string? Error { get; }

    /// <summary>
    ///     Gets the timestamp when the disconnection occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}