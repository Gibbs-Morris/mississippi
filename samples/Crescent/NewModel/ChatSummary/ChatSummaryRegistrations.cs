using Crescent.NewModel.Chat.Events;
using Crescent.NewModel.ChatSummary.Reducers;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.NewModel.ChatSummary;

/// <summary>
///     Extension methods for registering ChatSummary projection services.
/// </summary>
internal static class ChatSummaryRegistrations
{
    /// <summary>
    ///     Adds the ChatSummary projection reducers to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddChatSummaryProjection(
        this IServiceCollection services
    )
    {
        // Register reducers for ChatSummaryProjection
        services.AddSingleton<IReducer<ChatCreated, ChatSummaryProjection>, ChatSummaryCreatedReducer>();
        services.AddSingleton<IReducer<MessageAdded, ChatSummaryProjection>, ChatSummaryMessageAddedReducer>();
        services.AddSingleton<IReducer<MessageEdited, ChatSummaryProjection>, ChatSummaryMessageEditedReducer>();
        services.AddSingleton<IReducer<MessageDeleted, ChatSummaryProjection>, ChatSummaryMessageDeletedReducer>();

        return services;
    }
}
