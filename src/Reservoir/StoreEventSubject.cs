using System;
using System.Collections.Generic;


namespace Mississippi.Reservoir;

/// <summary>
///     A simple observable subject that allows publishing values to multiple subscribers.
/// </summary>
/// <typeparam name="T">The type of values to observe.</typeparam>
/// <remarks>
///     <para>
///         This is a lightweight implementation of the observer pattern without requiring
///         System.Reactive. It supports multiple subscribers and thread-safe subscription management.
///     </para>
/// </remarks>
internal sealed class StoreEventSubject<T> : IObservable<T>, IDisposable
{
    private readonly object subscribersLock = new();

    private bool disposed;

    private List<IObserver<T>> subscribers = [];

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        List<IObserver<T>> snapshot;
        lock (subscribersLock)
        {
            snapshot = subscribers;
            subscribers = [];
        }

        foreach (IObserver<T> observer in snapshot)
        {
            observer.OnCompleted();
        }
    }

    /// <summary>
    ///     Publishes a value to all subscribers.
    /// </summary>
    /// <param name="value">The value to publish.</param>
    public void OnNext(
        T value
    )
    {
        if (disposed)
        {
            return;
        }

        List<IObserver<T>> snapshot;
        lock (subscribersLock)
        {
            snapshot = [.. subscribers];
        }

        foreach (IObserver<T> observer in snapshot)
        {
            observer.OnNext(value);
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe(
        IObserver<T> observer
    )
    {
        ArgumentNullException.ThrowIfNull(observer);
        if (disposed)
        {
            observer.OnCompleted();
            return new EmptyDisposable();
        }

        lock (subscribersLock)
        {
            subscribers = [.. subscribers, observer];
        }

        return new Unsubscriber(this, observer);
    }

    private void Unsubscribe(
        IObserver<T> observer
    )
    {
        lock (subscribersLock)
        {
            List<IObserver<T>> newList = [.. subscribers];
            newList.Remove(observer);
            subscribers = newList;
        }
    }

    /// <summary>
    ///     Empty disposable for when the subject is already disposed.
    /// </summary>
    private sealed class EmptyDisposable : IDisposable
    {
        public void Dispose()
        {
            // Nothing to do.
        }
    }

    /// <summary>
    ///     Disposable that removes the observer from the subject.
    /// </summary>
    private sealed class Unsubscriber : IDisposable
    {
        private IObserver<T>? observer;

        private StoreEventSubject<T>? subject;

        public Unsubscriber(
            StoreEventSubject<T> subject,
            IObserver<T> observer
        )
        {
            this.subject = subject;
            this.observer = observer;
        }

        public void Dispose()
        {
            subject?.Unsubscribe(observer!);
            subject = null;
            observer = null;
        }
    }
}
