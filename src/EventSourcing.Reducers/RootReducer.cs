using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Mississippi.EventSourcing.Reducers;

/// <summary>
///     Default <see cref="IRootReducer{TModel}" /> implementation that sequentially applies reducers to events.
/// </summary>
/// <typeparam name="TModel">The model type produced by the reducers.</typeparam>
public class RootReducer<TModel> : IRootReducer<TModel>
    where TModel : class
{
    private static readonly EqualityComparer<TModel> ModelComparer = EqualityComparer<TModel>.Default;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RootReducer{TModel}" /> class.
    /// </summary>
    /// <param name="reducers">The reducers to apply for each event.</param>
    public RootReducer(
        IEnumerable<IReducer<TModel>> reducers
    )
    {
        ArgumentNullException.ThrowIfNull(reducers);
        Reducers = reducers as IReadOnlyList<IReducer<TModel>> ?? reducers.ToArray();
    }

    private IReadOnlyList<IReducer<TModel>> Reducers { get; }

    private static int? CaptureReferenceHash(
        TModel model
    )
    {
        if (model is null)
        {
            return null;
        }

        return ModelComparer.GetHashCode(model);
    }

    private static void EnsureImmutableTransition(
        TModel previous,
        TModel current,
        int? hashBefore
    )
    {
        if (!ReferenceEquals(previous, current))
        {
            return;
        }

        if (hashBefore is null || current is null)
        {
            return;
        }

        int hashAfter = ModelComparer.GetHashCode(current);
        if (hashBefore == hashAfter)
        {
            return;
        }

        throw new InvalidOperationException(
            "Reducers must return a new model instance when mutating state. Use immutable updates or records.");
    }

    /// <inheritdoc />
    public TModel Reduce(
        TModel input,
        IEnumerable<object> events
    )
    {
        ArgumentNullException.ThrowIfNull(events);
        foreach (object eventData in events)
        {
            input = ApplyReducers(input, eventData);
        }

        return input;
    }

    /// <inheritdoc />
    public async ValueTask<TModel> ReduceAsync(
        TModel input,
        IAsyncEnumerable<object> events,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(events);
        await foreach (object eventData in events.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            input = ApplyReducers(input, eventData);
        }

        return input;
    }

    private TModel ApplyReducers(
        TModel input,
        object eventData
    )
    {
        foreach (IReducer<TModel> reducer in Reducers)
        {
            TModel previous = input;
            int? stateHash = CaptureReferenceHash(previous);
            TModel updated = reducer.Reduce(previous, eventData);
            EnsureImmutableTransition(previous, updated, stateHash);
            input = updated;
        }

        return input;
    }
}