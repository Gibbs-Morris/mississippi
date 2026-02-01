// <copyright file="LineChartPoint.cs" company="Microsoft">
// Licensed under the MIT License.
// </copyright>

namespace Refraction.Components.Organisms;

/// <summary>
/// Represents a data point in the LineChart.
/// </summary>
public class LineChartPoint
{
    /// <summary>
    /// Gets or sets the data point label (typically a date or category).
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the numeric value.
    /// </summary>
    public double Value { get; set; }
}
