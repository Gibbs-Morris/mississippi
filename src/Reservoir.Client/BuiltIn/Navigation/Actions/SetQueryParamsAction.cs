using System.Collections.Generic;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Actions;

/// <summary>
///     Action dispatched to update query string parameters on the current URI.
/// </summary>
/// <remarks>
///     <para>
///         This action uses Blazor's <c>NavigationManager.GetUriWithQueryParameters</c>
///         to build the new URI and then navigates to it. Parameters can be added,
///         updated, or removed (by setting value to null).
///     </para>
///     <para>
///         The navigation preserves the current path and fragment, only modifying
///         the query string portion of the URI.
///     </para>
/// </remarks>
/// <param name="Parameters">
///     Dictionary of query parameters to set. Use null values to remove parameters.
/// </param>
/// <param name="ReplaceHistory">
///     If true, replaces the current history entry instead of pushing a new one.
///     Default is true to avoid polluting history with filter/sort changes.
/// </param>
public sealed record SetQueryParamsAction(
    IReadOnlyDictionary<string, object?> Parameters,
    bool ReplaceHistory = true
) : IAction;