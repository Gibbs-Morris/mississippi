namespace Mississippi.EventSourcing.Sagas.L0Tests;

/// <summary>
///     Invalid step type without the saga step interface.
/// </summary>
internal sealed class SagaInvalidStep
{
    /// <summary>
    ///     Returns a label for the invalid step.
    /// </summary>
    /// <returns>The invalid step name.</returns>
    public override string ToString() => nameof(SagaInvalidStep);
}