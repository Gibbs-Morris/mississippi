using System.Diagnostics.CodeAnalysis;

using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Blazor.BuiltIn.Navigation.State;

/// <summary>
///     Feature state for tracking browser navigation in a Reservoir store.
/// </summary>
/// <remarks>
///     <para>
///         This state is updated automatically when <see cref="Actions.LocationChangedAction" />
///         is dispatched. It tracks the current URI, previous URI for back-navigation context,
///         and whether the last navigation was intercepted from a link click.
///     </para>
///     <para>
///         Components can use this state to:
///     </para>
///     <list type="bullet">
///         <item>Display the current route in breadcrumbs or navigation UI</item>
///         <item>Conditionally render based on the current path</item>
///         <item>Track navigation history for analytics or debugging</item>
///     </list>
/// </remarks>
[SuppressMessage(
    "Design",
    "CA1056:URI properties should not be strings",
    Justification = "Blazor NavigationManager uses string URIs - matching that API")]
public sealed record NavigationState : IFeatureState
{
    /// <summary>
    ///     The feature key used to identify this state in the store.
    /// </summary>
    public const string Key = "reservoir:navigation";

    /// <inheritdoc />
    public static string FeatureKey => Key;

    /// <summary>
    ///     Gets the current absolute URI.
    /// </summary>
    public string? CurrentUri { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the last navigation was intercepted from a link click.
    /// </summary>
    /// <remarks>
    ///     True if Blazor intercepted navigation from an anchor tag or NavLink.
    ///     False if navigation was triggered programmatically via NavigateTo.
    /// </remarks>
    public bool IsNavigationIntercepted { get; init; }

    /// <summary>
    ///     Gets the number of navigations that have occurred since the store was initialized.
    /// </summary>
    public int NavigationCount { get; init; }

    /// <summary>
    ///     Gets the previous absolute URI before the last navigation.
    /// </summary>
    /// <remarks>
    ///     This is null on the first navigation. Useful for "back" UI patterns
    ///     or detecting navigation direction.
    /// </remarks>
    public string? PreviousUri { get; init; }
}