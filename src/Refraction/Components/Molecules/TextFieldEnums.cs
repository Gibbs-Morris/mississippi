// <copyright file="TextFieldEnums.cs" company="Batch Enterprises">
// Copyright (c) Batch Enterprises. All rights reserved.
// </copyright>

namespace Refraction.Components.Molecules;

/// <summary>
/// Specifies the size variant for the TextField.
/// </summary>
public enum TextFieldSize
{
    /// <summary>
    /// Small text field size.
    /// </summary>
    Small,

    /// <summary>
    /// Default medium text field size.
    /// </summary>
    Medium,

    /// <summary>
    /// Large text field size.
    /// </summary>
    Large,
}

/// <summary>
/// Specifies the state of the TextField.
/// </summary>
public enum TextFieldState
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

/// <summary>
/// Specifies the type of input for the TextField.
/// </summary>
public enum TextFieldType
{
    /// <summary>
    /// Standard text input.
    /// </summary>
    Text,

    /// <summary>
    /// Password input with masking.
    /// </summary>
    Password,

    /// <summary>
    /// Email input with validation.
    /// </summary>
    Email,

    /// <summary>
    /// Numeric input.
    /// </summary>
    Number,

    /// <summary>
    /// Search input with clear functionality.
    /// </summary>
    Search,

    /// <summary>
    /// URL input with validation.
    /// </summary>
    Url,

    /// <summary>
    /// Telephone input.
    /// </summary>
    Tel,
}
