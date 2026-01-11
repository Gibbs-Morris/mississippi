namespace Mississippi.Reservoir.Abstractions.State;

/// <summary>
///     Marker interface for feature state slices in the Reservoir state tree.
/// </summary>
/// <remarks>
///     <para>
///         Feature states are registered with the store and identified by a unique key.
///         Each feature state represents a slice of the application state (UI, forms, session).
///     </para>
///     <para>
///         Implementations should be immutable records to ensure proper change detection.
///         The <see cref="FeatureKey" /> must be unique across all registered feature states.
///     </para>
/// </remarks>
/// <example>
///     <code>
///         public sealed record SidebarState : IFeatureState
///         {
///             public static string FeatureKey => "sidebar";
///             public bool IsOpen { get; init; }
///             public string ActivePanel { get; init; } = string.Empty;
///         }
///     </code>
/// </example>
public interface IFeatureState
{
    /// <summary>
    ///     Gets the unique key identifying this feature state in the store.
    /// </summary>
    static abstract string FeatureKey { get; }
}