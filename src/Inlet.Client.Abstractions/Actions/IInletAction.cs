using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Client.Abstractions.Actions;

/// <summary>
///     Marker interface for server-synced inlet actions that target a specific entity.
/// </summary>
/// <remarks>
///     Extends <see cref="IAction" /> with an <see cref="EntityId" /> property for actions
///     that interact with projections or aggregates identified by an entity key.
/// </remarks>
public interface IInletAction : IAction
{
    /// <summary>
    ///     Gets the entity identifier this action applies to.
    /// </summary>
    string EntityId { get; }
}