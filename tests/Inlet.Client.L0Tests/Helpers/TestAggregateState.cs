using Mississippi.Inlet.Client.Abstractions.State;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Test implementation of aggregate command state.
/// </summary>
internal sealed record TestAggregateState : AggregateCommandStateBase, IAggregateCommandState
{
    /// <inheritdoc />
    public static string FeatureKey => "test-aggregate";
}
