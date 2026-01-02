// <copyright file="MemberViewModel.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

namespace Cascade.Server.Components.ViewModels;

/// <summary>
///     View model for displaying a channel member in the UI.
/// </summary>
/// <remarks>
///     This type is public because it is used as a parameter type in Razor components,
///     which are compiled as public classes.
/// </remarks>
#pragma warning disable CA1515 // Consider making public types internal
public sealed record MemberViewModel
#pragma warning restore CA1515
{
    /// <summary>
    ///     Gets the display name of the member.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the member is online.
    /// </summary>
    public bool IsOnline { get; init; }

    /// <summary>
    ///     Gets the user ID of the member.
    /// </summary>
    public required string UserId { get; init; }
}