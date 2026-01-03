namespace Mississippi.Ripples.Abstractions;

/// <summary>
///     Marker interface for ripple actions.
/// </summary>
public interface IRippleAction
{
    /// <summary>
    ///     Gets the entity identifier this action applies to.
    /// </summary>
    string EntityId { get; }
}