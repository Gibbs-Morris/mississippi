// <copyright file="RegisterUser.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Orleans;


namespace Cascade.Domain.User.Commands;

/// <summary>
///     Command to register a new user.
/// </summary>
[GenerateSerializer]
[Alias("Cascade.Domain.User.Commands.RegisterUser")]
internal sealed record RegisterUser
{
    /// <summary>
    ///     Gets the user's display name.
    /// </summary>
    [Id(1)]
    public required string DisplayName { get; init; }

    /// <summary>
    ///     Gets the unique identifier for the user.
    /// </summary>
    [Id(0)]
    public required string UserId { get; init; }
}