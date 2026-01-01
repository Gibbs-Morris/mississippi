// <copyright file="CascadeRegistrations.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Cascade.Domain.Channel;
using Cascade.Domain.Channel.Commands;
using Cascade.Domain.Channel.Events;
using Cascade.Domain.Channel.Handlers;
using Cascade.Domain.Channel.Reducers;
using Cascade.Domain.Conversation;
using Cascade.Domain.Conversation.Commands;
using Cascade.Domain.Conversation.Events;
using Cascade.Domain.Conversation.Handlers;
using Cascade.Domain.Conversation.Reducers;
using Cascade.Domain.Projections.UserProfile;
using Cascade.Domain.Projections.UserProfile.Reducers;
using Cascade.Domain.User;
using Cascade.Domain.User.Commands;
using Cascade.Domain.User.Events;
using Cascade.Domain.User.Handlers;
using Cascade.Domain.User.Reducers;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Aggregates;
using Mississippi.EventSourcing.Reducers;
using Mississippi.EventSourcing.Snapshots;
using Mississippi.EventSourcing.UxProjections;


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
        services.AddCommandHandler<CreateChannel, ChannelState, CreateChannelHandler>();
        services.AddCommandHandler<RenameChannel, ChannelState, RenameChannelHandler>();
        services.AddCommandHandler<ArchiveChannel, ChannelState, ArchiveChannelHandler>();
        services.AddCommandHandler<AddMember, ChannelState, AddMemberHandler>();
        services.AddCommandHandler<RemoveMember, ChannelState, RemoveMemberHandler>();

        // Register reducers for state computation
        services.AddReducer<ChannelCreated, ChannelState, ChannelCreatedReducer>();
        services.AddReducer<ChannelRenamed, ChannelState, ChannelRenamedReducer>();
        services.AddReducer<ChannelArchived, ChannelState, ChannelArchivedReducer>();
        services.AddReducer<MemberAdded, ChannelState, MemberAddedReducer>();
        services.AddReducer<MemberRemoved, ChannelState, MemberRemovedReducer>();

        // Add snapshot state converter for ChannelState
        services.AddSnapshotStateConverter<ChannelState>();
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
        services.AddCommandHandler<StartConversation, ConversationState, StartConversationHandler>();
        services.AddCommandHandler<SendMessage, ConversationState, SendMessageHandler>();
        services.AddCommandHandler<EditMessage, ConversationState, EditMessageHandler>();
        services.AddCommandHandler<DeleteMessage, ConversationState, DeleteMessageHandler>();

        // Register reducers for state computation
        services.AddReducer<ConversationStarted, ConversationState, ConversationStartedReducer>();
        services.AddReducer<MessageSent, ConversationState, MessageSentReducer>();
        services.AddReducer<MessageEdited, ConversationState, MessageEditedReducer>();
        services.AddReducer<MessageDeleted, ConversationState, MessageDeletedReducer>();

        // Add snapshot state converter for ConversationState
        services.AddSnapshotStateConverter<ConversationState>();
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
        services.AddCommandHandler<RegisterUser, UserState, RegisterUserHandler>();
        services.AddCommandHandler<UpdateDisplayName, UserState, UpdateDisplayNameHandler>();
        services.AddCommandHandler<SetOnlineStatus, UserState, SetOnlineStatusHandler>();
        services.AddCommandHandler<JoinChannel, UserState, JoinChannelHandler>();
        services.AddCommandHandler<LeaveChannel, UserState, LeaveChannelHandler>();

        // Register reducers for state computation
        services.AddReducer<UserRegistered, UserState, UserRegisteredReducer>();
        services.AddReducer<DisplayNameUpdated, UserState, DisplayNameUpdatedReducer>();
        services.AddReducer<UserWentOnline, UserState, UserWentOnlineReducer>();
        services.AddReducer<UserWentOffline, UserState, UserWentOfflineReducer>();
        services.AddReducer<UserJoinedChannel, UserState, UserJoinedChannelReducer>();
        services.AddReducer<UserLeftChannel, UserState, UserLeftChannelReducer>();

        // Add snapshot state converter for UserState
        services.AddSnapshotStateConverter<UserState>();
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
        services.AddReducer<UserRegistered, UserProfileProjection, UserRegisteredProjectionReducer>();
        services.AddReducer<DisplayNameUpdated, UserProfileProjection, DisplayNameUpdatedProjectionReducer>();
        services.AddReducer<UserWentOnline, UserProfileProjection, UserWentOnlineProjectionReducer>();
        services.AddReducer<UserWentOffline, UserProfileProjection, UserWentOfflineProjectionReducer>();
        services.AddReducer<UserJoinedChannel, UserProfileProjection, UserJoinedChannelProjectionReducer>();
        services.AddReducer<UserLeftChannel, UserProfileProjection, UserLeftChannelProjectionReducer>();

        // Add snapshot state converter for UserProfileProjection
        services.AddSnapshotStateConverter<UserProfileProjection>();
        return services;
    }
}