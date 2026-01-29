using System.Diagnostics.CodeAnalysis;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Actions;

/// <summary>
///     Action dispatched to open an external URL in a new browser tab/window.
/// </summary>
/// <remarks>
///     <para>
///         This action uses JavaScript interop to call <c>window.open()</c>.
///         Unlike <see cref="NavigateAction" />, this opens the URL in a new tab
///         rather than navigating the current SPA.
///     </para>
///     <para>
///         <strong>Common use cases:</strong>
///     </para>
///     <list type="bullet">
///         <item>Opening external documentation or API references</item>
///         <item>Launching third-party services (payment, OAuth, etc.)</item>
///         <item>Opening downloadable resources</item>
///     </list>
///     <para>
///         <strong>Usage:</strong>
///     </para>
///     <code>
///         Dispatch(new OpenExternalLinkAction("https://docs.example.com"));
///     </code>
/// </remarks>
/// <param name="Url">The URL to open. Should be a full URL including protocol (https://).</param>
[SuppressMessage(
    "Design",
    "CA1054:URI parameters should not be strings",
    Justification = "JS window.open() uses string URLs - matching that API")]
[SuppressMessage(
    "Design",
    "CA1056:URI properties should not be strings",
    Justification = "JS window.open() uses string URLs - matching that API")]
public sealed record OpenExternalLinkAction(string Url) : IAction;
