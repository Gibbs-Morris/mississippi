// <copyright file="BarChartItem.cs" company="Microsoft">
// Licensed under the MIT License.
// </copyright>

namespace Refraction.Components.Organisms;

/// <summary>
/// Represents a data item in the BarChart.
/// </summary>
public class BarChartItem
{
    /// <summary>
    /// Gets or sets the category label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Gets or sets an optional color override.
    /// </summary>
    public string? Color { get; set; }
}
