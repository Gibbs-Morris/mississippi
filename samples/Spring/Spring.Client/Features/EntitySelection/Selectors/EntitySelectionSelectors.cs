using System;


namespace Spring.Client.Features.EntitySelection.Selectors;

/// <summary>
///     Selectors for deriving values from <see cref="EntitySelectionState" />.
/// </summary>
/// <remarks>
///     <para>
///         These selectors provide a consistent, reusable way to derive values
///         from entity selection state across components.
///     </para>
/// </remarks>
internal static class EntitySelectionSelectors
{
    /// <summary>
    ///     Selects the currently selected entity ID.
    /// </summary>
    /// <param name="state">The entity selection state.</param>
    /// <returns>The selected entity ID, or null if none selected.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="state" /> is null.
    /// </exception>
    public static string? GetEntityId(
        EntitySelectionState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.EntityId;
    }

    /// <summary>
    ///     Selects whether an entity is currently selected.
    /// </summary>
    /// <param name="state">The entity selection state.</param>
    /// <returns>True if an entity is selected; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="state" /> is null.
    /// </exception>
    public static bool HasEntitySelected(
        EntitySelectionState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return !string.IsNullOrEmpty(state.EntityId);
    }
}