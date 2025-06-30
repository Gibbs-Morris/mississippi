using System.Collections.Immutable;
using System.Reactive.Subjects;

using Microsoft.Extensions.Logging;

using Mississippi.Core.Abstractions.Cqrs.Query;

using Orleans.Streams;


namespace Mississippi.Client;

public class MirroredProjectionState : IMirroredProjectionState
{
    private static readonly Action<ILogger, QueryReference, Exception?> SubscriptionNotFound =
        LoggerMessage.Define<QueryReference>(
            LogLevel.Warning,
            new(1001, nameof(UnsubscribeAsync)),
            "No subscription found for key: {Key}");

    private static readonly Action<ILogger, QueryReference, Exception?> UnsubscribeError =
        LoggerMessage.Define<QueryReference>(
            LogLevel.Error,
            new(1002, nameof(UnsubscribeAsync)),
            "Error unsubscribing from stream for key: {Key}");

    public MirroredProjectionState(
        IClusterClient client,
        ILoggerFactory loggerFactory
    )
    {
        LoggerFactory = loggerFactory;
        Logger = loggerFactory.CreateLogger<MirroredProjectionState>();
        ClusterClient = client;
        VersionSubject.Subscribe(OnNext);
    }

    private Subject<VersionedQueryReference> VersionSubject { get; } = new();

    private IClusterClient ClusterClient { get; }

    private ILoggerFactory LoggerFactory { get; }

    private ILogger<MirroredProjectionState> Logger { get; }

    public IObservable<VersionedQueryReference> StatedChanged => VersionSubject;

    private ImmutableDictionary<QueryReference, object> State { get; set; } =
        ImmutableDictionary<QueryReference, object>.Empty;

    private ImmutableDictionary<QueryReference, long> Version { get; set; } =
        ImmutableDictionary<QueryReference, long>.Empty;

    private ImmutableDictionary<QueryReference, StreamSubscriptionHandle<VersionedQueryReference>>
        Subscriptions { get; set; } =
        ImmutableDictionary<QueryReference, StreamSubscriptionHandle<VersionedQueryReference>>.Empty;

    public async Task SubscribeAsync(
        QueryReference key
    )
    {
        if (Subscriptions.ContainsKey(key))
        {
            Logger.LogWarning("Already subscribed for key: {Key}", key);
            return;
        }

        ILogger<OrleansStreamObserver<VersionedQueryReference>> observerLogger =
            LoggerFactory.CreateLogger<OrleansStreamObserver<VersionedQueryReference>>();
        OrleansStreamObserver<VersionedQueryReference> streamObserver = new(VersionSubject, observerLogger);
        IAsyncStream<VersionedQueryReference>? stream = ClusterClient.GetStreamProvider("realtime")
            .GetStream<VersionedQueryReference>(StreamId.Create("ProjectionUpdate", key.Path));
        StreamSubscriptionHandle<VersionedQueryReference> r = await stream.SubscribeAsync(streamObserver)
            .ConfigureAwait(false);
        Subscriptions = Subscriptions.Add(key, r);
    }

    public async Task UnsubscribeAsync(
        QueryReference key
    )
    {
        if (!Subscriptions.TryGetValue(key, out StreamSubscriptionHandle<VersionedQueryReference>? handle))
        {
            SubscriptionNotFound(Logger, key, null);
            return;
        }

        try
        {
            await handle.UnsubscribeAsync().ConfigureAwait(false);
        }
        catch (OrleansException ex)
        {
            UnsubscribeError(Logger, key, ex);
        }
        finally
        {
            Subscriptions = Subscriptions.Remove(key);
            if (State.ContainsKey(key))
            {
                State = State.Remove(key);
                Version = Version.Remove(key);
            }
        }
    }

    public T? GetState<T>(
        QueryReference key
    )
        where T : class =>
        State[key] as T;

    private void OnNext(
        VersionedQueryReference obj
    )
    {
        State = State.SetItem(obj.ToQueryReference(), null);
    }

    public void SaveProjection<T>(
        QueryReference key,
        T item
    )
        where T : class
    {
        Logger.LogInformation("Saving state for key: {Key}", key);
        State = State.SetItem(key, item);
    }
}