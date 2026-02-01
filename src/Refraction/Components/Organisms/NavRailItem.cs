// <copyright file="NavRailItem.cs" company="Microsoft">
// Licensed under the MIT License.
// </copyright>

using Microsoft.AspNetCore.Components;

namespace Refraction.Components.Organisms;

/// <summary>
/// Represents an item in the NavRail.
/// </summary>
public class NavRailItem
{
    /// <summary>
    /// Gets or sets the display label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the navigation href.
    /// </summary>
    public string Href { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional icon content.
    /// </summary>
    public RenderFragment? IconContent { get; set; }

    /// <summary>
    /// Gets or sets an icon placeholder string.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets an optional badge count.
    /// </summary>
    public int Badge { get; set; }
}
