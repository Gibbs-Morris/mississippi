using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Moq;

using Orleans;


namespace Mississippi.EventSourcing.Aggregates.L0Tests;

/// <summary>
///     Tests for <see cref="FireAndForgetEffectRegistration{TEffect, TEvent, TAggregate}" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates")]
[AllureSubSuite("Fire-and-Forget Effects")]
public sealed class FireAndForgetEffectRegistrationTests
{
    /// <summary>
    ///     Event type that does not match the registration.
    /// </summary>
    /// <param name="Value">Event value.</param>
    internal sealed record OtherEvent(string Value);

    /// <summary>
    ///     Test aggregate state for dispatch verification.
    /// </summary>
    /// <param name="Value">Aggregate state value.</param>
    internal sealed record TestAggregate(string Value);

    /// <summary>
    ///     Effect used for registration dispatch tests.
    /// </summary>
    internal sealed class TestEffect : IFireAndForgetEventEffect<TestEvent, TestAggregate>
    {
        /// <summary>
        ///     Handles the event without side effects for testing.
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
        ) =>
            Task.CompletedTask;
    }

    /// <summary>
    ///     Test event used for registration dispatch verification.
    /// </summary>
    /// <param name="Value">Event value.</param>
    internal sealed record TestEvent(string Value);

    /// <summary>
    ///     Dispatch should invoke the worker grain for matching event types.
    /// </summary>
    [Fact]
    [AllureFeature("Dispatch")]
    public void DispatchInvokesWorkerGrainForMatchingEventType()
    {
        Mock<IGrainFactory> grainFactoryMock = new();
        Mock<IFireAndForgetEffectWorkerGrain<TestEvent, TestAggregate>> workerMock = new();
        workerMock
            .Setup(w => w.ExecuteAsync(It.IsAny<FireAndForgetEffectEnvelope<TestEvent, TestAggregate>>(), default))
            .Returns(Task.CompletedTask);
        grainFactoryMock
            .Setup(g => g.GetGrain<IFireAndForgetEffectWorkerGrain<TestEvent, TestAggregate>>(
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(workerMock.Object);
        FireAndForgetEffectRegistration<TestEffect, TestEvent, TestAggregate> registration = new();
        TestEvent eventData = new("event-1");
        TestAggregate aggregateState = new("state-1");
        string brookKey = "brook-1";
        long position = 12;
        string expectedEffectType = typeof(TestEffect).FullName ?? typeof(TestEffect).Name;
        string expectedGrainKey = typeof(TestAggregate).FullName ?? typeof(TestAggregate).Name;
        registration.Dispatch(grainFactoryMock.Object, eventData, aggregateState, brookKey, position);
        grainFactoryMock.Verify(
            g => g.GetGrain<IFireAndForgetEffectWorkerGrain<TestEvent, TestAggregate>>(
                expectedGrainKey,
                It.IsAny<string?>()),
            Times.Once);
        workerMock.Verify(
            w => w.ExecuteAsync(
                It.Is<FireAndForgetEffectEnvelope<TestEvent, TestAggregate>>(envelope =>
                    (envelope.EventData == eventData) &&
                    (envelope.AggregateState == aggregateState) &&
                    (envelope.BrookKey == brookKey) &&
                    (envelope.EventPosition == position) &&
                    (envelope.EffectTypeName == expectedEffectType)),
                default),
            Times.Once);
    }

    /// <summary>
    ///     Dispatch should not invoke the worker grain when the event type does not match.
    /// </summary>
    [Fact]
    [AllureFeature("Dispatch")]
    public void DispatchSkipsWorkerGrainForNonMatchingEventType()
    {
        Mock<IGrainFactory> grainFactoryMock = new();
        Mock<IFireAndForgetEffectWorkerGrain<TestEvent, TestAggregate>> workerMock = new();
        grainFactoryMock
            .Setup(g => g.GetGrain<IFireAndForgetEffectWorkerGrain<TestEvent, TestAggregate>>(
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(workerMock.Object);
        FireAndForgetEffectRegistration<TestEffect, TestEvent, TestAggregate> registration = new();
        OtherEvent eventData = new("event-2");
        TestAggregate aggregateState = new("state-2");
        registration.Dispatch(grainFactoryMock.Object, eventData, aggregateState, "brook-2", 4);
        grainFactoryMock.Verify(
            g => g.GetGrain<IFireAndForgetEffectWorkerGrain<TestEvent, TestAggregate>>(
                It.IsAny<string>(),
                It.IsAny<string?>()),
            Times.Never);
        workerMock.Verify(
            w => w.ExecuteAsync(It.IsAny<FireAndForgetEffectEnvelope<TestEvent, TestAggregate>>(), default),
            Times.Never);
    }
}