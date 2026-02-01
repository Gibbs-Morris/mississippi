// <copyright file="PieChartSlice.cs" company="Microsoft">
// Licensed under the MIT License.
// </copyright>

namespace Refraction.Components.Organisms;

/// <summary>
/// Represents a slice in the PieChart.
/// </summary>
public class PieChartSlice
{
    /// <summary>
    /// Gets or sets the slice label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the slice value.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Gets or sets an optional color override.
    /// </summary>
    public string? Color { get; set; }
}
