// <copyright file="ActionChipEnums.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

namespace Refraction.Components.Molecules;

/// <summary>
/// State variants for the ActionChip component.
/// </summary>
public enum ActionChipState
{
    /// <summary>
    /// Chip is available for interaction.
    /// </summary>
    Available,

    /// <summary>
    /// Chip is currently pressed.
    /// </summary>
    Pressed,

    /// <summary>
    /// Chip action is queued for execution.
    /// </summary>
    Queued,

    /// <summary>
    /// Chip is disabled.
    /// </summary>
    Disabled,
}

/// <summary>
/// Size variants for the ActionChip component.
/// </summary>
public enum ActionChipSize
{
    /// <summary>
    /// Compact chip for dense layouts.
    /// </summary>
    Small,

    /// <summary>
    /// Standard chip size.
    /// </summary>
    Medium,

    /// <summary>
    /// Large chip for prominent actions.
    /// </summary>
    Large,
}

/// <summary>
/// Visual variants for the ActionChip component.
/// </summary>
public enum ActionChipVariant
{
    /// <summary>
    /// Default outlined chip.
    /// </summary>
    Default,

    /// <summary>
    /// Primary filled chip.
    /// </summary>
    Primary,

    /// <summary>
    /// Subtle ghost chip.
    /// </summary>
    Ghost,
}
