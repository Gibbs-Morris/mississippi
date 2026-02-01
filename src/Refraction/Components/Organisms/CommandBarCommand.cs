// <copyright file="CommandBarCommand.cs" company="Microsoft">
// Licensed under the MIT License.
// </copyright>

using System;

using Microsoft.AspNetCore.Components;

namespace Refraction.Components.Organisms;

/// <summary>
/// Represents a command in the CommandBar.
/// </summary>
public class CommandBarCommand
{
    /// <summary>
    /// Gets or sets the command key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display label.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the tooltip text.
    /// </summary>
    public string? Tooltip { get; set; }

    /// <summary>
    /// Gets or sets optional icon content.
    /// </summary>
    public RenderFragment? IconContent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a primary action.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this command is active/toggled.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this command is disabled.
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// Gets or sets the click action.
    /// </summary>
    public Action? OnClick { get; set; }
}
