// <copyright file="ShoalsEnums.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

namespace Refraction.Components.Organisms;

/// <summary>
/// State variants for the Shoals component.
/// </summary>
public enum ShoalsState
{
    /// <summary>
    /// Background ambient state.
    /// </summary>
    Ambient,

    /// <summary>
    /// Focused with increased visibility.
    /// </summary>
    Focused,

    /// <summary>
    /// Inspected with full detail.
    /// </summary>
    Inspected,
}

/// <summary>
/// Visual style variants for the Shoals component.
/// </summary>
public enum ShoalsStyle
{
    /// <summary>
    /// Wireframe rendering.
    /// </summary>
    Wireframe,

    /// <summary>
    /// Ghosted/translucent rendering.
    /// </summary>
    Ghosted,

    /// <summary>
    /// Solid rendering with depth.
    /// </summary>
    Solid,
}
