using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using Mississippi.Inlet.Abstractions;


namespace Mississippi.Inlet.InProcess;

/// <summary>
///     In-process implementation of <see cref="IServerProjectionNotifier" /> for server-side use.
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
internal sealed class InProcessProjectionNotifier : IServerProjectionNotifier
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="InProcessProjectionNotifier" /> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public InProcessProjectionNotifier(
        ILogger<InProcessProjectionNotifier> logger
    )
    {
        ArgumentNullException.ThrowIfNull(logger);
        Logger = logger;
    }

    private ILogger<InProcessProjectionNotifier> Logger { get; }

    private ConcurrentDictionary<string, SubscriptionCollection> Subscriptions { get; } = new();

    private static string CreateKey(
        string projectionType,
        string entityId
    ) =>
        $"{projectionType}:{entityId}";

    /// <inheritdoc />
    public void NotifyProjectionChanged(
        string projectionType,
        string entityId,
        long newVersion
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(projectionType);
        ArgumentException.ThrowIfNullOrEmpty(entityId);
        string key = CreateKey(projectionType, entityId);
        if (Subscriptions.TryGetValue(key, out SubscriptionCollection? collection))
        {
            InProcessProjectionNotifierLoggerExtensions.NotifyingSubscribers(
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
        SubscriptionCollection collection = Subscriptions.GetOrAdd(key, _ => new(projectionType, entityId, Logger));
        Subscription subscription = new(this, key, callback);
        collection.Add(subscription);
        InProcessProjectionNotifierLoggerExtensions.SubscriptionCreated(Logger, projectionType, entityId);
        return subscription;
    }

    private void RemoveSubscription(
        string key,
        Subscription subscription
    )
    {
        if (Subscriptions.TryGetValue(key, out SubscriptionCollection? collection))
        {
            collection.Remove(subscription);
            if (collection.IsEmpty)
            {
                Subscriptions.TryRemove(key, out _);
            }
        }
    }

    private sealed class Subscription : IDisposable
    {
        private bool isDisposed;

        public Subscription(
            InProcessProjectionNotifier owner,
            string key,
            Action<ProjectionUpdatedEvent> callback
        )
        {
            Owner = owner;
            Key = key;
            Callback = callback;
        }

        private Action<ProjectionUpdatedEvent> Callback { get; }

        private string Key { get; }

        private InProcessProjectionNotifier Owner { get; }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            Owner.RemoveSubscription(Key, this);
        }

        public void Invoke(
            ProjectionUpdatedEvent args
        )
        {
            if (!isDisposed)
            {
                Callback(args);
            }
        }
    }

    private sealed class SubscriptionCollection
    {
        private readonly object syncRoot = new();

        public SubscriptionCollection(
            string projectionType,
            string entityId,
            ILogger logger
        )
        {
            ProjectionType = projectionType;
            EntityId = entityId;
            Logger = logger;
        }

        public int Count
        {
            get
            {
                lock (syncRoot)
                {
                    return Items.Count;
                }
            }
        }

        public bool IsEmpty
        {
            get
            {
                lock (syncRoot)
                {
                    return Items.Count == 0;
                }
            }
        }

        private string EntityId { get; }

        private List<Subscription> Items { get; } = [];

        private ILogger Logger { get; }

        private string ProjectionType { get; }

        public void Add(
            Subscription subscription
        )
        {
            lock (syncRoot)
            {
                Items.Add(subscription);
            }
        }

        public void NotifyAll(
            ProjectionUpdatedEvent args
        )
        {
            List<Subscription> snapshot;
            lock (syncRoot)
            {
                snapshot = [.. Items];
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
                    InProcessProjectionNotifierLoggerExtensions.CallbackFailed(Logger, ProjectionType, EntityId, ex);
                }
            }
        }

        public void Remove(
            Subscription subscription
        )
        {
            lock (syncRoot)
            {
                Items.Remove(subscription);
            }
        }
    }
}