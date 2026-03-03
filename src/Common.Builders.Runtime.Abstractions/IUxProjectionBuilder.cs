namespace Mississippi.Common.Builders.Runtime.Abstractions;

/// <summary>
///     Typed UX projection composition contract.
/// </summary>
/// <typeparam name="TProjectionState">Projection state type.</typeparam>
public interface IUxProjectionBuilder<out TProjectionState>
{
    /// <summary>
    ///     Gets the projection state type marker.
    /// </summary>
    TProjectionState? ProjectionStateMarker { get; }
}