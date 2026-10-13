using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Mississippi.DomainModeling.Runtime;
using Mississippi.Tributary.Runtime;
using Mississippi.Undercurrent.Abstractions;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands;
using Mississippi.Undercurrent.Abstractions.Projections.UserNotificationCounts;
using Mississippi.Undercurrent.Abstractions.Projections.UserNotificationList;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Handlers;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Reducers;
using Mississippi.Undercurrent.Domain.Projections.UserNotificationCounts.Reducers;


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
    ) =>
        AddUndercurrentDomainCore(services, static _ => { });

    /// <summary>Registers Undercurrent with host-supplied business policy.</summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="configure">The options configuration callback.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUndercurrentDomain(
        this IServiceCollection services,
        Action<UndercurrentOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        return AddUndercurrentDomainCore(services, options => options.Configure(configure));
    }

    /// <summary>Registers Undercurrent with policy bound from a configuration section.</summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="configuration">The Undercurrent configuration section.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUndercurrentDomain(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return AddUndercurrentDomainCore(services, options => options.Bind(configuration));
    }

    /// <summary>Registers Undercurrent with explicit notification capacities and request lifetime.</summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="maxNotifications">The retained notification capacity.</param>
    /// <param name="maxCreationReceipts">The total creation receipt capacity.</param>
    /// <param name="maxRequestLifetime">The maximum future request lifetime.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUndercurrentDomain(
        this IServiceCollection services,
        int maxNotifications,
        int maxCreationReceipts,
        TimeSpan maxRequestLifetime
    ) =>
        services.AddUndercurrentDomain(options =>
        {
            options.Notifications.MaxNotifications = maxNotifications;
            options.Notifications.MaxCreationReceipts = maxCreationReceipts;
            options.Notifications.MaxRequestLifetime = maxRequestLifetime;
        });

    private static IServiceCollection AddUndercurrentDomainCore(
        IServiceCollection services,
        Action<OptionsBuilder<UndercurrentOptions>> configure
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        OptionsBuilder<UndercurrentOptions> options = services.AddOptions<UndercurrentOptions>();
        configure(options);
        options.ValidateOnStart();
        services.TryAddSingleton<UserNotificationPolicy>();
        services.TryAddTransient<UserNotificationStateSizeLimit>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<UndercurrentOptions>, UndercurrentOptionsValidator>());
        return services.AddUxProjections()
            .AddUserNotificationsAggregate()
            .AddUserNotificationCountsProjection()
            .AddUserNotificationListProjection();
    }

    private static IServiceCollection AddUserNotificationCountsProjection(
        this IServiceCollection services
    ) =>
        services.AddSnapshotType<UserNotificationCountsProjection>()
            .AddReducer<NotificationCreated, UserNotificationCountsProjection, NotificationCreatedProjectionReducer>()
            .AddReducer<NotificationMarkedAsRead, UserNotificationCountsProjection,
                NotificationMarkedAsReadProjectionReducer>()
            .AddReducer<NotificationDeleted, UserNotificationCountsProjection, NotificationDeletedProjectionReducer>()
            .AddReducer<NotificationsDeleted, UserNotificationCountsProjection, NotificationsDeletedProjectionReducer>()
            .AddSnapshotStateConverter<UserNotificationCountsProjection>();

    private static IServiceCollection AddUserNotificationListProjection(
        this IServiceCollection services
    ) =>
        services.AddSnapshotType<UserNotificationListProjection>()
            .AddReducer<NotificationCreated, UserNotificationListProjection,
                Projections.UserNotificationList.Reducers.NotificationCreatedProjectionReducer>()
            .AddReducer<NotificationMarkedAsRead, UserNotificationListProjection,
                Projections.UserNotificationList.Reducers.NotificationMarkedAsReadProjectionReducer>()
            .AddReducer<NotificationDeleted, UserNotificationListProjection,
                Projections.UserNotificationList.Reducers.NotificationDeletedProjectionReducer>()
            .AddReducer<NotificationsDeleted, UserNotificationListProjection,
                Projections.UserNotificationList.Reducers.NotificationsDeletedProjectionReducer>()
            .AddSnapshotStateConverter<UserNotificationListProjection>();

    private static IServiceCollection AddUserNotificationsAggregate(
        this IServiceCollection services
    ) =>
        services.AddEventType<NotificationCreated>()
            .AddEventType<NotificationMarkedAsRead>()
            .AddEventType<NotificationDeleted>()
            .AddEventType<NotificationsDeleted>()
            .AddCommandHandler<CreateNotification, UserNotificationsAggregate, CreateNotificationHandler>()
            .AddCommandHandler<MarkNotificationAsRead, UserNotificationsAggregate, MarkNotificationAsReadHandler>()
            .AddCommandHandler<DeleteNotification, UserNotificationsAggregate, DeleteNotificationHandler>()
            .AddCommandHandler<DeleteNotificationsBefore, UserNotificationsAggregate,
                DeleteNotificationsBeforeHandler>()
            .AddReducer<NotificationCreated, UserNotificationsAggregate, NotificationCreatedReducer>()
            .AddReducer<NotificationMarkedAsRead, UserNotificationsAggregate, NotificationMarkedAsReadReducer>()
            .AddReducer<NotificationDeleted, UserNotificationsAggregate, NotificationDeletedReducer>()
            .AddReducer<NotificationsDeleted, UserNotificationsAggregate, NotificationsDeletedReducer>()
            .AddSnapshotStateConverter<UserNotificationsAggregate>();
}