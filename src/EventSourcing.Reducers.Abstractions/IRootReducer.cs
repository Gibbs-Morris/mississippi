using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.EventSourcing.Reducers.Abstractions;

/// <summary>
///     Provides methods for applying ordered event streams to a model instance.
/// </summary>
/// <typeparam name="TModel">The type of model produced by the reducers.</typeparam>
public interface IRootReducer<TModel>
    where TModel : class
{
    /// <summary>
    ///     Applies the provided events to the model using synchronous enumeration.
    /// </summary>
    /// <param name="input">The immutable model snapshot that serves as the starting point.</param>
    /// <param name="events">The ordered event stream to replay.</param>
    /// <returns>The new model snapshot after all events have been applied.</returns>
    TModel Reduce(
        TModel input,
        IEnumerable<object> events
    );

    /// <summary>
    ///     Applies the provided events to the model using asynchronous enumeration.
    /// </summary>
    /// <param name="input">The immutable model snapshot that serves as the starting point.</param>
    /// <param name="events">The ordered asynchronous event stream to replay.</param>
    /// <param name="cancellationToken">The token that signals when event replay should stop.</param>
    /// <returns>A task-like value that completes with the new model snapshot.</returns>
    ValueTask<TModel> ReduceAsync(
        TModel input,
        IAsyncEnumerable<object> events,
        CancellationToken cancellationToken = default
    );
}