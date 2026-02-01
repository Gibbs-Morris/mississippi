// <copyright file="TabBarTab.cs" company="Microsoft">
// Licensed under the MIT License.
// </copyright>

using Microsoft.AspNetCore.Components;

namespace Refraction.Components.Organisms;

/// <summary>
/// Represents a tab in the TabBar.
/// </summary>
public class TabBarTab
{
    /// <summary>
    /// Gets or sets the unique key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional icon content.
    /// </summary>
    public RenderFragment? IconContent { get; set; }

    /// <summary>
    /// Gets or sets an optional badge count.
    /// </summary>
    public int Badge { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this tab can be closed.
    /// </summary>
    public bool IsCloseable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this tab is disabled.
    /// </summary>
    public bool IsDisabled { get; set; }
}
