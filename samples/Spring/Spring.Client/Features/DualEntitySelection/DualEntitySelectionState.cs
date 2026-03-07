using Mississippi.Reservoir.Abstractions.State;


namespace Spring.Client.Features.DualEntitySelection;

/// <summary>
///     Feature state for tracking two active entity identifiers in the UI.
/// </summary>
/// <remarks>
///     <para>
///         This state is used by the Operations page to support simultaneous
///         A/B account panels and transfers between them.
///     </para>
///     <para>
///         It is a UI selection concern, not aggregate command state.
///     </para>
/// </remarks>
internal sealed record DualEntitySelectionState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "dualEntitySelection";

    /// <summary>
    ///     Gets the account A entity identifier.
    /// </summary>
    public string? AccountAId { get; init; }

    /// <summary>
    ///     Gets the account B entity identifier.
    /// </summary>
    public string? AccountBId { get; init; }
}