// <copyright file="BreadcrumbItem.cs" company="Microsoft">
// Licensed under the MIT License.
// </copyright>

using Microsoft.AspNetCore.Components;

namespace Refraction.Components.Molecules;

/// <summary>
/// Represents an item in the Breadcrumb navigation.
/// </summary>
public class BreadcrumbItem
{
    /// <summary>
    /// Gets or sets the display label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the navigation href.
    /// </summary>
    public string? Href { get; set; }

    /// <summary>
    /// Gets or sets optional icon content.
    /// </summary>
    public RenderFragment? IconContent { get; set; }
}
