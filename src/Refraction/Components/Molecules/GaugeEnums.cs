// <copyright file="GaugeEnums.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

namespace Refraction.Components.Molecules;

/// <summary>
/// Visual variants for the Gauge component.
/// </summary>
public enum GaugeVariant
{
    /// <summary>
    /// Arc-style gauge (semi-circular).
    /// </summary>
    Arc,

    /// <summary>
    /// Horizontal bar gauge.
    /// </summary>
    Bar,

    /// <summary>
    /// Vertical bar gauge.
    /// </summary>
    VerticalBar,

    /// <summary>
    /// Full circular gauge.
    /// </summary>
    Circle,
}

/// <summary>
/// State variants for the Gauge component based on value thresholds.
/// </summary>
public enum GaugeState
{
    /// <summary>
    /// Normal operating range.
    /// </summary>
    Normal,

    /// <summary>
    /// Warning threshold reached.
    /// </summary>
    Warning,

    /// <summary>
    /// Critical threshold reached.
    /// </summary>
    Critical,
}

/// <summary>
/// Size variants for the Gauge component.
/// </summary>
public enum GaugeSize
{
    /// <summary>
    /// Compact gauge for dense layouts.
    /// </summary>
    Small,

    /// <summary>
    /// Standard gauge size.
    /// </summary>
    Medium,

    /// <summary>
    /// Large prominent gauge.
    /// </summary>
    Large,
}
