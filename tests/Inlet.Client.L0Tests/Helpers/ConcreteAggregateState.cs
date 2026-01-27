using Mississippi.Inlet.Client.Abstractions.State;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Concrete implementation of AggregateCommandStateBase for testing.
/// </summary>
internal sealed record ConcreteAggregateState : AggregateCommandStateBase, IAggregateCommandState
{
    /// <inheritdoc />
    public static string FeatureKey => "concrete-aggregate";
}
