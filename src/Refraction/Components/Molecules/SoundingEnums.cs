// <copyright file="SoundingEnums.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

namespace Refraction.Components.Molecules;

/// <summary>
/// Size variants for the Sounding component.
/// </summary>
public enum SoundingSize
{
    /// <summary>
    /// Compact readout for dense layouts.
    /// </summary>
    Small,

    /// <summary>
    /// Standard readout size.
    /// </summary>
    Medium,

    /// <summary>
    /// Large prominent readout.
    /// </summary>
    Large,
}

/// <summary>
/// State variants for the Sounding component.
/// </summary>
public enum SoundingState
{
    /// <summary>
    /// Static value display.
    /// </summary>
    Idle,

    /// <summary>
    /// Value is currently updating.
    /// </summary>
    Updating,
}
