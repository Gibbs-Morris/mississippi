using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using Mississippi.Ripples.Abstractions;


namespace Mississippi.Ripples.InProcess;

/// <summary>
///     In-process implementation of <see cref="IProjectionUpdateNotifier" /> for server-side use.
/// </summary>
/// <remarks>
///     <para>
///         This notifier maintains subscriptions in memory and dispatches notifications
///         synchronously when projections change. It is designed for Blazor Server scenarios
///         where components run on the server and can react immediately to grain state changes.
///     </para>
///     <para>
///         In production, this notifier should be connected to an Orleans stream or grain
///         observer to receive real-time updates when projection grains change state.
///     </para>
/// </remarks>
internal sealed class InProcessProjectionUpdateNotifier : IProjectionUpdateNotifier
{
    private readonly ConcurrentDictionary<string, SubscriptionCollection> subscriptions = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="InProcessProjectionUpdateNotifier" /> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public InProcessProjectionUpdateNotifier(
        ILogger<InProcessProjectionUpdateNotifier> logger
    )
    {
        ArgumentNullException.ThrowIfNull(logger);
        Logger = logger;
    }

    private ILogger<InProcessProjectionUpdateNotifier> Logger { get; }

    private static string CreateKey(
        string projectionType,
        string entityId
    ) =>
        $"{projectionType}:{entityId}";

    /// <summary>
    ///     Notifies all subscribed handlers that a projection has been updated.
    /// </summary>
    /// <param name="projectionType">The type of projection that changed.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="newVersion">The new version number.</param>
    public void NotifyProjectionChanged(
        string projectionType,
        string entityId,
        long newVersion
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(projectionType);
        ArgumentException.ThrowIfNullOrEmpty(entityId);
        string key = CreateKey(projectionType, entityId);
        if (subscriptions.TryGetValue(key, out SubscriptionCollection? collection))
        {
            InProcessProjectionUpdateNotifierLoggerExtensions.NotifyingSubscribers(
                Logger,
                collection.Count,
                projectionType,
                entityId,
                newVersion);
            ProjectionUpdatedEvent args = new(projectionType, entityId, newVersion);
            collection.NotifyAll(args);
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe(
        string projectionType,
        string entityId,
        Action<ProjectionUpdatedEvent> callback
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(projectionType);
        ArgumentException.ThrowIfNullOrEmpty(entityId);
        ArgumentNullException.ThrowIfNull(callback);
        string key = CreateKey(projectionType, entityId);
        SubscriptionCollection collection = subscriptions.GetOrAdd(key, _ => new(projectionType, entityId, Logger));
        Subscription subscription = new(this, key, callback);
        collection.Add(subscription);
        InProcessProjectionUpdateNotifierLoggerExtensions.SubscriptionCreated(Logger, projectionType, entityId);
        return subscription;
    }

    private void RemoveSubscription(
        string key,
        Subscription subscription
    )
    {
        if (subscriptions.TryGetValue(key, out SubscriptionCollection? collection))
        {
            collection.Remove(subscription);
            if (collection.IsEmpty)
            {
                subscriptions.TryRemove(key, out SubscriptionCollection? _);
            }
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Action<ProjectionUpdatedEvent> callback;

        private readonly string key;

        private readonly InProcessProjectionUpdateNotifier owner;

        private bool isDisposed;

        public Subscription(
            InProcessProjectionUpdateNotifier owner,
            string key,
            Action<ProjectionUpdatedEvent> callback
        )
        {
            this.owner = owner;
            this.key = key;
            this.callback = callback;
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            owner.RemoveSubscription(key, this);
        }

        public void Invoke(
            ProjectionUpdatedEvent args
        )
        {
            if (!isDisposed)
            {
                callback(args);
            }
        }
    }

    private sealed class SubscriptionCollection
    {
        private readonly string entityId;

        private readonly List<Subscription> items = [];

        private readonly ILogger logger;

        private readonly string projectionType;

        private readonly object syncRoot = new();

        public SubscriptionCollection(
            string projectionType,
            string entityId,
            ILogger logger
        )
        {
            this.projectionType = projectionType;
            this.entityId = entityId;
            this.logger = logger;
        }

        public int Count
        {
            get
            {
                lock (syncRoot)
                {
                    return items.Count;
                }
            }
        }

        public bool IsEmpty
        {
            get
            {
                lock (syncRoot)
                {
                    return items.Count == 0;
                }
            }
        }

        public void Add(
            Subscription subscription
        )
        {
            lock (syncRoot)
            {
                items.Add(subscription);
            }
        }

        public void NotifyAll(
            ProjectionUpdatedEvent args
        )
        {
            List<Subscription> snapshot;
            lock (syncRoot)
            {
                snapshot = [.. items];
            }

            foreach (Subscription subscription in snapshot)
            {
                try
                {
                    subscription.Invoke(args);
                }
#pragma warning disable CA1031 // Catch more specific exception - callbacks may throw any exception
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    InProcessProjectionUpdateNotifierLoggerExtensions.CallbackFailed(
                        logger,
                        projectionType,
                        entityId,
                        ex);
                }
            }
        }

        public void Remove(
            Subscription subscription
        )
        {
            lock (syncRoot)
            {
                items.Remove(subscription);
            }
        }
    }
}