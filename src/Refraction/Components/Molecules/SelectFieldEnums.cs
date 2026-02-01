// <copyright file="SelectFieldEnums.cs" company="Batch Enterprises">
// Copyright (c) Batch Enterprises. All rights reserved.
// </copyright>

namespace Refraction.Components.Molecules;

/// <summary>
/// Specifies the size variant for the SelectField.
/// </summary>
public enum SelectFieldSize
{
    /// <summary>
    /// Small select size.
    /// </summary>
    Small,

    /// <summary>
    /// Default medium select size.
    /// </summary>
    Medium,

    /// <summary>
    /// Large select size.
    /// </summary>
    Large,
}

/// <summary>
/// Specifies the state of the SelectField.
/// </summary>
public enum SelectFieldState
{
    /// <summary>
    /// Default idle state.
    /// </summary>
    Default,

    /// <summary>
    /// Field has an error.
    /// </summary>
    Error,

    /// <summary>
    /// Field value is valid.
    /// </summary>
    Success,
}
