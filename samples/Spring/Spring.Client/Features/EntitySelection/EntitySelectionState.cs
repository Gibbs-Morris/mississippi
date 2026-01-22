using Mississippi.Reservoir.Abstractions.State;


namespace Spring.Client.Features.EntitySelection;

/// <summary>
///     Feature state for tracking which entity is currently selected in the UI.
/// </summary>
/// <remarks>
///     <para>
///         This is application-specific state for navigation and entity selection.
///         It is separate from aggregate command state because entity selection is a UI concern,
///         not part of the command execution lifecycle.
///     </para>
///     <para>
///         Commands get the EntityId from this state when dispatched, rather than having
///         the EntityId embedded in the aggregate state itself.
///     </para>
/// </remarks>
internal sealed record EntitySelectionState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "entitySelection";

    /// <summary>
    ///     Gets the currently selected entity ID.
    /// </summary>
    /// <remarks>
    ///     When an entity is selected (e.g., via URL navigation or explicit selection),
    ///     this property is updated. Commands read this value when being dispatched.
    /// </remarks>
    public string? EntityId { get; init; }
}