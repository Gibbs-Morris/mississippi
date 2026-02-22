using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Moq;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.Testing.Effects;

/// <summary>
///     Test harness for testing event effects that dispatch commands to other aggregates.
/// </summary>
/// <typeparam name="TEffect">The effect type under test.</typeparam>
/// <typeparam name="TEvent">The event type the effect handles.</typeparam>
/// <typeparam name="TAggregate">The aggregate state type.</typeparam>
/// <remarks>
///     <para>
///         This harness provides a fluent API for testing effects that dispatch commands
///         to other aggregates via <see cref="IAggregateGrainFactory" />. It captures
///         dispatched commands for verification.
///     </para>
///     <para>
///         The harness generates a brook key in the format <c>brookName|entityId</c> from the
///         configured grain key. By default, the brook name is "TEST.DOMAIN.AGGREGATE" but can
///         be customized using <see cref="WithBrookName" />.
///     </para>
/// </remarks>
public sealed class EffectTestHarness<TEffect, TEvent, TAggregate>
    where TEffect : class
    where TEvent : class
    where TAggregate : class
{
    private readonly Mock<IAggregateGrainFactory> aggregateGrainFactoryMock = new(MockBehavior.Strict);

    private readonly List<(Type AggregateType, string EntityId, object Command)> dispatchedCommands = [];

    private readonly Mock<IGrainContext> grainContextMock = new();

    private string brookName = "TEST.DOMAIN.AGGREGATE";

    private long eventPosition = 1;

    private string grainKey = "test-entity";

    private EffectTestHarness()
    {
    }

    /// <summary>
    ///     Gets the list of commands dispatched to aggregate grains during effect execution.
    /// </summary>
    public IReadOnlyList<(Type AggregateType, string EntityId, object Command)> DispatchedCommands =>
        dispatchedCommands;

    /// <summary>
    ///     Gets the logger mock for verifying log calls.
    /// </summary>
    public Mock<ILogger<TEffect>> LoggerMock { get; } = new();

    /// <summary>
    ///     Creates a new instance of the effect test harness.
    /// </summary>
    /// <returns>A new harness instance.</returns>
#pragma warning disable CA1000 // Do not declare static members on generic types - Factory pattern is idiomatic
    public static EffectTestHarness<TEffect, TEvent, TAggregate> Create() => new();
#pragma warning restore CA1000

    /// <summary>
    ///     Builds the effect using the provided factory function.
    /// </summary>
    /// <param name="factory">Factory function that creates the effect with mocked dependencies.</param>
    /// <returns>The constructed effect.</returns>
    public TEffect Build(
        Func<IAggregateGrainFactory, IGrainContext, ILogger<TEffect>, TEffect> factory
    )
    {
        ArgumentNullException.ThrowIfNull(factory);
        SetupGrainContext();
        return factory(aggregateGrainFactoryMock.Object, grainContextMock.Object, LoggerMock.Object);
    }

    /// <summary>
    ///     Invokes the effect by calling HandleAsync via reflection to access protected methods.
    /// </summary>
    /// <param name="effect">The effect to invoke.</param>
    /// <param name="eventData">The event data.</param>
    /// <param name="currentState">The current aggregate state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Any yielded objects from the effect.</returns>
    public async Task<IReadOnlyList<object>> InvokeAsync(
        TEffect effect,
        TEvent eventData,
        TAggregate currentState,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(effect);

        // Generate brook key in the format brookName|entityId
        string brookKey = $"{brookName}|{grainKey}";

        // Access the protected or public HandleSimpleAsync or HandleAsync method via IEventEffect interface
        Type effectType = effect.GetType();

        // Try HandleSimpleAsync first (for SimpleEventEffectBase) - 5-parameter signature
        MethodInfo? handleSimpleMethod = effectType.GetMethod(
            "HandleSimpleAsync",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            [typeof(TEvent), typeof(TAggregate), typeof(string), typeof(long), typeof(CancellationToken)]);
        if (handleSimpleMethod != null)
        {
            Task? task = handleSimpleMethod.Invoke(
                effect,
                [eventData, currentState, brookKey, eventPosition, cancellationToken]) as Task;
            if (task != null)
            {
                await task;
            }

            return [];
        }

        // Try HandleAsync for EventEffectBase - 5-parameter signature
        MethodInfo? handleAsyncMethod = effectType.GetMethod(
            "HandleAsync",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            [typeof(TEvent), typeof(TAggregate), typeof(string), typeof(long), typeof(CancellationToken)]);
        if (handleAsyncMethod != null)
        {
            object? result = handleAsyncMethod.Invoke(
                effect,
                [eventData, currentState, brookKey, eventPosition, cancellationToken]);
            if (result is IAsyncEnumerable<object> asyncEnumerable)
            {
                List<object> yieldedObjects = [];
                await foreach (object item in asyncEnumerable.WithCancellation(cancellationToken))
                {
                    yieldedObjects.Add(item);
                }

                return yieldedObjects;
            }
        }

        throw new InvalidOperationException(
            $"Could not find HandleSimpleAsync or HandleAsync method on {effectType.Name}");
    }

    /// <summary>
    ///     Creates an <see cref="EffectTestResult" /> from the dispatched commands for fluent assertions.
    /// </summary>
    /// <returns>A result object for fluent assertions.</returns>
    public EffectTestResult ToResult() => new(dispatchedCommands);

    /// <summary>
    ///     Configures a mock aggregate grain to return a specific response when a command is executed.
    /// </summary>
    /// <typeparam name="TTargetAggregate">The target aggregate type.</typeparam>
    /// <param name="entityId">The entity ID the effect will call.</param>
    /// <param name="response">The response to return.</param>
    /// <returns>The harness for chaining.</returns>
    public EffectTestHarness<TEffect, TEvent, TAggregate> WithAggregateGrainResponse<TTargetAggregate>(
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
    ///     Configures the brook name used when constructing the brook key.
    /// </summary>
    /// <param name="name">The brook name (e.g., "SPRING.BANKING.ACCOUNT").</param>
    /// <returns>The harness for chaining.</returns>
    /// <remarks>
    ///     The brook key is constructed as <c>brookName|entityId</c> where entityId comes from
    ///     <see cref="WithGrainKey" />. The default brook name is "TEST.DOMAIN.AGGREGATE".
    /// </remarks>
    public EffectTestHarness<TEffect, TEvent, TAggregate> WithBrookName(
        string name
    )
    {
        brookName = name;
        return this;
    }

    /// <summary>
    ///     Configures the event position passed to the effect.
    /// </summary>
    /// <param name="position">The event position in the brook.</param>
    /// <returns>The harness for chaining.</returns>
    public EffectTestHarness<TEffect, TEvent, TAggregate> WithEventPosition(
        long position
    )
    {
        eventPosition = position;
        return this;
    }

    /// <summary>
    ///     Configures the grain context to return the specified grain key.
    /// </summary>
    /// <param name="key">The grain key (entity ID portion of the brook key).</param>
    /// <returns>The harness for chaining.</returns>
    public EffectTestHarness<TEffect, TEvent, TAggregate> WithGrainKey(
        string key
    )
    {
        grainKey = key;
        return this;
    }

    private void SetupGrainContext()
    {
        GrainId grainId = GrainId.Create("test", grainKey);
        grainContextMock.Setup(c => c.GrainId).Returns(grainId);
    }
}