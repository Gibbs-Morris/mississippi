using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;


namespace Mississippi.Reservoir.Core.L0Tests;

/// <summary>
///     Tests for <see cref="StoreEventSubject{T}" />.
/// </summary>
public sealed class StoreEventSubjectTests : IDisposable
{
    private readonly StoreEventSubject<int> sut = new();

    /// <inheritdoc />
    public void Dispose()
    {
        sut.Dispose();
    }

    /// <summary>
    ///     A throwing observer should not prevent subsequent observers from receiving OnNext.
    /// </summary>
    [Fact]
    public void ThrowingObserverDoesNotPreventSubsequentObserversFromReceivingOnNext()
    {
        // Arrange
        List<int> firstReceived = [];
        List<int> thirdReceived = [];
        Exception? secondOnError = null;

        using IDisposable first = sut.Subscribe(new DelegateObserver<int>(
            onNext: v => firstReceived.Add(v)));
        using IDisposable second = sut.Subscribe(new DelegateObserver<int>(
            onNext: _ => throw new InvalidOperationException("Observer failure"),
            onError: ex => secondOnError = ex));
        using IDisposable third = sut.Subscribe(new DelegateObserver<int>(
            onNext: v => thirdReceived.Add(v)));

        // Act
        sut.OnNext(42);

        // Assert
        Assert.Equal([42], firstReceived);
        Assert.Equal([42], thirdReceived);
        Assert.IsType<InvalidOperationException>(secondOnError);
    }

    /// <summary>
    ///     When an observer's OnError also throws, remaining observers still receive OnNext.
    /// </summary>
    [Fact]
    public void ObserverOnErrorThrowDoesNotPreventSubsequentObserversFromReceivingOnNext()
    {
        // Arrange
        List<int> secondReceived = [];

        using IDisposable first = sut.Subscribe(new DelegateObserver<int>(
            onNext: _ => throw new InvalidOperationException("OnNext failure"),
            onError: _ => throw new InvalidOperationException("OnError failure")));
        using IDisposable second = sut.Subscribe(new DelegateObserver<int>(
            onNext: v => secondReceived.Add(v)));

        // Act
        sut.OnNext(7);

        // Assert
        Assert.Equal([7], secondReceived);
    }

    /// <summary>
    ///     Disposing the subject completes all observers.
    ///     A dedicated subject instance is used so that Dispose can be called explicitly
    ///     as the test's act step without conflicting with the test fixture teardown.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Subject is disposed as part of the test act")]
    public void DisposeCompletesAllObservers()
    {
        // Arrange
        bool completed = false;
        using StoreEventSubject<int> subject = new();
        using IDisposable subscription = subject.Subscribe(
            new DelegateObserver<int>(onCompleted: () => completed = true));

        // Act
        subject.Dispose();

        // Assert
        Assert.True(completed);
    }

    /// <summary>
    ///     Subscribing to a disposed subject immediately completes the observer.
    /// </summary>
    [Fact]
    public void SubscribeToDisposedSubjectCompletesImmediately()
    {
        // Arrange
        bool completed = false;
        sut.Dispose();

        // Act
        using IDisposable subscription = sut.Subscribe(
            new DelegateObserver<int>(onCompleted: () => completed = true));

        // Assert
        Assert.True(completed);
    }

    /// <summary>
    ///     OnNext on a disposed subject is a no-op.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Explicit Dispose is required to test behavior after disposal")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP016:Don't use disposed instance",
        Justification = "Testing that OnNext is a no-op after disposal requires calling it on the disposed subject")]
    public void OnNextOnDisposedSubjectIsNoOp()
    {
        // Arrange
        bool received = false;
        StoreEventSubject<int> subject = new();
        using IDisposable subscription = subject.Subscribe(new DelegateObserver<int>(onNext: _ => received = true));
        subject.Dispose();

        // Act
        subject.OnNext(1);

        // Assert
        Assert.False(received);
    }

    /// <summary>
    ///     Unsubscribing an observer stops it from receiving future OnNext values.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing explicit Dispose behavior requires non-using pattern")]
    public void UnsubscribedObserverDoesNotReceiveOnNext()
    {
        // Arrange
        int callCount = 0;
        IDisposable subscription = sut.Subscribe(new DelegateObserver<int>(onNext: _ => callCount++));

        // Act
        sut.OnNext(1);
        subscription.Dispose();
        sut.OnNext(2);

        // Assert
        Assert.Equal(1, callCount);
    }

    /// <summary>
    ///     Simple delegate-based IObserver for testing.
    /// </summary>
    private sealed class DelegateObserver<T> : IObserver<T>
    {
        private readonly Action? onCompleted;

        private readonly Action<Exception>? onError;

        private readonly Action<T>? onNext;

        public DelegateObserver(
            Action<T>? onNext = null,
            Action<Exception>? onError = null,
            Action? onCompleted = null
        )
        {
            this.onNext = onNext;
            this.onError = onError;
            this.onCompleted = onCompleted;
        }

        public void OnCompleted() => onCompleted?.Invoke();

        public void OnError(
            Exception error
        ) =>
            onError?.Invoke(error);

        public void OnNext(
            T value
        ) =>
            onNext?.Invoke(value);
    }
}
