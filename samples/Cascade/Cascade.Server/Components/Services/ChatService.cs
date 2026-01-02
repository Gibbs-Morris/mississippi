// -----------------------------------------------------------------------
// <copyright file="ChatService.cs" company="GMM">
//     Copyright (c) GMM. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Cascade.Domain.Channel;
using Cascade.Domain.Conversation;
using Cascade.Domain.User;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions;


namespace Cascade.Server.Components.Services;

/// <summary>
///     Facade for chat operations that wraps grain calls.
/// </summary>
/// <remarks>
///     <para>
///         This service provides a clean abstraction layer between Blazor components
///         and Orleans grains. It handles the grain resolution and operation result
///         processing, throwing descriptive exceptions on failure.
///     </para>
/// </remarks>
internal sealed class ChatService : IChatService
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ChatService" /> class.
    /// </summary>
    /// <param name="aggregateGrainFactory">Factory for resolving aggregate grains.</param>
    /// <param name="session">The current user session.</param>
    public ChatService(
        IAggregateGrainFactory aggregateGrainFactory,
        UserSession session
    )
    {
        AggregateGrainFactory = aggregateGrainFactory ?? throw new ArgumentNullException(nameof(aggregateGrainFactory));
        Session = session ?? throw new ArgumentNullException(nameof(session));
    }

    private IAggregateGrainFactory AggregateGrainFactory { get; }

    private UserSession Session { get; }

    private static string GenerateChannelId(
        string name
    )
    {
        // Generate a predictable but unique ID based on name and timestamp
        string normalizedName = name.ToUpperInvariant().Replace(" ", "-", StringComparison.Ordinal);
        string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        return $"channel-{normalizedName}-{timestamp}";
    }

    private static string GenerateMessageId()
    {
        // Generate a unique message ID using timestamp and GUID suffix
        string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
        string suffix = Guid.NewGuid().ToString("N")[..8];
        return $"msg-{timestamp}-{suffix}";
    }

    /// <inheritdoc />
    public async Task<string> CreateChannelAsync(
        string name,
        string? description = null,
        CancellationToken cancellationToken = default
    )
    {
        EnsureAuthenticated();

        // Generate a unique channel ID
        string channelId = GenerateChannelId(name);

        // Get the channel grain and create the channel
        BrookKey channelKey = BrookKey.ForGrain<IChannelAggregateGrain>(channelId);
        IChannelAggregateGrain channelGrain = AggregateGrainFactory.GetAggregate<IChannelAggregateGrain>(channelKey);
        OperationResult result = await channelGrain.CreateAsync(channelId, name, Session.UserId!);
        if (!result.Success)
        {
            throw new ChatOperationException(result.ErrorMessage ?? "Failed to create channel", result.ErrorCode);
        }

        // Add the creator as a member
        OperationResult memberResult = await channelGrain.AddMemberAsync(Session.UserId!);
        if (!memberResult.Success)
        {
            throw new ChatOperationException(
                memberResult.ErrorMessage ?? "Failed to add creator as member",
                memberResult.ErrorCode);
        }

        // Also update the user's channel list
        BrookKey userKey = BrookKey.ForGrain<IUserAggregateGrain>(Session.UserId!);
        IUserAggregateGrain userGrain = AggregateGrainFactory.GetAggregate<IUserAggregateGrain>(userKey);
        await userGrain.JoinChannelAsync(channelId);
        return channelId;
    }

    /// <inheritdoc />
    public async Task JoinChannelAsync(
        string channelId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(channelId);
        EnsureAuthenticated();

        // Add user to channel
        BrookKey channelKey = BrookKey.ForGrain<IChannelAggregateGrain>(channelId);
        IChannelAggregateGrain channelGrain = AggregateGrainFactory.GetAggregate<IChannelAggregateGrain>(channelKey);
        OperationResult result = await channelGrain.AddMemberAsync(Session.UserId!);
        if (!result.Success && (result.ErrorCode != AggregateErrorCodes.InvalidState))
        {
            throw new ChatOperationException(result.ErrorMessage ?? "Failed to join channel", result.ErrorCode);
        }

        // Update user's channel list
        BrookKey userKey = BrookKey.ForGrain<IUserAggregateGrain>(Session.UserId!);
        IUserAggregateGrain userGrain = AggregateGrainFactory.GetAggregate<IUserAggregateGrain>(userKey);
        OperationResult userResult = await userGrain.JoinChannelAsync(channelId);
        if (!userResult.Success && (userResult.ErrorCode != AggregateErrorCodes.InvalidState))
        {
            throw new ChatOperationException(
                userResult.ErrorMessage ?? "Failed to update channel membership",
                userResult.ErrorCode);
        }
    }

    /// <inheritdoc />
    public async Task LeaveChannelAsync(
        string channelId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(channelId);
        EnsureAuthenticated();

        // Remove user from channel
        BrookKey channelKey = BrookKey.ForGrain<IChannelAggregateGrain>(channelId);
        IChannelAggregateGrain channelGrain = AggregateGrainFactory.GetAggregate<IChannelAggregateGrain>(channelKey);
        OperationResult result = await channelGrain.RemoveMemberAsync(Session.UserId!);
        if (!result.Success && (result.ErrorCode != AggregateErrorCodes.InvalidState))
        {
            throw new ChatOperationException(result.ErrorMessage ?? "Failed to leave channel", result.ErrorCode);
        }

        // Update user's channel list
        BrookKey userKey = BrookKey.ForGrain<IUserAggregateGrain>(Session.UserId!);
        IUserAggregateGrain userGrain = AggregateGrainFactory.GetAggregate<IUserAggregateGrain>(userKey);
        OperationResult userResult = await userGrain.LeaveChannelAsync(channelId);
        if (!userResult.Success && (userResult.ErrorCode != AggregateErrorCodes.InvalidState))
        {
            throw new ChatOperationException(
                userResult.ErrorMessage ?? "Failed to update channel membership",
                userResult.ErrorCode);
        }
    }

    /// <inheritdoc />
    public async Task SendMessageAsync(
        string channelId,
        string content,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(channelId);
        ArgumentException.ThrowIfNullOrEmpty(content);
        EnsureAuthenticated();

        // For simplicity, use the channel ID as the conversation ID (1:1 relationship)
        string conversationId = channelId;

        // Get the conversation grain
        BrookKey conversationKey = BrookKey.ForGrain<IConversationAggregateGrain>(conversationId);
        IConversationAggregateGrain conversationGrain =
            AggregateGrainFactory.GetAggregate<IConversationAggregateGrain>(conversationKey);

        // Generate a unique message ID
        string messageId = GenerateMessageId();

        // First, ensure the conversation is started (idempotent operation)
        OperationResult startResult = await conversationGrain.StartAsync(conversationId, channelId);
        if (!startResult.Success && (startResult.ErrorCode != AggregateErrorCodes.InvalidState))
        {
            throw new ChatOperationException(
                startResult.ErrorMessage ?? "Failed to start conversation",
                startResult.ErrorCode);
        }

        // Send the message
        OperationResult result = await conversationGrain.SendMessageAsync(messageId, content, Session.UserId!);
        if (!result.Success)
        {
            throw new ChatOperationException(result.ErrorMessage ?? "Failed to send message", result.ErrorCode);
        }
    }

    private void EnsureAuthenticated()
    {
        if (!Session.IsAuthenticated)
        {
            throw new ChatOperationException("User must be authenticated to perform this action");
        }
    }
}