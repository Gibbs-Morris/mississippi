using System.Diagnostics.CodeAnalysis;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Actions;

/// <summary>
///     Action dispatched to replace the current URI in browser history rather than pushing a new entry.
/// </summary>
/// <remarks>
///     <para>
///         This action triggers navigation via Blazor's <c>NavigationManager.NavigateTo</c>
///         with <c>ReplaceHistoryEntry = true</c>. Use this when you want to update the URL
///         without adding to the browser's back button history.
///     </para>
///     <para>
///         Common use cases include:
///     </para>
///     <list type="bullet">
///         <item>Redirecting after form submission without allowing "back" to the form</item>
///         <item>Updating query parameters for filtering/sorting without polluting history</item>
///         <item>Correcting a URL after initial navigation</item>
///     </list>
/// </remarks>
/// <param name="Uri">
///     The URI to navigate to. Can be relative or an absolute URI that matches the app's origin.
///     External URLs are not supported by this action.
/// </param>
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
public sealed record ReplaceRouteAction(string Uri, bool ForceLoad = false) : IAction;