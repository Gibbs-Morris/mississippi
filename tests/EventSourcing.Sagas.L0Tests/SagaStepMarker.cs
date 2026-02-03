namespace Mississippi.EventSourcing.Sagas.L0Tests;

/// <summary>
///     Marker step type for saga tests.
/// </summary>
internal sealed class SagaStepMarker
{
    /// <summary>
    ///     Returns a display label for the marker type.
    /// </summary>
    /// <returns>The marker name.</returns>
    public override string ToString() => nameof(SagaStepMarker);
}