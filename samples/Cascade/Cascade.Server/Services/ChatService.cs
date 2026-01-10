using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Cascade.Components.Services;
using Cascade.Domain.Channel;
using Cascade.Domain.Channel.Commands;
using Cascade.Domain.Conversation;
using Cascade.Domain.Conversation.Commands;
using Cascade.Domain.User;
using Cascade.Domain.User.Commands;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Server.Services;

/// <summary>
///     Facade for chat operations that wraps grain calls.
/// </summary>
/// <remarks>
///     <para>
///         This service provides a clean abstraction layer between Blazor components
///         and Orleans grains. It handles the grain resolution and operation result
///         processing, throwing descriptive exceptions on failure.
///     </para>
///     <para>
///         Uses GenericAggregateGrain pattern with ExecuteAsync for all commands.
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
        // Generate a unique ID based on normalized name and GUID suffix for uniqueness
        string normalizedName = name.Replace(" ", "-", StringComparison.Ordinal).ToUpperInvariant();
        string suffix = Guid.NewGuid().ToString("N")[..8];
        return $"channel-{normalizedName}-{suffix}";
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
        IGenericAggregateGrain<ChannelAggregate> channelGrain =
            AggregateGrainFactory.GetGenericAggregate<ChannelAggregate>(channelId);
        OperationResult result = await channelGrain.ExecuteAsync(
            new CreateChannel
            {
                ChannelId = channelId,
                Name = name,
                CreatedBy = Session.UserId!,
            },
            cancellationToken);
        if (!result.Success)
        {
            throw new ChatOperationException(result.ErrorMessage ?? "Failed to create channel", result.ErrorCode);
        }

        // Add the creator as a member
        OperationResult memberResult = await channelGrain.ExecuteAsync(
            new AddMember
            {
                UserId = Session.UserId!,
            },
            cancellationToken);
        if (!memberResult.Success)
        {
            throw new ChatOperationException(
                memberResult.ErrorMessage ?? "Failed to add creator as member",
                memberResult.ErrorCode);
        }

        // Also update the user's channel list
        IGenericAggregateGrain<UserAggregate> userGrain =
            AggregateGrainFactory.GetGenericAggregate<UserAggregate>(Session.UserId!);
        await userGrain.ExecuteAsync(
            new JoinChannel
            {
                ChannelId = channelId,
            },
            cancellationToken);
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
        IGenericAggregateGrain<ChannelAggregate> channelGrain =
            AggregateGrainFactory.GetGenericAggregate<ChannelAggregate>(channelId);
        OperationResult result = await channelGrain.ExecuteAsync(
            new AddMember
            {
                UserId = Session.UserId!,
            },
            cancellationToken);
        if (!result.Success && (result.ErrorCode != AggregateErrorCodes.InvalidState))
        {
            throw new ChatOperationException(result.ErrorMessage ?? "Failed to join channel", result.ErrorCode);
        }

        // Update user's channel list
        IGenericAggregateGrain<UserAggregate> userGrain =
            AggregateGrainFactory.GetGenericAggregate<UserAggregate>(Session.UserId!);
        OperationResult userResult = await userGrain.ExecuteAsync(
            new JoinChannel
            {
                ChannelId = channelId,
            },
            cancellationToken);
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
        IGenericAggregateGrain<ChannelAggregate> channelGrain =
            AggregateGrainFactory.GetGenericAggregate<ChannelAggregate>(channelId);
        OperationResult result = await channelGrain.ExecuteAsync(
            new RemoveMember
            {
                UserId = Session.UserId!,
            },
            cancellationToken);
        if (!result.Success && (result.ErrorCode != AggregateErrorCodes.InvalidState))
        {
            throw new ChatOperationException(result.ErrorMessage ?? "Failed to leave channel", result.ErrorCode);
        }

        // Update user's channel list
        IGenericAggregateGrain<UserAggregate> userGrain =
            AggregateGrainFactory.GetGenericAggregate<UserAggregate>(Session.UserId!);
        OperationResult userResult = await userGrain.ExecuteAsync(
            new LeaveChannel
            {
                ChannelId = channelId,
            },
            cancellationToken);
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
        IGenericAggregateGrain<ConversationAggregate> conversationGrain =
            AggregateGrainFactory.GetGenericAggregate<ConversationAggregate>(conversationId);

        // Generate a unique message ID
        string messageId = GenerateMessageId();

        // First, ensure the conversation is started (idempotent operation)
        OperationResult startResult = await conversationGrain.ExecuteAsync(
            new StartConversation
            {
                ConversationId = conversationId,
                ChannelId = channelId,
            },
            cancellationToken);
        if (!startResult.Success && (startResult.ErrorCode != AggregateErrorCodes.InvalidState))
        {
            throw new ChatOperationException(
                startResult.ErrorMessage ?? "Failed to start conversation",
                startResult.ErrorCode);
        }

        // Send the message
        OperationResult result = await conversationGrain.ExecuteAsync(
            new SendMessage
            {
                MessageId = messageId,
                Content = content,
                SentBy = Session.UserId!,
            },
            cancellationToken);
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