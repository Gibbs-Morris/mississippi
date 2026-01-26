using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Aggregates.Diagnostics;

using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     Stateless worker grain that executes fire-and-forget event effects.
/// </summary>
/// <typeparam name="TEvent">The event type.</typeparam>
/// <typeparam name="TAggregate">The aggregate state type.</typeparam>
/// <remarks>
///     <para>
///         This grain resolves effects via DI and executes them. It provides Orleans
///         single-threaded guarantees but is otherwise just infrastructure for running
///         the effect business logic.
///     </para>
///     <para>
///         The <see cref="StatelessWorkerAttribute" /> enables horizontal scaling since
///         effects are stateless and can run on any silo.
///     </para>
/// </remarks>
[StatelessWorker]
internal sealed class FireAndForgetEffectWorkerGrain<TEvent, TAggregate>
    : IFireAndForgetEffectWorkerGrain<TEvent, TAggregate>,
      IGrainBase
    where TEvent : class
    where TAggregate : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FireAndForgetEffectWorkerGrain{TEvent, TAggregate}" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="serviceProvider">The service provider for resolving effects.</param>
    /// <param name="logger">Logger instance.</param>
    public FireAndForgetEffectWorkerGrain(
        IGrainContext grainContext,
        IServiceProvider serviceProvider,
        ILogger<FireAndForgetEffectWorkerGrain<TEvent, TAggregate>> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private ILogger<FireAndForgetEffectWorkerGrain<TEvent, TAggregate>> Logger { get; }

    private IServiceProvider ServiceProvider { get; }

    /// <summary>
    ///     Determines if an exception is critical and should not be swallowed.
    /// </summary>
    private static bool IsCriticalException(
        Exception ex
    ) =>
        ex is OutOfMemoryException or StackOverflowException or ThreadInterruptedException;

    /// <inheritdoc />
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Fire-and-forget effects must not propagate exceptions to the caller")]
    public async Task ExecuteAsync(
        FireAndForgetEffectEnvelope<TEvent, TAggregate> envelope,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(envelope);
        string effectTypeName = envelope.EffectTypeName;
        string aggregateTypeName = typeof(TAggregate).Name;
        string eventTypeName = typeof(TEvent).Name;
        Logger.ExecutingFireAndForgetEffect(effectTypeName, eventTypeName, envelope.BrookKey);
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            IFireAndForgetEventEffect<TEvent, TAggregate>? effect = ResolveEffect(envelope.EffectTypeName);
            if (effect is null)
            {
                sw.Stop();
                Logger.EffectNotFound(effectTypeName, aggregateTypeName);
                FireAndForgetEffectMetrics.RecordFailure(
                    aggregateTypeName,
                    eventTypeName,
                    effectTypeName,
                    "effect_not_found");
                return;
            }

            if (envelope.EventData is null)
            {
                sw.Stop();
                Logger.EffectEnvelopeMissingEventData(effectTypeName, envelope.BrookKey);
                FireAndForgetEffectMetrics.RecordFailure(
                    aggregateTypeName,
                    eventTypeName,
                    effectTypeName,
                    "missing_event_data");
                return;
            }

            if (envelope.AggregateState is null)
            {
                sw.Stop();
                Logger.EffectEnvelopeMissingAggregateState(effectTypeName, envelope.BrookKey);
                FireAndForgetEffectMetrics.RecordFailure(
                    aggregateTypeName,
                    eventTypeName,
                    effectTypeName,
                    "missing_aggregate_state");
                return;
            }

            await effect.HandleAsync(
                envelope.EventData,
                envelope.AggregateState,
                envelope.BrookKey,
                envelope.EventPosition,
                cancellationToken);
            sw.Stop();
            FireAndForgetEffectMetrics.RecordSuccess(
                aggregateTypeName,
                eventTypeName,
                effectTypeName,
                sw.Elapsed.TotalMilliseconds);
            Logger.FireAndForgetEffectCompleted(
                effectTypeName,
                eventTypeName,
                envelope.BrookKey,
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex) when (!IsCriticalException(ex))
        {
            sw.Stop();
            Logger.FireAndForgetEffectFailed(effectTypeName, eventTypeName, envelope.BrookKey, ex);
            FireAndForgetEffectMetrics.RecordFailure(
                aggregateTypeName,
                eventTypeName,
                effectTypeName,
                ex.GetType().Name);
            FireAndForgetEffectMetrics.RecordDuration(
                aggregateTypeName,
                eventTypeName,
                effectTypeName,
                sw.Elapsed.TotalMilliseconds,
                false);
        }
    }

    /// <summary>
    ///     Resolves the specific effect by type name from DI.
    /// </summary>
    private IFireAndForgetEventEffect<TEvent, TAggregate>? ResolveEffect(
        string effectTypeName
    )
    {
        IEnumerable<IFireAndForgetEventEffect<TEvent, TAggregate>> effects =
            ServiceProvider.GetServices<IFireAndForgetEventEffect<TEvent, TAggregate>>();
        return effects.FirstOrDefault(e => e.GetType().FullName == effectTypeName);
    }
}