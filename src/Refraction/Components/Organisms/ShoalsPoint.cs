// <copyright file="ShoalsPoint.cs" company="Microsoft">
// Licensed under the MIT License.
// </copyright>

namespace Refraction.Components.Organisms;

/// <summary>
/// Represents a point in the Shoals visualization.
/// </summary>
public class ShoalsPoint
{
    /// <summary>
    /// Gets or sets the X coordinate.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Gets or sets an optional label for the point.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets an optional Z coordinate for 3D effects.
    /// </summary>
    public double Z { get; set; }
}
