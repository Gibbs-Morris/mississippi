// <copyright file="DriftGridDensity.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

namespace Refraction.Components.Atoms;

/// <summary>
/// Density variants for the DriftGrid component.
/// </summary>
public enum DriftGridDensity
{
    /// <summary>
    /// Sparse grid with wide spacing.
    /// </summary>
    Sparse,

    /// <summary>
    /// Default grid density.
    /// </summary>
    Default,

    /// <summary>
    /// Dense grid for detailed reference.
    /// </summary>
    Dense,
}
