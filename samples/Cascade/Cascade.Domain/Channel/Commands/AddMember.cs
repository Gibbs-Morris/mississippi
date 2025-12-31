// <copyright file="AddMember.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Orleans;


namespace Cascade.Domain.Channel.Commands;

/// <summary>
///     Command to add a member to a channel.
/// </summary>
[GenerateSerializer]
[Alias("Cascade.Domain.Channel.Commands.AddMember")]
internal sealed record AddMember
{
    /// <summary>
    ///     Gets the user ID of the member being added.
    /// </summary>
    [Id(0)]
    public required string UserId { get; init; }
}
