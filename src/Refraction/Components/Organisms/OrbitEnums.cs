// <copyright file="OrbitEnums.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

namespace Refraction.Components.Organisms;

/// <summary>
/// Size variants for the Orbit component.
/// </summary>
public enum OrbitSize
{
    /// <summary>
    /// Compact orbit for constrained spaces.
    /// </summary>
    Small,

    /// <summary>
    /// Standard orbit size.
    /// </summary>
    Medium,

    /// <summary>
    /// Large prominent orbit.
    /// </summary>
    Large,
}

/// <summary>
/// State variants for the Orbit component.
/// </summary>
public enum OrbitState
{
    /// <summary>
    /// Default idle state.
    /// </summary>
    Idle,

    /// <summary>
    /// A segment is actively selected.
    /// </summary>
    Active,
}
