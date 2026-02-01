// <copyright file="DatePickerEnums.cs" company="Batch Enterprises">
// Copyright (c) Batch Enterprises. All rights reserved.
// </copyright>

namespace Refraction.Components.Organisms;

/// <summary>
/// Specifies the size variant for the DatePicker.
/// </summary>
public enum DatePickerSize
{
    /// <summary>
    /// Small date picker size.
    /// </summary>
    Small,

    /// <summary>
    /// Default medium date picker size.
    /// </summary>
    Medium,

    /// <summary>
    /// Large date picker size.
    /// </summary>
    Large,
}

/// <summary>
/// Specifies the state of the DatePicker.
/// </summary>
public enum DatePickerState
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
