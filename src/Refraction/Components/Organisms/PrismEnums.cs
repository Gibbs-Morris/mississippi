// <copyright file="PrismEnums.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

namespace Refraction.Components.Organisms;

/// <summary>
/// Split mode for the Prism component.
/// </summary>
public enum PrismSplitMode
{
    /// <summary>
    /// Single pane, no split.
    /// </summary>
    NoSplit,

    /// <summary>
    /// Horizontal split (panes stacked vertically).
    /// </summary>
    Horizontal,

    /// <summary>
    /// Vertical split (panes side by side).
    /// </summary>
    Vertical,
}

/// <summary>
/// Size distribution for split panes.
/// </summary>
public enum PrismDistribution
{
    /// <summary>
    /// Equal distribution between panes.
    /// </summary>
    Equal,

    /// <summary>
    /// Primary pane takes more space.
    /// </summary>
    PrimaryLarger,

    /// <summary>
    /// Secondary pane takes more space.
    /// </summary>
    SecondaryLarger,
}
