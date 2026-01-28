using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Client.SignalRConnection;

/// <summary>
///     Action dispatched when the SignalR connection is attempting to reconnect.
/// </summary>
/// <remarks>
///     This action is dispatched when the automatic reconnect logic begins a reconnection attempt
///     after the connection was unexpectedly lost.
/// </remarks>
public sealed record SignalRReconnectingAction : IAction
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SignalRReconnectingAction" /> class.
    /// </summary>
    /// <param name="error">The error message that caused the reconnection, if available.</param>
    /// <param name="attemptNumber">The current reconnection attempt number.</param>
    public SignalRReconnectingAction(
        string? error,
        int attemptNumber
    )
    {
        Error = error;
        AttemptNumber = attemptNumber;
    }

    /// <summary>
    ///     Gets the current reconnection attempt number.
    /// </summary>
    public int AttemptNumber { get; }

    /// <summary>
    ///     Gets the error message that caused the reconnection, if available.
    /// </summary>
    public string? Error { get; }
}