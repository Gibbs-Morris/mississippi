using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Inlet.Client.Abstractions.State;

/// <summary>
///     Feature state for Inlet SignalR connection management.
/// </summary>
/// <remarks>
///     <para>
///         This state tracks the SignalR connection lifecycle and subscription management.
///         Effects scoped to this feature handle projection subscriptions via the InletHub.
///     </para>
///     <para>
///         The state itself may be minimal, as the primary purpose is to provide a feature
///         compartment for Inlet's SignalR action effects to be scoped to.
///     </para>
/// </remarks>
public sealed record InletConnectionState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "inlet-connection";
}