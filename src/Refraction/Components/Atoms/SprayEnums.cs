// <copyright file="SprayEnums.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

namespace Refraction.Components.Atoms;

/// <summary>
/// Intensity variants for the Spray particle effect.
/// </summary>
public enum SprayIntensity
{
    /// <summary>
    /// Light particle burst.
    /// </summary>
    Light,

    /// <summary>
    /// Medium particle burst.
    /// </summary>
    Medium,

    /// <summary>
    /// Dense particle burst.
    /// </summary>
    Heavy,
}

/// <summary>
/// Color variants for the Spray particle effect.
/// </summary>
public enum SprayColor
{
    /// <summary>
    /// Default accent color particles.
    /// </summary>
    Accent,

    /// <summary>
    /// Success-themed particles.
    /// </summary>
    Success,

    /// <summary>
    /// Warning-themed particles.
    /// </summary>
    Warning,

    /// <summary>
    /// Critical-themed particles.
    /// </summary>
    Critical,
}
