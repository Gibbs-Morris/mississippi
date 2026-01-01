using Crescent.NewModel.Chat.Commands;
using Crescent.NewModel.Chat.Events;
using Crescent.NewModel.Chat.Handlers;
using Crescent.NewModel.Chat.Reducers;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Aggregates;
using Mississippi.EventSourcing.Reducers;


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
        services.AddCommandHandler<CreateChat, ChatState, CreateChatHandler>();
        services.AddCommandHandler<AddMessage, ChatState, AddMessageHandler>();
        services.AddCommandHandler<EditMessage, ChatState, EditMessageHandler>();
        services.AddCommandHandler<DeleteMessage, ChatState, DeleteMessageHandler>();

        // Register reducers for state computation
        services.AddReducer<ChatCreated, ChatState, ChatCreatedReducer>();
        services.AddReducer<MessageAdded, ChatState, MessageAddedReducer>();
        services.AddReducer<MessageEdited, ChatState, MessageEditedReducer>();
        services.AddReducer<MessageDeleted, ChatState, MessageDeletedReducer>();

        return services;
    }
}
