using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Actions;

/// <summary>
///     Action dispatched when the browser location has changed.
/// </summary>
/// <remarks>
///     <para>
///         This action is dispatched automatically by the
///         <see cref="Components.ReservoirNavigationProvider" /> component when it detects
///         a location change via <c>NavigationManager.LocationChanged</c>.
///     </para>
///     <para>
///         Components and effects can listen for this action to react to navigation events.
///         The <see cref="State.NavigationState" /> reducer updates the navigation state
///         when this action is dispatched.
///     </para>
/// </remarks>
/// <param name="Location">The new absolute URI after navigation.</param>
/// <param name="IsNavigationIntercepted">
///     True if Blazor intercepted the navigation from a link click.
///     False if navigation was triggered programmatically via <c>NavigateTo</c>.
/// </param>
public sealed record LocationChangedAction(string Location, bool IsNavigationIntercepted) : IAction;