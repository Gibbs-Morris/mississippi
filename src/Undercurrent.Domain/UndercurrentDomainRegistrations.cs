using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.DomainModeling.Runtime;
using Mississippi.Tributary.Runtime;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Commands;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Handlers;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Reducers;


namespace Mississippi.Undercurrent.Domain;

/// <summary>
///     Provides the public entry point for applications to register Undercurrent business behavior.
/// </summary>
public static class UndercurrentDomainRegistrations
{
    /// <summary>
    ///     Registers the Undercurrent notification aggregate using existing framework services.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUndercurrentDomain(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddUserNotificationsAggregate();
    }

    private static IServiceCollection AddUserNotificationsAggregate(
        this IServiceCollection services
    ) =>
        services.AddEventType<NotificationCreated>()
            .AddEventType<NotificationMarkedAsRead>()
            .AddEventType<NotificationDeleted>()
            .AddCommandHandler<CreateNotification, UserNotificationsAggregate, CreateNotificationHandler>()
            .AddCommandHandler<MarkNotificationAsRead, UserNotificationsAggregate, MarkNotificationAsReadHandler>()
            .AddCommandHandler<DeleteNotification, UserNotificationsAggregate, DeleteNotificationHandler>()
            .AddReducer<NotificationCreated, UserNotificationsAggregate, NotificationCreatedReducer>()
            .AddReducer<NotificationMarkedAsRead, UserNotificationsAggregate, NotificationMarkedAsReadReducer>()
            .AddReducer<NotificationDeleted, UserNotificationsAggregate, NotificationDeletedReducer>()
            .AddSnapshotStateConverter<UserNotificationsAggregate>();
}