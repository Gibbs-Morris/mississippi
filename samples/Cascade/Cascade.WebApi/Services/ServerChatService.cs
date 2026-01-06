using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Cascade.Components.Services;
using Cascade.Domain.Channel;
using Cascade.Domain.Conversation;
using Cascade.Domain.User;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.WebApi.Services;

/// <summary>
///     Server-side implementation of <see cref="IChatService" /> for the WebApi host.
/// </summary>
/// <remarks>
///     This implementation uses the Orleans aggregate grain factory to interact
///     with channel, user, and conversation aggregates directly.
/// </remarks>
internal sealed class ServerChatService : IChatService
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ServerChatService" /> class.
    /// </summary>
    /// <param name="aggregateFactory">The aggregate grain factory.</param>
    /// <param name="userContext">The current user context.</param>
    public ServerChatService(
        IAggregateGrainFactory aggregateFactory,
        IUserContext userContext
    )
    {
        AggregateFactory = aggregateFactory
                           ?? throw new ArgumentNullException(nameof(aggregateFactory));
        UserContext = userContext
                      ?? throw new ArgumentNullException(nameof(userContext));
    }

    private IAggregateGrainFactory AggregateFactory { get; }

    private IUserContext UserContext { get; }

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
        string channelId = GenerateChannelId(name);

        // Get the channel grain and create the channel
        AggregateKey channelKey = AggregateKey.ForAggregate<IChannelAggregateGrain>(channelId);
        IChannelAggregateGrain channelGrain = AggregateFactory.GetAggregate<IChannelAggregateGrain>(channelKey);
        OperationResult result = await channelGrain.CreateAsync(channelId, name, UserContext.UserId!);
        if (!result.Success)
        {
            throw new ChatOperationException(result.ErrorMessage ?? "Failed to create channel", result.ErrorCode);
        }

        // Add the creator as a member
        OperationResult memberResult = await channelGrain.AddMemberAsync(UserContext.UserId!);
        if (!memberResult.Success)
        {
            throw new ChatOperationException(
                memberResult.ErrorMessage ?? "Failed to add creator as member",
                memberResult.ErrorCode);
        }

        // Also update the user's channel list
        AggregateKey userKey = AggregateKey.ForAggregate<IUserAggregateGrain>(UserContext.UserId!);
        IUserAggregateGrain userGrain = AggregateFactory.GetAggregate<IUserAggregateGrain>(userKey);
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
        AggregateKey channelKey = AggregateKey.ForAggregate<IChannelAggregateGrain>(channelId);
        IChannelAggregateGrain channelGrain = AggregateFactory.GetAggregate<IChannelAggregateGrain>(channelKey);
        OperationResult result = await channelGrain.AddMemberAsync(UserContext.UserId!);
        if (!result.Success && (result.ErrorCode != AggregateErrorCodes.InvalidState))
        {
            throw new ChatOperationException(result.ErrorMessage ?? "Failed to join channel", result.ErrorCode);
        }

        // Update user's channel list
        AggregateKey userKey = AggregateKey.ForAggregate<IUserAggregateGrain>(UserContext.UserId!);
        IUserAggregateGrain userGrain = AggregateFactory.GetAggregate<IUserAggregateGrain>(userKey);
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
        AggregateKey channelKey = AggregateKey.ForAggregate<IChannelAggregateGrain>(channelId);
        IChannelAggregateGrain channelGrain = AggregateFactory.GetAggregate<IChannelAggregateGrain>(channelKey);
        OperationResult result = await channelGrain.RemoveMemberAsync(UserContext.UserId!);
        if (!result.Success && (result.ErrorCode != AggregateErrorCodes.InvalidState))
        {
            throw new ChatOperationException(result.ErrorMessage ?? "Failed to leave channel", result.ErrorCode);
        }

        // Update user's channel list
        AggregateKey userKey = AggregateKey.ForAggregate<IUserAggregateGrain>(UserContext.UserId!);
        IUserAggregateGrain userGrain = AggregateFactory.GetAggregate<IUserAggregateGrain>(userKey);
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

        // Messages are stored in a Conversation aggregate keyed by channelId
        string messageId = GenerateMessageId();
        AggregateKey conversationKey = AggregateKey.ForAggregate<IConversationAggregateGrain>(channelId);
        IConversationAggregateGrain conversationGrain =
            AggregateFactory.GetAggregate<IConversationAggregateGrain>(conversationKey);
        OperationResult result = await conversationGrain.SendMessageAsync(
            messageId,
            content,
            UserContext.UserId!);
        if (!result.Success)
        {
            throw new ChatOperationException(result.ErrorMessage ?? "Failed to send message", result.ErrorCode);
        }
    }

    private void EnsureAuthenticated()
    {
        if (!UserContext.IsAuthenticated)
        {
            throw new ChatOperationException("User is not authenticated.", "USER_NOT_AUTHENTICATED");
        }
    }
}
