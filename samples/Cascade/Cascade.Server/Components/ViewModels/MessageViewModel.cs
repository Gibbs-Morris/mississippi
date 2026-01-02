// <copyright file="MessageViewModel.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;


namespace Cascade.Server.Components.ViewModels;

/// <summary>
///     View model for displaying a message in the UI.
/// </summary>
/// <remarks>
///     This type is public because it is used as a parameter type in Razor components,
///     which are compiled as public classes.
/// </remarks>
#pragma warning disable CA1515 // Consider making public types internal
public sealed record MessageViewModel
#pragma warning restore CA1515
{
    /// <summary>
    ///     Gets the display name of the message author.
    /// </summary>
    public required string AuthorDisplayName { get; init; }

    /// <summary>
    ///     Gets the user ID of the author.
    /// </summary>
    public required string AuthorUserId { get; init; }

    /// <summary>
    ///     Gets the message content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the message has been deleted.
    /// </summary>
    public bool IsDeleted { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the message has been edited.
    /// </summary>
    public bool IsEdited { get; init; }

    /// <summary>
    ///     Gets the message identifier.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the message was sent.
    /// </summary>
    public required DateTimeOffset SentAt { get; init; }
}