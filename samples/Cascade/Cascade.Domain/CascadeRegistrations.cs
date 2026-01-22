using Cascade.Domain.Aggregates.Channel;
using Cascade.Domain.Aggregates.Channel.Commands;
using Cascade.Domain.Aggregates.Channel.Events;
using Cascade.Domain.Aggregates.Channel.Handlers;
using Cascade.Domain.Aggregates.Channel.Reducers;
using Cascade.Domain.Aggregates.Conversation;
using Cascade.Domain.Aggregates.Conversation.Commands;
using Cascade.Domain.Aggregates.Conversation.Events;
using Cascade.Domain.Aggregates.Conversation.Handlers;
using Cascade.Domain.Aggregates.Conversation.Reducers;
using Cascade.Domain.Aggregates.User;
using Cascade.Domain.Aggregates.User.Commands;
using Cascade.Domain.Aggregates.User.Events;
using Cascade.Domain.Aggregates.User.Handlers;
using Cascade.Domain.Aggregates.User.Reducers;
using Cascade.Domain.Projections.ChannelMemberList;
using Cascade.Domain.Projections.ChannelMessageIds;
using Cascade.Domain.Projections.ChannelMessages;
using Cascade.Domain.Projections.OnlineUsers;
using Cascade.Domain.Projections.UserChannelList;
using Cascade.Domain.Projections.UserProfile;
using Cascade.Domain.Projections.UserProfile.Reducers;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Aggregates;
using Mississippi.EventSourcing.Reducers;
using Mississippi.EventSourcing.Snapshots;
using Mississippi.EventSourcing.UxProjections;

using ChannelMemberListReducers = Cascade.Domain.Projections.ChannelMemberList.Reducers;
using ChannelMessageIdsReducers = Cascade.Domain.Projections.ChannelMessageIds.Reducers;
using ChannelMessagesReducers = Cascade.Domain.Projections.ChannelMessages.Reducers;
using OnlineUsersReducers = Cascade.Domain.Projections.OnlineUsers.Reducers;
using UserChannelListReducers = Cascade.Domain.Projections.UserChannelList.Reducers;


namespace Cascade.Domain;

/// <summary>
///     Extension methods for registering Cascade domain services.
/// </summary>
public static class CascadeRegistrations
{
    /// <summary>
    ///     Adds the Cascade domain services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCascadeDomain(
        this IServiceCollection services
    )
    {
        // Add aggregate infrastructure
        services.AddAggregateSupport();

        // Add User aggregate
        services.AddUserAggregate();

        // Add Channel aggregate
        services.AddChannelAggregate();

        // Add Conversation aggregate
        services.AddConversationAggregate();

        // Add UX projections
        services.AddUserProfileProjection();
        services.AddUserChannelListProjection();
        services.AddChannelMessagesProjection();
        services.AddChannelMessageIdsProjection();
        services.AddChannelMemberListProjection();
        services.AddOnlineUsersProjection();

        // Add UX projections infrastructure
        services.AddUxProjections();
        return services;
    }

    /// <summary>
    ///     Adds the Channel aggregate services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    private static IServiceCollection AddChannelAggregate(
        this IServiceCollection services
    )
    {
        // Register event types for hydration
        services.AddEventType<ChannelCreated>();
        services.AddEventType<ChannelRenamed>();
        services.AddEventType<ChannelArchived>();
        services.AddEventType<MemberAdded>();
        services.AddEventType<MemberRemoved>();

        // Register command handlers
        services.AddCommandHandler<CreateChannel, ChannelAggregate, CreateChannelHandler>();
        services.AddCommandHandler<RenameChannel, ChannelAggregate, RenameChannelHandler>();
        services.AddCommandHandler<ArchiveChannel, ChannelAggregate, ArchiveChannelHandler>();
        services.AddCommandHandler<AddMember, ChannelAggregate, AddMemberHandler>();
        services.AddCommandHandler<RemoveMember, ChannelAggregate, RemoveMemberHandler>();

        // Register reducers for state computation
        services.AddReducer<ChannelCreated, ChannelAggregate, ChannelCreatedEventReducer>();
        services.AddReducer<ChannelRenamed, ChannelAggregate, ChannelRenamedEventReducer>();
        services.AddReducer<ChannelArchived, ChannelAggregate, ChannelArchivedEventReducer>();
        services.AddReducer<MemberAdded, ChannelAggregate, MemberAddedEventReducer>();
        services.AddReducer<MemberRemoved, ChannelAggregate, MemberRemovedEventReducer>();

        // Add snapshot state converter for ChannelAggregate
        services.AddSnapshotStateConverter<ChannelAggregate>();
        return services;
    }

    /// <summary>
    ///     Adds the ChannelMemberListProjection services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    private static IServiceCollection AddChannelMemberListProjection(
        this IServiceCollection services
    )
    {
        // Register reducers for ChannelMemberListProjection (UX projection)
        services
            .AddReducer<ChannelCreated, ChannelMemberListProjection,
                ChannelMemberListReducers.ChannelCreatedProjectionEventReducer>();
        services
            .AddReducer<MemberAdded, ChannelMemberListProjection,
                ChannelMemberListReducers.MemberAddedProjectionEventReducer>();
        services
            .AddReducer<MemberRemoved, ChannelMemberListProjection,
                ChannelMemberListReducers.MemberRemovedProjectionEventReducer>();

        // Add snapshot state converter for ChannelMemberListProjection
        services.AddSnapshotStateConverter<ChannelMemberListProjection>();
        return services;
    }

    /// <summary>
    ///     Adds the ChannelMessageIdsProjection services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    private static IServiceCollection AddChannelMessageIdsProjection(
        this IServiceCollection services
    )
    {
        // Register reducers for ChannelMessageIdsProjection (UX projection)
        services
            .AddReducer<ConversationStarted, ChannelMessageIdsProjection,
                ChannelMessageIdsReducers.ConversationStartedMessageIdsEventReducer>();
        services
            .AddReducer<MessageSent, ChannelMessageIdsProjection,
                ChannelMessageIdsReducers.MessageSentMessageIdsEventReducer>();

        // Add snapshot state converter for ChannelMessageIdsProjection
        services.AddSnapshotStateConverter<ChannelMessageIdsProjection>();
        return services;
    }

    /// <summary>
    ///     Adds the ChannelMessagesProjection services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    private static IServiceCollection AddChannelMessagesProjection(
        this IServiceCollection services
    )
    {
        // Register reducers for ChannelMessagesProjection (UX projection)
        services
            .AddReducer<ConversationStarted, ChannelMessagesProjection,
                ChannelMessagesReducers.ConversationStartedProjectionEventReducer>();
        services
            .AddReducer<MessageSent, ChannelMessagesProjection,
                ChannelMessagesReducers.MessageSentProjectionEventReducer>();
        services
            .AddReducer<MessageEdited, ChannelMessagesProjection,
                ChannelMessagesReducers.MessageEditedProjectionEventReducer>();
        services
            .AddReducer<MessageDeleted, ChannelMessagesProjection,
                ChannelMessagesReducers.MessageDeletedProjectionEventReducer>();

        // Add snapshot state converter for ChannelMessagesProjection
        services.AddSnapshotStateConverter<ChannelMessagesProjection>();
        return services;
    }

    /// <summary>
    ///     Adds the Conversation aggregate services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    private static IServiceCollection AddConversationAggregate(
        this IServiceCollection services
    )
    {
        // Register event types for hydration
        services.AddEventType<ConversationStarted>();
        services.AddEventType<MessageSent>();
        services.AddEventType<MessageEdited>();
        services.AddEventType<MessageDeleted>();

        // Register command handlers
        services.AddCommandHandler<StartConversation, ConversationAggregate, StartConversationHandler>();
        services.AddCommandHandler<SendMessage, ConversationAggregate, SendMessageHandler>();
        services.AddCommandHandler<EditMessage, ConversationAggregate, EditMessageHandler>();
        services.AddCommandHandler<DeleteMessage, ConversationAggregate, DeleteMessageHandler>();

        // Register reducers for state computation
        services.AddReducer<ConversationStarted, ConversationAggregate, ConversationStartedEventReducer>();
        services.AddReducer<MessageSent, ConversationAggregate, MessageSentEventReducer>();
        services.AddReducer<MessageEdited, ConversationAggregate, MessageEditedEventReducer>();
        services.AddReducer<MessageDeleted, ConversationAggregate, MessageDeletedEventReducer>();

        // Add snapshot state converter for ConversationAggregate
        services.AddSnapshotStateConverter<ConversationAggregate>();
        return services;
    }

    /// <summary>
    ///     Adds the OnlineUsersProjection services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    private static IServiceCollection AddOnlineUsersProjection(
        this IServiceCollection services
    )
    {
        // Register reducers for OnlineUsersProjection (UX projection)
        services
            .AddReducer<UserRegistered, OnlineUsersProjection,
                OnlineUsersReducers.UserRegisteredProjectionEventReducer>();
        services
            .AddReducer<DisplayNameUpdated, OnlineUsersProjection,
                OnlineUsersReducers.DisplayNameUpdatedProjectionEventReducer>();
        services
            .AddReducer<UserWentOnline, OnlineUsersProjection,
                OnlineUsersReducers.UserWentOnlineProjectionEventReducer>();
        services
            .AddReducer<UserWentOffline, OnlineUsersProjection,
                OnlineUsersReducers.UserWentOfflineProjectionEventReducer>();

        // Add snapshot state converter for OnlineUsersProjection
        services.AddSnapshotStateConverter<OnlineUsersProjection>();
        return services;
    }

    /// <summary>
    ///     Adds the User aggregate services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    private static IServiceCollection AddUserAggregate(
        this IServiceCollection services
    )
    {
        // Register event types for hydration
        services.AddEventType<UserRegistered>();
        services.AddEventType<DisplayNameUpdated>();
        services.AddEventType<UserWentOnline>();
        services.AddEventType<UserWentOffline>();
        services.AddEventType<UserJoinedChannel>();
        services.AddEventType<UserLeftChannel>();

        // Register command handlers
        services.AddCommandHandler<RegisterUser, UserAggregate, RegisterUserHandler>();
        services.AddCommandHandler<UpdateDisplayName, UserAggregate, UpdateDisplayNameHandler>();
        services.AddCommandHandler<SetOnlineStatus, UserAggregate, SetOnlineStatusHandler>();
        services.AddCommandHandler<JoinChannel, UserAggregate, JoinChannelHandler>();
        services.AddCommandHandler<LeaveChannel, UserAggregate, LeaveChannelHandler>();

        // Register reducers for state computation
        services.AddReducer<UserRegistered, UserAggregate, UserRegisteredEventReducer>();
        services.AddReducer<DisplayNameUpdated, UserAggregate, DisplayNameUpdatedEventReducer>();
        services.AddReducer<UserWentOnline, UserAggregate, UserWentOnlineEventReducer>();
        services.AddReducer<UserWentOffline, UserAggregate, UserWentOfflineEventReducer>();
        services.AddReducer<UserJoinedChannel, UserAggregate, UserJoinedChannelEventReducer>();
        services.AddReducer<UserLeftChannel, UserAggregate, UserLeftChannelEventReducer>();

        // Add snapshot state converter for UserAggregate
        services.AddSnapshotStateConverter<UserAggregate>();
        return services;
    }

    /// <summary>
    ///     Adds the UserChannelListProjection services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    private static IServiceCollection AddUserChannelListProjection(
        this IServiceCollection services
    )
    {
        // Register reducers for UserChannelListProjection (UX projection)
        services
            .AddReducer<UserRegistered, UserChannelListProjection,
                UserChannelListReducers.UserRegisteredProjectionEventReducer>();
        services
            .AddReducer<UserJoinedChannel, UserChannelListProjection,
                UserChannelListReducers.UserJoinedChannelProjectionEventReducer>();
        services
            .AddReducer<UserLeftChannel, UserChannelListProjection,
                UserChannelListReducers.UserLeftChannelProjectionEventReducer>();

        // Add snapshot state converter for UserChannelListProjection
        services.AddSnapshotStateConverter<UserChannelListProjection>();
        return services;
    }

    /// <summary>
    ///     Adds the UserProfileProjection services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    private static IServiceCollection AddUserProfileProjection(
        this IServiceCollection services
    )
    {
        // Register reducers for UserProfileProjection (UX projection)
        services.AddReducer<UserRegistered, UserProfileProjection, UserRegisteredProjectionEventReducer>();
        services.AddReducer<DisplayNameUpdated, UserProfileProjection, DisplayNameUpdatedProjectionEventReducer>();
        services.AddReducer<UserWentOnline, UserProfileProjection, UserWentOnlineProjectionEventReducer>();
        services.AddReducer<UserWentOffline, UserProfileProjection, UserWentOfflineProjectionEventReducer>();
        services.AddReducer<UserJoinedChannel, UserProfileProjection, UserJoinedChannelProjectionEventReducer>();
        services.AddReducer<UserLeftChannel, UserProfileProjection, UserLeftChannelProjectionEventReducer>();

        // Add snapshot state converter for UserProfileProjection
        services.AddSnapshotStateConverter<UserProfileProjection>();
        return services;
    }
}