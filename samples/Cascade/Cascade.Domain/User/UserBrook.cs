// <copyright file="UserBrook.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Cascade.Domain.User;

/// <summary>
///     Brook definition for user aggregates.
///     Provides compile-time type safety for referencing the user event stream.
/// </summary>
[BrookName("CASCADE", "CHAT", "USER")]
internal sealed class UserBrook : IBrookDefinition
{
    /// <inheritdoc />
    public static string BrookName => "CASCADE.CHAT.USER";
}
