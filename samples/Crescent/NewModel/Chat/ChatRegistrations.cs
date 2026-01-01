using Crescent.NewModel.Chat.Commands;
using Crescent.NewModel.Chat.Events;
using Crescent.NewModel.Chat.Handlers;
using Crescent.NewModel.Chat.Reducers;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Aggregates;
using Mississippi.EventSourcing.Reducers;
using Mississippi.EventSourcing.Snapshots;


namespace Crescent.NewModel.Chat;

/// <summary>
///     Extension methods for registering chat aggregate services.
/// </summary>
internal static class ChatRegistrations
{
    /// <summary>
    ///     Adds the chat aggregate services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddChatAggregate(
        this IServiceCollection services
    )
    {
        // Add aggregate infrastructure
        services.AddAggregateSupport();

        // Register event types for hydration
        services.AddEventType<ChatCreated>();
        services.AddEventType<MessageAdded>();
        services.AddEventType<MessageEdited>();
        services.AddEventType<MessageDeleted>();

        // Register command handlers
        services.AddCommandHandler<CreateChat, ChatAggregate, CreateChatHandler>();
        services.AddCommandHandler<AddMessage, ChatAggregate, AddMessageHandler>();
        services.AddCommandHandler<EditMessage, ChatAggregate, EditMessageHandler>();
        services.AddCommandHandler<DeleteMessage, ChatAggregate, DeleteMessageHandler>();

        // Register reducers for state computation
        services.AddReducer<ChatCreated, ChatAggregate, ChatCreatedReducer>();
        services.AddReducer<MessageAdded, ChatAggregate, MessageAddedReducer>();
        services.AddReducer<MessageEdited, ChatAggregate, MessageEditedReducer>();
        services.AddReducer<MessageDeleted, ChatAggregate, MessageDeletedReducer>();

        // Add snapshot state converter for ChatAggregate
        services.AddSnapshotStateConverter<ChatAggregate>();
        return services;
    }
}