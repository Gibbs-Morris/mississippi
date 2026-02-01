// <copyright file="FocusReticleEnums.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

namespace Refraction.Components.Molecules;

/// <summary>
/// State variants for the FocusReticle component.
/// </summary>
public enum FocusReticleState
{
    /// <summary>
    /// Reticle is acquiring target (animating).
    /// </summary>
    Acquiring,

    /// <summary>
    /// Reticle is locked on target (stable).
    /// </summary>
    Locked,
}

/// <summary>
/// Size variants for the FocusReticle component.
/// </summary>
public enum FocusReticleSize
{
    /// <summary>
    /// Compact reticle for small targets.
    /// </summary>
    Small,

    /// <summary>
    /// Standard reticle size.
    /// </summary>
    Medium,

    /// <summary>
    /// Large reticle for prominent targets.
    /// </summary>
    Large,
}
