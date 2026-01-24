using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Blazor.WebAssembly.SignalRConnection;

/// <summary>
///     Action dispatched to request the SignalR connection be established.
/// </summary>
/// <remarks>
///     <para>
///         Dispatching this action triggers the <see cref="ActionEffects.InletSignalRActionEffect" />
///         to call <c>EnsureConnectedAsync</c> on the hub connection provider. This allows
///         components to eagerly establish the SignalR connection before subscribing to
///         any projections.
///     </para>
/// </remarks>
public sealed record RequestSignalRConnectionAction : IAction;