using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.EventSourcing.Reducers.Abstractions;

/// <summary>
///     Defines the contract for a root reducer that applies an ordered stream of events to a model.
/// </summary>
/// <typeparam name="TModel">The type of the model being reduced.</typeparam>
public interface IRootReducer<TModel>
{
    /// <summary>
    ///     Applies a single event to the model synchronously.
    /// </summary>
    /// <param name="model">The current model state before applying the event.</param>
    /// <param name="event">The event to apply to the model.</param>
    /// <returns>A new model instance with the event applied.</returns>
    /// <remarks>
    ///     The original model is not mutated. A new instance is returned.
    /// </remarks>
    TModel Reduce(
        TModel model,
        object @event
    );

    /// <summary>
    ///     Applies a sequence of events to the model synchronously.
    /// </summary>
    /// <param name="model">The initial model state.</param>
    /// <param name="events">The ordered sequence of events to apply.</param>
    /// <returns>A new model instance with all events applied.</returns>
    /// <remarks>
    ///     Events are applied in order. The original model is not mutated.
    /// </remarks>
    TModel Reduce(
        TModel model,
        IEnumerable<object> events
    );

    /// <summary>
    ///     Applies a single event to the model asynchronously.
    /// </summary>
    /// <param name="model">The current model state before applying the event.</param>
    /// <param name="event">The event to apply to the model.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation with the new model instance.</returns>
    /// <remarks>
    ///     The original model is not mutated. A new instance is returned.
    /// </remarks>
    Task<TModel> ReduceAsync(
        TModel model,
        object @event,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Applies a sequence of events to the model asynchronously.
    /// </summary>
    /// <param name="model">The initial model state.</param>
    /// <param name="events">The ordered sequence of events to apply.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation with the new model instance.</returns>
    /// <remarks>
    ///     Events are applied in order. The original model is not mutated.
    /// </remarks>
    Task<TModel> ReduceAsync(
        TModel model,
        IEnumerable<object> events,
        CancellationToken cancellationToken = default
    );
}
