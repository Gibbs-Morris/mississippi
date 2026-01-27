using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Client.SignalRConnection;

/// <summary>
///     Action dispatched when the SignalR connection is starting.
/// </summary>
/// <remarks>
///     This action is dispatched before calling <c>StartAsync</c> on the hub connection.
/// </remarks>
public sealed record SignalRConnectingAction : IAction;