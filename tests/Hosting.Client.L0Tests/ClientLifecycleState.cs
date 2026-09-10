using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Hosting.Client.L0Tests;

/// <summary>
///     Provides a feature state for client composition lifecycle tests.
/// </summary>
internal sealed record ClientLifecycleState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "client-lifecycle";
}