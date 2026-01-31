using System.Threading;
using System.Threading.Tasks;

using Orleans;
using Orleans.Concurrency;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Grain interface for executing fire-and-forget event effects.
/// </summary>
/// <remarks>
///     <para>
///         This grain is a stateless worker that resolves effects via DI and executes them.
///         It provides Orleans single-threaded guarantees but is otherwise just infrastructure.
///     </para>
///     <para>
///         The grain is keyed by aggregate type name for routing. Using
///         <see cref="StatelessWorkerAttribute" /> on the implementation enables horizontal scaling.
///     </para>
/// </remarks>
/// <typeparam name="TEvent">The event type.</typeparam>
/// <typeparam name="TAggregate">The aggregate state type.</typeparam>
[Alias("Mississippi.EventSourcing.Aggregates.Abstractions.IFireAndForgetEffectWorkerGrain`2")]
public interface IFireAndForgetEffectWorkerGrain<TEvent, TAggregate> : IGrainWithStringKey
    where TEvent : class
    where TAggregate : class
{
    /// <summary>
    ///     Executes a fire-and-forget effect.
    /// </summary>
    /// <param name="envelope">The effect envelope containing event and state data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    ///     This method is marked with <see cref="OneWayAttribute" /> for fire-and-forget semantics.
    ///     The caller does not wait for execution to complete.
    /// </remarks>
    [Alias("ExecuteAsync")]
    [OneWay]
    Task ExecuteAsync(
        FireAndForgetEffectEnvelope<TEvent, TAggregate> envelope,
        CancellationToken cancellationToken = default
    );
}