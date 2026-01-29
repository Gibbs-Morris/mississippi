using System.Diagnostics.CodeAnalysis;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Actions;

/// <summary>
///     Action dispatched to navigate to a new URI, pushing a new entry onto the browser history stack.
/// </summary>
/// <remarks>
///     <para>
///         This action triggers navigation via Blazor's <c>NavigationManager.NavigateTo</c>.
///         The navigation effect handles the actual navigation call.
///     </para>
///     <para>
///         After navigation completes, a <see cref="LocationChangedAction" /> will be dispatched
///         automatically by the <see cref="Components.ReservoirNavigationProvider" /> component.
///     </para>
/// </remarks>
/// <param name="Uri">The URI to navigate to. Can be relative or absolute.</param>
/// <param name="ForceLoad">
///     If true, bypasses client-side routing and forces the browser to load the page from the server.
///     Default is false.
/// </param>
[SuppressMessage(
    "Design",
    "CA1054:URI parameters should not be strings",
    Justification = "Blazor NavigationManager uses string URIs - matching that API")]
[SuppressMessage(
    "Design",
    "CA1056:URI properties should not be strings",
    Justification = "Blazor NavigationManager uses string URIs - matching that API")]
public sealed record NavigateAction(string Uri, bool ForceLoad = false) : IAction;