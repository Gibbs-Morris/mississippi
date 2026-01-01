// <copyright file="ConversationBrook.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Cascade.Domain.Conversation;

/// <summary>
///     Defines the brook (event stream) for conversation aggregates.
/// </summary>
[BrookName("CASCADE", "CHAT", "CONVERSATION")]
internal sealed class ConversationBrook : IBrookDefinition
{
    /// <inheritdoc />
    public static string BrookName => "CASCADE.CHAT.CONVERSATION";
}