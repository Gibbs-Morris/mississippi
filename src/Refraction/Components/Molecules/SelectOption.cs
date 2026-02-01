// <copyright file="SelectOption.cs" company="Microsoft">
// Licensed under the MIT License.
// </copyright>

namespace Refraction.Components.Molecules;

/// <summary>
/// Represents an option in the SelectField.
/// </summary>
/// <typeparam name="TValue">The type of the option value.</typeparam>
public class SelectOption<TValue>
{
    /// <summary>
    /// Gets or sets the option value.
    /// </summary>
    public TValue Value { get; set; } = default!;

    /// <summary>
    /// Gets or sets the display label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this option is disabled.
    /// </summary>
    public bool IsDisabled { get; set; }
}
