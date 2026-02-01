// <copyright file="DockEnums.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

namespace Refraction.Components.Organisms;

/// <summary>
/// Position for the Dock component.
/// </summary>
public enum DockPosition
{
    /// <summary>
    /// Dock at the top of the container.
    /// </summary>
    Top,

    /// <summary>
    /// Dock at the bottom of the container.
    /// </summary>
    Bottom,

    /// <summary>
    /// Dock at the left of the container.
    /// </summary>
    Left,

    /// <summary>
    /// Dock at the right of the container.
    /// </summary>
    Right,
}

/// <summary>
/// Size variants for the Dock component.
/// </summary>
public enum DockSize
{
    /// <summary>
    /// Compact dock with minimal padding.
    /// </summary>
    Compact,

    /// <summary>
    /// Standard dock size.
    /// </summary>
    Default,
}
