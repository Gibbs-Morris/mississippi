namespace Mississippi.Ripples.Server;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Mississippi.Ripples.Abstractions;

/// <summary>
/// In-process implementation of <see cref="IProjectionUpdateNotifier"/> for server-side use.
/// </summary>
/// <remarks>
/// <para>
/// This notifier maintains subscriptions in memory and dispatches notifications
/// synchronously when projections change. It is designed for Blazor Server scenarios
/// where components run on the server and can react immediately to grain state changes.
/// </para>
/// <para>
/// In production, this notifier should be connected to an Orleans stream or grain
/// observer to receive real-time updates when projection grains change state.
/// </para>
/// </remarks>
public sealed class InProcessProjectionUpdateNotifier : IProjectionUpdateNotifier
{
    private readonly ConcurrentDictionary<string, SubscriptionCollection> subscriptions = new();

    /// <inheritdoc/>
    public IDisposable Subscribe(
        string projectionType,
        string entityId,
        Action<ProjectionUpdatedEvent> callback)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectionType);
        ArgumentException.ThrowIfNullOrEmpty(entityId);
        ArgumentNullException.ThrowIfNull(callback);

        string key = CreateKey(projectionType, entityId);
        SubscriptionCollection collection = subscriptions.GetOrAdd(key, _ => new SubscriptionCollection());

        Subscription subscription = new(this, key, callback);
        collection.Add(subscription);

        return subscription;
    }

    /// <summary>
    /// Notifies all subscribed handlers that a projection has been updated.
    /// </summary>
    /// <param name="projectionType">The type of projection that changed.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="newVersion">The new version number.</param>
    public void NotifyProjectionChanged(string projectionType, string entityId, long newVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectionType);
        ArgumentException.ThrowIfNullOrEmpty(entityId);

        string key = CreateKey(projectionType, entityId);

        if (subscriptions.TryGetValue(key, out SubscriptionCollection? collection))
        {
            ProjectionUpdatedEvent args = new(projectionType, entityId, newVersion);
            collection.NotifyAll(args);
        }
    }

    private static string CreateKey(string projectionType, string entityId) =>
        $"{projectionType}:{entityId}";

    private void RemoveSubscription(string key, Subscription subscription)
    {
        if (subscriptions.TryGetValue(key, out SubscriptionCollection? collection))
        {
            collection.Remove(subscription);

            if (collection.IsEmpty)
            {
                subscriptions.TryRemove(key, out _);
            }
        }
    }

    private sealed class SubscriptionCollection
    {
        private readonly List<Subscription> items = [];
        private readonly object syncRoot = new();

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

        public void Add(Subscription subscription)
        {
            lock (syncRoot)
            {
                items.Add(subscription);
            }
        }

        public void Remove(Subscription subscription)
        {
            lock (syncRoot)
            {
                items.Remove(subscription);
            }
        }

        public void NotifyAll(ProjectionUpdatedEvent args)
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
                    // Handler exceptions should not propagate - log to debugger
                    Debug.WriteLine($"Projection update callback failed: {ex}");
                }
            }
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly InProcessProjectionUpdateNotifier owner;
        private readonly string key;
        private readonly Action<ProjectionUpdatedEvent> callback;
        private bool isDisposed;

        public Subscription(
            InProcessProjectionUpdateNotifier owner,
            string key,
            Action<ProjectionUpdatedEvent> callback)
        {
            this.owner = owner;
            this.key = key;
            this.callback = callback;
        }

        public void Invoke(ProjectionUpdatedEvent args)
        {
            if (!isDisposed)
            {
                callback(args);
            }
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
    }
}
