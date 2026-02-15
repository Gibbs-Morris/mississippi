using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;


namespace LightSpeed.Client.Compoments;

/// <summary>
///     Standardized wrapper for kitchen sink demo sections.
///     Provides consistent layout: header, preview + properties, and collapsible events drawer.
/// </summary>
public sealed partial class KitchenSinkDemoSection : ComponentBase
{
    /// <summary>
    ///     Gets or sets the live preview content (the actual component demo).
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Gets or sets an optional tag name shown as a code badge (e.g. "MisButton").
    /// </summary>
    [Parameter]
    public string? ComponentTag { get; set; }

    /// <summary>
    ///     Gets or sets a short description of the component shown beneath the title.
    /// </summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>
    ///     Gets or sets the number of events for the badge counter.
    /// </summary>
    [Parameter]
    public int EventCount { get; set; }

    /// <summary>
    ///     Gets or sets the events list content.
    /// </summary>
    [Parameter]
    public RenderFragment? Events { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the events drawer is open.
    /// </summary>
    [Parameter]
    public bool IsEventsOpen { get; set; }

    /// <summary>
    ///     Gets or sets the callback invoked when the clear-events button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClearEvents { get; set; }

    /// <summary>
    ///     Gets or sets the callback invoked when the events drawer toggle is clicked.
    /// </summary>
    [Parameter]
    public EventCallback OnToggleEvents { get; set; }

    /// <summary>
    ///     Gets or sets the properties/controls panel content.
    /// </summary>
    [Parameter]
    public RenderFragment? Properties { get; set; }

    /// <summary>
    ///     Gets or sets the component display name shown in the section header.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;
}