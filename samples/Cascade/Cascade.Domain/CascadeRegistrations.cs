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
        services.AddCommandHandler<CreateChannel, ChannelAggregate, CreateChannelHandler>();
        services.AddCommandHandler<RenameChannel, ChannelAggregate, RenameChannelHandler>();
        services.AddCommandHandler<ArchiveChannel, ChannelAggregate, ArchiveChannelHandler>();
        services.AddCommandHandler<AddMember, ChannelAggregate, AddMemberHandler>();
        services.AddCommandHandler<RemoveMember, ChannelAggregate, RemoveMemberHandler>();

        // Register reducers for state computation
        services.AddReducer<ChannelCreated, ChannelAggregate, ChannelCreatedReducer>();
        services.AddReducer<ChannelRenamed, ChannelAggregate, ChannelRenamedReducer>();
        services.AddReducer<ChannelArchived, ChannelAggregate, ChannelArchivedReducer>();
        services.AddReducer<MemberAdded, ChannelAggregate, MemberAddedReducer>();
        services.AddReducer<MemberRemoved, ChannelAggregate, MemberRemovedReducer>();

        // Add snapshot state converter for ChannelAggregate
        services.AddSnapshotStateConverter<ChannelAggregate>();
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
        services.AddReducer<ConversationStarted, ConversationAggregate, ConversationStartedReducer>();
        services.AddReducer<MessageSent, ConversationAggregate, MessageSentReducer>();
        services.AddReducer<MessageEdited, ConversationAggregate, MessageEditedReducer>();
        services.AddReducer<MessageDeleted, ConversationAggregate, MessageDeletedReducer>();

        // Add snapshot state converter for ConversationAggregate
        services.AddSnapshotStateConverter<ConversationAggregate>();
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
        services.AddReducer<UserRegistered, UserAggregate, UserRegisteredReducer>();
        services.AddReducer<DisplayNameUpdated, UserAggregate, DisplayNameUpdatedReducer>();
        services.AddReducer<UserWentOnline, UserAggregate, UserWentOnlineReducer>();
        services.AddReducer<UserWentOffline, UserAggregate, UserWentOfflineReducer>();
        services.AddReducer<UserJoinedChannel, UserAggregate, UserJoinedChannelReducer>();
        services.AddReducer<UserLeftChannel, UserAggregate, UserLeftChannelReducer>();

        // Add snapshot state converter for UserAggregate
        services.AddSnapshotStateConverter<UserAggregate>();
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