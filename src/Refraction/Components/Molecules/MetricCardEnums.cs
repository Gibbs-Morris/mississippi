// <copyright file="MetricCardEnums.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

namespace Refraction.Components.Molecules;

/// <summary>
/// Trend direction for the MetricCard component.
/// </summary>
public enum MetricCardTrend
{
    /// <summary>
    /// No trend displayed.
    /// </summary>
    None,

    /// <summary>
    /// Upward trend (positive).
    /// </summary>
    Up,

    /// <summary>
    /// Downward trend (negative).
    /// </summary>
    Down,

    /// <summary>
    /// Stable/flat trend.
    /// </summary>
    Stable,
}

/// <summary>
/// Size variants for the MetricCard component.
/// </summary>
public enum MetricCardSize
{
    /// <summary>
    /// Compact metric card.
    /// </summary>
    Small,

    /// <summary>
    /// Standard metric card.
    /// </summary>
    Medium,

    /// <summary>
    /// Large prominent metric card.
    /// </summary>
    Large,
}
