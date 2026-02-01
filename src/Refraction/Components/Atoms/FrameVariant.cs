// <copyright file="FrameVariant.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

namespace Refraction.Components.Atoms;

/// <summary>
/// Visual variants for the Frame component.
/// </summary>
public enum FrameVariant
{
    /// <summary>
    /// Default subtle corner brackets.
    /// </summary>
    Default,

    /// <summary>
    /// More prominent corner brackets with increased opacity.
    /// </summary>
    Prominent,

    /// <summary>
    /// Minimal corner brackets with reduced size.
    /// </summary>
    Minimal,
}

/// <summary>
/// Size variants for the Frame component.
/// </summary>
public enum FrameSize
{
    /// <summary>
    /// Small frame with short brackets.
    /// </summary>
    Small,

    /// <summary>
    /// Medium frame with standard brackets.
    /// </summary>
    Medium,

    /// <summary>
    /// Large frame with extended brackets.
    /// </summary>
    Large,
}
