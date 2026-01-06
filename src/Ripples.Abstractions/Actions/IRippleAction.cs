namespace Mississippi.Ripples.Abstractions.Actions;

/// <summary>
///     Marker interface for server-synced ripple actions that target a specific entity.
/// </summary>
/// <remarks>
///     Extends <see cref="IAction" /> with an <see cref="EntityId" /> property for actions
///     that interact with projections or aggregates identified by an entity key.
/// </remarks>
public interface IRippleAction : IAction
{
    /// <summary>
    ///     Gets the entity identifier this action applies to.
    /// </summary>
    string EntityId { get; }
}