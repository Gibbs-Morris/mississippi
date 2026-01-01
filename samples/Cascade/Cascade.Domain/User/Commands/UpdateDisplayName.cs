// <copyright file="UpdateDisplayName.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Orleans;


namespace Cascade.Domain.User.Commands;

/// <summary>
///     Command to update a user's display name.
/// </summary>
[GenerateSerializer]
[Alias("Cascade.Domain.User.Commands.UpdateDisplayName")]
internal sealed record UpdateDisplayName
{
    /// <summary>
    ///     Gets the new display name.
    /// </summary>
    [Id(0)]
    public required string NewDisplayName { get; init; }
}