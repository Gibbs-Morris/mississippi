using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Runtime.Diagnostics;

using Moq;


namespace Mississippi.DomainModeling.Runtime.L0Tests.EventEffects;

/// <summary>
///     Public xUnit tests for per-effect failure isolation and enumerator ownership.
/// </summary>
public sealed class RootEventEffectLifecycleTests : IDisposable
{
    private readonly ConcurrentQueue<(long Value, KeyValuePair<string, object?>[] Tags)> errors = new();

    private readonly MeterListener listener = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="RootEventEffectLifecycleTests" /> class.
    /// </summary>
    public RootEventEffectLifecycleTests()
    {
        Logger.Setup(logger => logger.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if ((instrument.Meter.Name == EventEffectMetrics.MeterName) &&
                (instrument.Name == "effect.execution.errors"))
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if ((tag.Key == "aggregate.type") && Equals(tag.Value, nameof(EffectLifecycleAggregate)))
                {
                    errors.Enqueue((value, tags.ToArray()));
                    break;
                }
            }
        });
        listener.Start();
    }

    /// <inheritdoc />
    public void Dispose() => listener.Dispose();

    /// <summary>
    ///     Gets all configured critical types at every supported boundary.
    /// </summary>
    public static TheoryData<int, Type, bool> CriticalCases
    {
        get
        {
            TheoryData<int, Type, bool> cases = new();
            Type[] criticalTypes =
                [typeof(OutOfMemoryException), typeof(StackOverflowException), typeof(ThreadInterruptedException)];
            foreach (EffectLifecyclePoint point in Enum.GetValues<EffectLifecyclePoint>())
            {
                if (point == EffectLifecyclePoint.None)
                {
                    continue;
                }

                foreach (Type type in criticalTypes)
                {
                    cases.Add((int)point, type, false);
                    if (point is EffectLifecyclePoint.MoveNext or EffectLifecyclePoint.Dispose)
                    {
                        cases.Add((int)point, type, true);
                    }
                }
            }

            return cases;
        }
    }

    /// <summary>
    ///     Gets every lifecycle boundary and both ValueTask failure modes.
    /// </summary>
    public static TheoryData<int, bool> FailureCases
    {
        get
        {
            TheoryData<int, bool> cases = new();
            foreach (EffectLifecyclePoint point in Enum.GetValues<EffectLifecyclePoint>())
            {
                if (point == EffectLifecyclePoint.None)
                {
                    continue;
                }

                cases.Add((int)point, false);
                if (point is EffectLifecyclePoint.MoveNext or EffectLifecyclePoint.Dispose)
                {
                    cases.Add((int)point, true);
                }
            }

            return cases;
        }
    }

    private Mock<ILogger<RootEventEffect<EffectLifecycleAggregate>>> Logger { get; } = new();

    /// <summary>
    ///     Verifies retained items, later-effect ordering and acquired enumerator ownership.
    /// </summary>
    /// <param name="point">The lifecycle boundary selected to fail.</param>
    /// <param name="effect">The effect whose lifecycle is interrupted.</param>
    /// <param name="following">The effect that should run afterward.</param>
    /// <param name="results">The items collected from the dispatch stream.</param>
    /// <param name="token">The cancellation token expected by both effects.</param>
    private static void AssertContinuation(
        EffectLifecyclePoint point,
        LifecycleEffect effect,
        LifecycleEffect following,
        List<object> results,
        CancellationToken token
    )
    {
        List<object> expected = [];
        if (point is EffectLifecyclePoint.MoveNext or EffectLifecyclePoint.Current or EffectLifecyclePoint.Dispose)
        {
            expected.Add(effect.FirstEvent);
        }

        if (point == EffectLifecyclePoint.Dispose)
        {
            expected.Add(effect.SecondEvent);
        }

        expected.AddRange([following.FirstEvent, following.SecondEvent]);
        Assert.Equal(expected, results);
        AssertOwnership(point, effect, token);
        AssertOwnership(EffectLifecyclePoint.None, following, token);
    }

    /// <summary>
    ///     Verifies lifecycle call counts, cancellation-token forwarding and disposal ownership.
    /// </summary>
    /// <param name="point">The lifecycle boundary selected to fail.</param>
    /// <param name="effect">The effect recording its lifecycle operations.</param>
    /// <param name="token">The cancellation token expected by invocation and acquisition.</param>
    private static void AssertOwnership(
        EffectLifecyclePoint point,
        LifecycleEffect effect,
        CancellationToken token
    )
    {
        Assert.Equal(1, effect.HandleCount);
        Assert.Equal(token, effect.HandleToken);
        Assert.Equal(point == EffectLifecyclePoint.Handle ? 0 : 1, effect.AcquisitionCount);
        Assert.Equal(
            point is EffectLifecyclePoint.Handle or EffectLifecyclePoint.Acquire ? 0 : 1,
            effect.DisposalCount);
        if (point != EffectLifecyclePoint.Handle)
        {
            Assert.Equal(token, effect.EnumeratorToken);
        }
    }

    /// <summary>
    ///     Checks structured log state for the expected effect, event and aggregate type tags.
    /// </summary>
    /// <param name="state">The structured state supplied to the logger.</param>
    /// <param name="effectType">The expected effect type name.</param>
    /// <returns>Whether all three type tags match the expected context.</returns>
    private static bool HasContext(
        object? state,
        string effectType
    ) =>
        state is IEnumerable<KeyValuePair<string, object?>> tags &&
        tags.Any(tag => (tag.Key == "EffectType") && Equals(tag.Value, effectType)) &&
        tags.Any(tag => (tag.Key == "EventType") && Equals(tag.Value, nameof(EffectLifecycleEvent))) &&
        tags.Any(tag => (tag.Key == "AggregateType") && Equals(tag.Value, nameof(EffectLifecycleAggregate)));

    /// <summary>
    ///     Registers the typed effect directly or through an unindexed forwarding mock.
    /// </summary>
    /// <param name="effect">The effect whose lifecycle operations are exercised.</param>
    /// <param name="fallback">Whether to use the unindexed fallback registration.</param>
    /// <returns>The indexed effect or its forwarding fallback registration.</returns>
    private static IEventEffect<EffectLifecycleAggregate> Register(
        LifecycleEffect effect,
        bool fallback
    )
    {
        if (!fallback)
        {
            return effect;
        }

        Mock<IEventEffect<EffectLifecycleAggregate>> registration = new();
        registration.Setup(value => value.CanHandle(It.IsAny<object>())).Returns(true);
        registration
            .Setup(value => value.HandleAsync(
                It.IsAny<object>(),
                It.IsAny<EffectLifecycleAggregate>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                object data, EffectLifecycleAggregate state, string key, long position, CancellationToken token
            ) => effect.HandleAsync(data, state, key, position, token));
        return registration.Object;
    }

    /// <summary>
    ///     Verifies ordinary-failure log context, exception identity and tagged error metric counts.
    /// </summary>
    /// <param name="effect">The registration whose type appears in the diagnostics.</param>
    /// <param name="failures">The ordinary exceptions expected exactly once in the error logs.</param>
    private void AssertFailures(
        IEventEffect<EffectLifecycleAggregate> effect,
        params Exception[] failures
    )
    {
        Logger.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.Is<EventId>(id => id.Id == 107),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(failures.Length));
        foreach (Exception failure in failures)
        {
            Logger.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.Is<EventId>(id => id.Id == 107),
                    It.Is<It.IsAnyType>((state, _) => HasContext(state, effect.GetType().Name)),
                    It.Is<Exception?>(exception => ReferenceEquals(exception, failure)),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        (long Value, KeyValuePair<string, object?>[] Tags)[] observed = errors.ToArray();
        Assert.Equal(failures.Length, observed.Length);
        Assert.All(
            observed,
            error =>
            {
                Assert.Equal(1L, error.Value);
                Assert.Contains(
                    error.Tags,
                    tag => (tag.Key == "aggregate.type") && Equals(tag.Value, nameof(EffectLifecycleAggregate)));
                Assert.Contains(
                    error.Tags,
                    tag => (tag.Key == "effect.type") && Equals(tag.Value, effect.GetType().Name));
                Assert.Contains(
                    error.Tags,
                    tag => (tag.Key == "event.type") && Equals(tag.Value, nameof(EffectLifecycleEvent)));
            });
    }

    /// <summary>
    ///     Exercises an ordinary lifecycle failure and verifies continuation, ownership and diagnostics.
    /// </summary>
    /// <param name="point">The lifecycle boundary selected to fail.</param>
    /// <param name="asynchronousFault">Whether supported async operations return a faulted value task.</param>
    /// <param name="fallback">Whether to register effects through the unindexed fallback.</param>
    /// <returns>The asynchronous verification operation.</returns>
    private async Task AssertOrdinaryContinuationAsync(
        EffectLifecyclePoint point,
        bool asynchronousFault,
        bool fallback
    )
    {
        InvalidOperationException failure = new("ordinary lifecycle failure");
        LifecycleEffect effect = new()
        {
            FailurePoint = point,
            Failure = failure,
            AsynchronousFault = asynchronousFault,
        };
        LifecycleEffect following = new();
        IEventEffect<EffectLifecycleAggregate> registration = Register(effect, fallback);
        List<object> results = await ConsumeAsync(
            [registration, Register(following, fallback)],
            TestContext.Current.CancellationToken);
        AssertContinuation(point, effect, following, results, TestContext.Current.CancellationToken);
        AssertFailures(registration, failure);
    }

    /// <summary>
    ///     Dispatches one lifecycle-test event and collects its streamed items in order.
    /// </summary>
    /// <param name="effects">The effects registered in dispatch order.</param>
    /// <param name="token">The cancellation token forwarded to the dispatcher.</param>
    /// <returns>The items yielded by the registered effects, in streaming order.</returns>
    private async Task<List<object>> ConsumeAsync(
        IEventEffect<EffectLifecycleAggregate>[] effects,
        CancellationToken token
    )
    {
        RootEventEffect<EffectLifecycleAggregate> sut = new(effects, Logger.Object);
        List<object> results = [];
        await foreach (object item in sut.DispatchAsync(new EffectLifecycleEvent(), new(), "lifecycle", 7L, token))
        {
            results.Add(item);
        }

        return results;
    }

    /// <summary>
    ///     Cancellation ends only the affected effect without ordinary-error signals.
    /// </summary>
    /// <param name="failurePoint">The lifecycle boundary that is canceled.</param>
    /// <param name="asynchronousFault">Whether a failed ValueTask carries cancellation.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [MemberData(nameof(FailureCases))]
    public async Task CancellationRemainsQuiet(
        int failurePoint,
        bool asynchronousFault
    )
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        EffectLifecyclePoint point = (EffectLifecyclePoint)failurePoint;
        LifecycleEffect effect = new()
        {
            FailurePoint = point,
            Failure = new OperationCanceledException(cancellation.Token),
            AsynchronousFault = asynchronousFault,
        };
        LifecycleEffect following = new();
        List<object> results = await ConsumeAsync([effect, following], cancellation.Token);
        AssertContinuation(point, effect, following, results, cancellation.Token);
        AssertFailures(effect);
    }

    /// <summary>
    ///     A critical disposal failure still propagates after an ordinary move failure.
    /// </summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task CriticalDisposalFailurePropagates()
    {
        InvalidOperationException ordinary = new("move failure");
        ThreadInterruptedException critical = new("cleanup failure");
        LifecycleEffect effect = new()
        {
            FailurePoint = EffectLifecyclePoint.MoveNext,
            Failure = ordinary,
            DisposalFailure = critical,
        };
        LifecycleEffect following = new();
        Exception observed = await Assert.ThrowsAsync<ThreadInterruptedException>(() => ConsumeAsync(
            [effect, following],
            TestContext.Current.CancellationToken));
        Assert.Same(critical, observed);
        Assert.Equal(0, following.HandleCount);
        AssertOwnership(EffectLifecyclePoint.MoveNext, effect, TestContext.Current.CancellationToken);
        AssertFailures(effect, ordinary);
    }

    /// <summary>
    ///     Critical failures propagate unchanged without invoking later effects.
    /// </summary>
    /// <param name="failurePoint">The lifecycle boundary that fails.</param>
    /// <param name="exceptionType">The configured critical exception type.</param>
    /// <param name="asynchronousFault">Whether a failed ValueTask carries the exception.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [MemberData(nameof(CriticalCases))]
    public async Task CriticalFailurePropagates(
        int failurePoint,
        Type exceptionType,
        bool asynchronousFault
    )
    {
        EffectLifecyclePoint point = (EffectLifecyclePoint)failurePoint;
        Exception failure = (Exception)Activator.CreateInstance(exceptionType, "critical lifecycle failure")!;
        LifecycleEffect effect = new()
        {
            FailurePoint = point,
            Failure = failure,
            AsynchronousFault = asynchronousFault,
        };
        LifecycleEffect following = new();
        Exception observed = await Assert.ThrowsAsync(
            exceptionType,
            () => ConsumeAsync([effect, following], TestContext.Current.CancellationToken));
        Assert.Same(failure, observed);
        Assert.Equal(0, following.HandleCount);
        AssertOwnership(point, effect, TestContext.Current.CancellationToken);
        AssertFailures(effect);
    }

    /// <summary>
    ///     Ordinary or canceled disposal cannot replace a pending critical failure.
    /// </summary>
    /// <param name="failurePoint">The lifecycle boundary with the primary failure.</param>
    /// <param name="canceledDisposal">Whether disposal is canceled or fails ordinarily.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData((int)EffectLifecyclePoint.MoveNext, false)]
    [InlineData((int)EffectLifecyclePoint.Current, false)]
    [InlineData((int)EffectLifecyclePoint.MoveNext, true)]
    [InlineData((int)EffectLifecyclePoint.Current, true)]
    public async Task DisposalDoesNotMaskCriticalFailure(
        int failurePoint,
        bool canceledDisposal
    )
    {
        EffectLifecyclePoint point = (EffectLifecyclePoint)failurePoint;
        OutOfMemoryException critical = (OutOfMemoryException)Activator.CreateInstance(
            typeof(OutOfMemoryException),
            "primary critical failure")!;
        Exception cleanup = canceledDisposal
            ? new OperationCanceledException()
            : new InvalidOperationException("cleanup failure");
        LifecycleEffect effect = new()
        {
            FailurePoint = point,
            Failure = critical,
            DisposalFailure = cleanup,
            AsynchronousFault = true,
        };
        LifecycleEffect following = new();
        Exception observed = await Assert.ThrowsAsync<OutOfMemoryException>(() => ConsumeAsync(
            [effect, following],
            TestContext.Current.CancellationToken));
        Assert.Same(critical, observed);
        Assert.Equal(0, following.HandleCount);
        AssertOwnership(point, effect, TestContext.Current.CancellationToken);
        AssertFailures(effect, canceledDisposal ? [] : [cleanup]);
    }

    /// <summary>
    ///     Early caller termination disposes once and never invokes later effects.
    /// </summary>
    /// <param name="failingDisposal">Whether disposal fails ordinarily.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EarlyTerminationRetainsEnumeratorOwnership(
        bool failingDisposal
    )
    {
        InvalidOperationException cleanup = new("cleanup failure");
        LifecycleEffect effect = new()
        {
            DisposalFailure = failingDisposal ? cleanup : null,
            AsynchronousFault = true,
        };
        LifecycleEffect following = new();
        RootEventEffect<EffectLifecycleAggregate> sut = new([effect, following], Logger.Object);
        await using (IAsyncEnumerator<object> enumerator =
                     sut.DispatchAsync(
                             new EffectLifecycleEvent(),
                             new(),
                             "lifecycle",
                             7L,
                             TestContext.Current.CancellationToken)
                         .GetAsyncEnumerator(TestContext.Current.CancellationToken))
        {
            Assert.True(await enumerator.MoveNextAsync());
            Assert.Same(effect.FirstEvent, enumerator.Current);
        }

        Assert.Equal(1, effect.MoveNextCount);
        Assert.Equal(1, effect.CurrentReadCount);
        Assert.Equal(0, following.HandleCount);
        AssertOwnership(EffectLifecyclePoint.None, effect, TestContext.Current.CancellationToken);
        AssertFailures(effect, failingDisposal ? [cleanup] : []);
    }

    /// <summary>
    ///     The same isolation applies to fallback effects without a type index.
    /// </summary>
    /// <param name="failurePoint">The lifecycle boundary that fails.</param>
    /// <param name="asynchronousFault">Whether a failed ValueTask carries the exception.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [MemberData(nameof(FailureCases))]
    public Task OrdinaryFailureDoesNotSkipFallbackEffects(
        int failurePoint,
        bool asynchronousFault
    ) =>
        AssertOrdinaryContinuationAsync((EffectLifecyclePoint)failurePoint, asynchronousFault, true);

    /// <summary>
    ///     Ordinary failures preserve prior items and later indexed effects.
    /// </summary>
    /// <param name="failurePoint">The lifecycle boundary that fails.</param>
    /// <param name="asynchronousFault">Whether a failed ValueTask carries the exception.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [MemberData(nameof(FailureCases))]
    public Task OrdinaryFailureDoesNotSkipIndexedEffects(
        int failurePoint,
        bool asynchronousFault
    ) =>
        AssertOrdinaryContinuationAsync((EffectLifecyclePoint)failurePoint, asynchronousFault, false);

    /// <summary>
    ///     Separate move and disposal failures each retain their diagnostics.
    /// </summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task SeparateOrdinaryFailuresAreBothReported()
    {
        InvalidOperationException move = new("move failure");
        InvalidOperationException cleanup = new("cleanup failure");
        LifecycleEffect effect = new()
        {
            FailurePoint = EffectLifecyclePoint.MoveNext,
            Failure = move,
            DisposalFailure = cleanup,
            AsynchronousFault = true,
        };
        LifecycleEffect following = new();
        List<object> results = await ConsumeAsync([effect, following], TestContext.Current.CancellationToken);
        AssertContinuation(
            EffectLifecyclePoint.MoveNext,
            effect,
            following,
            results,
            TestContext.Current.CancellationToken);
        AssertFailures(effect, move, cleanup);
    }

    /// <summary>
    ///     Successful effects preserve registration order, tokens and disposal ownership.
    /// </summary>
    /// <param name="fallback">Whether the effects lack a type index.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SuccessfulStreamingPreservesOrder(
        bool fallback
    )
    {
        using CancellationTokenSource cancellation = new();
        LifecycleEffect effect = new();
        LifecycleEffect following = new();
        IEventEffect<EffectLifecycleAggregate> registration = Register(effect, fallback);
        List<object> results = await ConsumeAsync([registration, Register(following, fallback)], cancellation.Token);
        Assert.Equal<object>(
            [effect.FirstEvent, effect.SecondEvent, following.FirstEvent, following.SecondEvent],
            results);
        AssertOwnership(EffectLifecyclePoint.None, effect, cancellation.Token);
        AssertOwnership(EffectLifecyclePoint.None, following, cancellation.Token);
        Assert.Equal(3, effect.MoveNextCount);
        Assert.Equal(2, effect.CurrentReadCount);
        AssertFailures(registration);
    }
}