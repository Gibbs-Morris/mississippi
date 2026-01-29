using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Actions;

/// <summary>
///     Action dispatched to scroll to a named anchor element on the current page.
/// </summary>
/// <remarks>
///     <para>
///         This action navigates to the current path with a fragment identifier,
///         which causes the browser to scroll to the element with the matching ID.
///     </para>
///     <para>
///         This is a pure .NET implementation using Blazor's built-in fragment navigation
///         and does not require JavaScript interop.
///     </para>
/// </remarks>
/// <param name="AnchorId">
///     The ID of the element to scroll to (without the # prefix).
/// </param>
/// <param name="ReplaceHistory">
///     If true, replaces the current history entry instead of pushing a new one.
///     Default is false to allow back navigation.
/// </param>
public sealed record ScrollToAnchorAction(string AnchorId, bool ReplaceHistory = false) : IAction;