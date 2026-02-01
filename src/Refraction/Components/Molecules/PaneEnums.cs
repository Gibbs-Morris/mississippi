// <copyright file="PaneEnums.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

namespace Refraction.Components.Molecules;

/// <summary>
/// Visual variants for the Pane component.
/// </summary>
public enum PaneVariant
{
    /// <summary>
    /// Default translucent pane.
    /// </summary>
    Default,

    /// <summary>
    /// Elevated pane with stronger contrast.
    /// </summary>
    Elevated,

    /// <summary>
    /// Subdued pane for background content.
    /// </summary>
    Subdued,
}

/// <summary>
/// Size presets for the Pane component.
/// </summary>
public enum PaneSize
{
    /// <summary>
    /// Auto-size based on content.
    /// </summary>
    Auto,

    /// <summary>
    /// Small fixed-width pane.
    /// </summary>
    Small,

    /// <summary>
    /// Medium fixed-width pane.
    /// </summary>
    Medium,

    /// <summary>
    /// Large fixed-width pane.
    /// </summary>
    Large,

    /// <summary>
    /// Full-width pane.
    /// </summary>
    Full,
}
