using System;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Moq;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.Aggregates.L0Tests;

/// <summary>
///     Tests for <see cref="FireAndForgetEffectWorkerGrain{TEvent, TAggregate}" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates")]
[AllureSubSuite("Fire-and-Forget Effects")]
public sealed class FireAndForgetEffectWorkerGrainTests
{
    private static ServiceProvider BuildServiceProvider(
        IFireAndForgetEventEffect<TestEvent, TestAggregate> effect
    )
    {
        ServiceCollection services = new();
        services.AddSingleton(effect);
        services.AddSingleton(effect.GetType(), effect);
        services.AddSingleton<IFireAndForgetEventEffect<TestEvent, TestAggregate>>(effect);
        return services.BuildServiceProvider();
    }

    private static FireAndForgetEffectWorkerGrain<TestEvent, TestAggregate> CreateGrain(
        IServiceProvider provider
    )
    {
        Mock<IGrainContext> grainContextMock = new();
        Mock<ILogger<FireAndForgetEffectWorkerGrain<TestEvent, TestAggregate>>> loggerMock = new();
        return new(grainContextMock.Object, provider, loggerMock.Object);
    }

    /// <summary>
    ///     Test aggregate state used for fire-and-forget effect tests.
    /// </summary>
    /// <param name="Value">Aggregate state value.</param>
    internal sealed record TestAggregate(string Value);

    /// <summary>
    ///     Test event used for fire-and-forget effect tests.
    /// </summary>
    /// <param name="Value">Event value.</param>
    internal sealed record TestEvent(string Value);

    /// <summary>
    ///     Effect that throws to validate resilience behavior.
    /// </summary>
    internal sealed class ThrowingEffect : IFireAndForgetEventEffect<TestEvent, TestAggregate>
    {
        /// <summary>
        ///     Throws to simulate an effect failure.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        /// <param name="aggregateState">The aggregate state.</param>
        /// <param name="brookKey">The brook key.</param>
        /// <param name="eventPosition">The event position.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that always faults.</returns>
        public Task HandleAsync(
            TestEvent eventData,
            TestAggregate aggregateState,
            string brookKey,
            long eventPosition,
            CancellationToken cancellationToken
        ) =>
            throw new InvalidOperationException("Boom");
    }

    /// <summary>
    ///     Effect that records invocation details for assertions.
    /// </summary>
    internal sealed class TrackingEffect : IFireAndForgetEventEffect<TestEvent, TestAggregate>
    {
        /// <summary>
        ///     Gets the number of times the effect was invoked.
        /// </summary>
        public int CallCount { get; private set; }

        /// <summary>
        ///     Gets the last aggregate state passed to the effect.
        /// </summary>
        public TestAggregate? LastAggregate { get; private set; }

        /// <summary>
        ///     Gets the last brook key passed to the effect.
        /// </summary>
        public string? LastBrookKey { get; private set; }

        /// <summary>
        ///     Gets the last event passed to the effect.
        /// </summary>
        public TestEvent? LastEvent { get; private set; }

        /// <summary>
        ///     Gets the last event position passed to the effect.
        /// </summary>
        public long LastEventPosition { get; private set; }

        /// <summary>
        ///     Records the event and aggregate state for assertions.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        /// <param name="aggregateState">The aggregate state.</param>
        /// <param name="brookKey">The brook key.</param>
        /// <param name="eventPosition">The event position.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A completed task.</returns>
        public Task HandleAsync(
            TestEvent eventData,
            TestAggregate aggregateState,
            string brookKey,
            long eventPosition,
            CancellationToken cancellationToken
        )
        {
            CallCount++;
            LastEvent = eventData;
            LastAggregate = aggregateState;
            LastBrookKey = brookKey;
            LastEventPosition = eventPosition;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Constructor should throw when grain context is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void ConstructorThrowsWhenGrainContextIsNull()
    {
        Mock<IServiceProvider> serviceProviderMock = new();
        Mock<ILogger<FireAndForgetEffectWorkerGrain<TestEvent, TestAggregate>>> loggerMock = new();
        Assert.Throws<ArgumentNullException>(() => new FireAndForgetEffectWorkerGrain<TestEvent, TestAggregate>(
            null!,
            serviceProviderMock.Object,
            loggerMock.Object));
    }

    /// <summary>
    ///     ExecuteAsync should swallow exceptions from effects that throw.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    [AllureFeature("Resilience")]
    public async Task ExecuteAsyncDoesNotThrowWhenEffectThrows()
    {
        using ServiceProvider provider = BuildServiceProvider(new ThrowingEffect());
        FireAndForgetEffectWorkerGrain<TestEvent, TestAggregate> grain = CreateGrain(provider);
        FireAndForgetEffectEnvelope<TestEvent, TestAggregate> envelope = new()
        {
            EventData = new("evt-2"),
            AggregateState = new("state-3"),
            BrookKey = "brook-3",
            EventPosition = 9,
            EffectTypeName = typeof(ThrowingEffect).FullName ?? typeof(ThrowingEffect).Name,
        };
        await grain.ExecuteAsync(envelope, CancellationToken.None);
        Assert.True(true);
    }

    /// <summary>
    ///     ExecuteAsync should invoke the matching effect when envelope is valid.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    [AllureFeature("Dispatch")]
    public async Task ExecuteAsyncInvokesEffectWhenEnvelopeIsValid()
    {
        TrackingEffect effect = new();
        using ServiceProvider provider = BuildServiceProvider(effect);
        FireAndForgetEffectWorkerGrain<TestEvent, TestAggregate> grain = CreateGrain(provider);
        TestEvent eventData = new("evt-1");
        TestAggregate aggregateState = new("state-1");
        FireAndForgetEffectEnvelope<TestEvent, TestAggregate> envelope = new()
        {
            EventData = eventData,
            AggregateState = aggregateState,
            BrookKey = "brook-1",
            EventPosition = 5,
            EffectTypeName = typeof(TrackingEffect).FullName ?? typeof(TrackingEffect).Name,
        };
        await grain.ExecuteAsync(envelope, CancellationToken.None);
        Assert.Equal(1, effect.CallCount);
        Assert.Same(eventData, effect.LastEvent);
        Assert.Same(aggregateState, effect.LastAggregate);
    }

    /// <summary>
    ///     ExecuteAsync should not invoke the effect when the envelope is missing event data.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    [AllureFeature("Validation")]
    public async Task ExecuteAsyncSkipsEffectWhenEventDataIsMissing()
    {
        TrackingEffect effect = new();
        using ServiceProvider provider = BuildServiceProvider(effect);
        FireAndForgetEffectWorkerGrain<TestEvent, TestAggregate> grain = CreateGrain(provider);
        FireAndForgetEffectEnvelope<TestEvent, TestAggregate> envelope = new()
        {
            EventData = null,
            AggregateState = new("state-2"),
            BrookKey = "brook-2",
            EventPosition = 7,
            EffectTypeName = typeof(TrackingEffect).FullName ?? typeof(TrackingEffect).Name,
        };
        await grain.ExecuteAsync(envelope, CancellationToken.None);
        Assert.Equal(0, effect.CallCount);
    }
}