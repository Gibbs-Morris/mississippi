namespace Mississippi.Common.Builders.Runtime.Abstractions;

/// <summary>
///     Typed feature-state composition contract.
/// </summary>
/// <typeparam name="TFeatureState">Feature state type.</typeparam>
public interface IFeatureStateBuilder<out TFeatureState>
{
    /// <summary>
    ///     Gets the feature state type marker.
    /// </summary>
    TFeatureState? FeatureStateMarker { get; }
}