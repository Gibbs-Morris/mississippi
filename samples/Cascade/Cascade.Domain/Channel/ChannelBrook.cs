// <copyright file="ChannelBrook.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Cascade.Domain.Channel;

/// <summary>
///     Defines the brook (event stream) for channel aggregates.
/// </summary>
[BrookName("CASCADE", "CHAT", "CHANNEL")]
internal sealed class ChannelBrook : IBrookDefinition
{
    /// <inheritdoc />
    public static string BrookName => "CASCADE.CHAT.CHANNEL";
}