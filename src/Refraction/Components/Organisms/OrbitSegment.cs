// <copyright file="OrbitSegment.cs" company="Microsoft">
// Licensed under the MIT License.
// </copyright>

namespace Refraction.Components.Organisms;

/// <summary>
/// Represents a segment in the Orbit navigation.
/// </summary>
public class OrbitSegment
{
    /// <summary>
    /// Gets or sets the segment label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional icon identifier.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets optional custom data associated with this segment.
    /// </summary>
    public object? Data { get; set; }
}
