// <copyright file="NavRailEnums.cs" company="Batch Enterprises">
// Copyright (c) Batch Enterprises. All rights reserved.
// </copyright>

namespace Refraction.Components.Organisms;

/// <summary>
/// Specifies the position of the NavRail.
/// </summary>
public enum NavRailPosition
{
    /// <summary>
    /// Left side of the viewport.
    /// </summary>
    Left,

    /// <summary>
    /// Right side of the viewport.
    /// </summary>
    Right,
}

/// <summary>
/// Specifies the state of the NavRail.
/// </summary>
public enum NavRailState
{
    /// <summary>
    /// Collapsed to icons only.
    /// </summary>
    Collapsed,

    /// <summary>
    /// Expanded to show labels.
    /// </summary>
    Expanded,
}
