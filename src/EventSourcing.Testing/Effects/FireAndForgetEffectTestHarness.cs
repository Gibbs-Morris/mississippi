using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Moq;


namespace Mississippi.EventSourcing.Testing.Effects;

/// <summary>
///     Test harness for testing fire-and-forget event effects.
/// </summary>
/// <typeparam name="TEffect">The effect type under test.</typeparam>
/// <typeparam name="TEvent">The event type the effect handles.</typeparam>
/// <typeparam name="TAggregate">The aggregate state type.</typeparam>
/// <remarks>
///     <para>
///         This harness provides a fluent API for testing fire-and-forget effects that
///         run in worker grains. Unlike synchronous effects, fire-and-forget effects
///         receive the brook key and event position as parameters.
///     </para>
///     <para>
///         The brook key format must be <c>brookName|entityId</c> (e.g., <c>SPRING.BANKING.ACCOUNT|acc-123</c>)
///         to match <c>BrookKey.FromString()</c> expectations.
///     </para>
/// </remarks>
public sealed class FireAndForgetEffectTestHarness<TEffect, TEvent, TAggregate>
    where TEffect : class, IFireAndForgetEventEffect<TEvent, TAggregate>
    where TEvent : class
    where TAggregate : class
{
    private readonly Mock<IAggregateGrainFactory> aggregateGrainFactoryMock = new(MockBehavior.Strict);

    private readonly List<(Type AggregateType, string EntityId, object Command)> dispatchedCommands = [];

    private string brookKey = "TEST.DOMAIN.ENTITY|test-entity";

    private long eventPosition = 1;

    private FireAndForgetEffectTestHarness()
    {
    }

    /// <summary>
    ///     Gets the configured brook key for this test.
    /// </summary>
    public string BrookKey => brookKey;

    /// <summary>
    ///     Gets the list of commands dispatched to aggregate grains during effect execution.
    /// </summary>
    public IReadOnlyList<(Type AggregateType, string EntityId, object Command)> DispatchedCommands =>
        dispatchedCommands;

    /// <summary>
    ///     Gets the configured event position for this test.
    /// </summary>
    public long EventPosition => eventPosition;

    /// <summary>
    ///     Gets the logger mock for verifying log calls.
    /// </summary>
    public Mock<ILogger<TEffect>> LoggerMock { get; } = new();

    /// <summary>
    ///     Creates a new instance of the fire-and-forget effect test harness.
    /// </summary>
    /// <returns>A new harness instance.</returns>
#pragma warning disable CA1000 // Do not declare static members on generic types - Factory pattern is idiomatic
    public static FireAndForgetEffectTestHarness<TEffect, TEvent, TAggregate> Create() => new();
#pragma warning restore CA1000

    /// <summary>
    ///     Builds the effect using the provided factory function with aggregate grain factory.
    /// </summary>
    /// <param name="factory">Factory function that creates the effect with mocked dependencies.</param>
    /// <returns>The constructed effect.</returns>
    public TEffect Build(
        Func<IAggregateGrainFactory, ILogger<TEffect>, TEffect> factory
    )
    {
        ArgumentNullException.ThrowIfNull(factory);
        return factory(aggregateGrainFactoryMock.Object, LoggerMock.Object);
    }

    /// <summary>
    ///     Builds the effect using the provided factory function with only logger.
    /// </summary>
    /// <param name="factory">Factory function that creates the effect with logger.</param>
    /// <returns>The constructed effect.</returns>
    public TEffect Build(
        Func<ILogger<TEffect>, TEffect> factory
    )
    {
        ArgumentNullException.ThrowIfNull(factory);
        return factory(LoggerMock.Object);
    }

    /// <summary>
    ///     Invokes the fire-and-forget effect with the configured brook key and event position.
    /// </summary>
    /// <param name="effect">The effect to invoke.</param>
    /// <param name="eventData">The event data.</param>
    /// <param name="aggregateState">The current aggregate state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(
        TEffect effect,
        TEvent eventData,
        TAggregate aggregateState,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(effect);
        await effect.HandleAsync(eventData, aggregateState, brookKey, eventPosition, cancellationToken);
    }

    /// <summary>
    ///     Configures a mock aggregate grain to return a specific response when a command is executed.
    /// </summary>
    /// <typeparam name="TTargetAggregate">The target aggregate type.</typeparam>
    /// <param name="entityId">The entity ID the effect will call.</param>
    /// <param name="response">The response to return.</param>
    /// <returns>The harness for chaining.</returns>
    public FireAndForgetEffectTestHarness<TEffect, TEvent, TAggregate> WithAggregateGrainResponse<TTargetAggregate>(
        string entityId,
        OperationResult response
    )
        where TTargetAggregate : class
    {
        Mock<IGenericAggregateGrain<TTargetAggregate>> grainMock = new();
        grainMock.Setup(g => g.ExecuteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((
                cmd,
                _
            ) => dispatchedCommands.Add((typeof(TTargetAggregate), entityId, cmd)))
            .ReturnsAsync(response);
        aggregateGrainFactoryMock.Setup(f => f.GetGenericAggregate<TTargetAggregate>(entityId))
            .Returns(grainMock.Object);
        return this;
    }

    /// <summary>
    ///     Configures the brook key for this test.
    /// </summary>
    /// <param name="key">The brook key (e.g., "VENDOR:AREA:ENTITY:entity-id").</param>
    /// <returns>The harness for chaining.</returns>
    public FireAndForgetEffectTestHarness<TEffect, TEvent, TAggregate> WithBrookKey(
        string key
    )
    {
        brookKey = key;
        return this;
    }

    /// <summary>
    ///     Configures the brook key using entity ID only, with a test-friendly prefix.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <returns>The harness for chaining.</returns>
    public FireAndForgetEffectTestHarness<TEffect, TEvent, TAggregate> WithEntityId(
        string entityId
    )
    {
        brookKey = $"TEST.DOMAIN.ENTITY|{entityId}";
        return this;
    }

    /// <summary>
    ///     Configures the event position for this test.
    /// </summary>
    /// <param name="position">The event position in the brook.</param>
    /// <returns>The harness for chaining.</returns>
    public FireAndForgetEffectTestHarness<TEffect, TEvent, TAggregate> WithEventPosition(
        long position
    )
    {
        eventPosition = position;
        return this;
    }
}
