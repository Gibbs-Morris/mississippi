// <copyright file="BearingEnums.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

namespace Refraction.Components.Molecules;

/// <summary>
/// Size variants for the Bearing component.
/// </summary>
public enum BearingSize
{
    /// <summary>
    /// Compact compass for dense layouts.
    /// </summary>
    Small,

    /// <summary>
    /// Standard compass size.
    /// </summary>
    Medium,

    /// <summary>
    /// Large prominent compass.
    /// </summary>
    Large,
}

/// <summary>
/// State variants for the Bearing component.
/// </summary>
public enum BearingState
{
    /// <summary>
    /// Static heading display.
    /// </summary>
    Idle,

    /// <summary>
    /// Actively tracking heading changes.
    /// </summary>
    Tracking,
}
