using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.Extensions.Logging;


namespace Mississippi.Projections.Reducers;

/// <summary>
///     Default implementation of <see cref="IRootReducer{TModel}" /> that runs each registered reducer sequentially.
/// </summary>
/// <typeparam name="TModel">The projection model type.</typeparam>
public sealed class RootReducer<TModel> : IRootReducer<TModel>
    where TModel : notnull, new()
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RootReducer{TModel}" /> class.
    /// </summary>
    /// <param name="reducers">The reducers that will be applied to the model.</param>
    /// <param name="logger">The logger used to record reducer diagnostics.</param>
    public RootReducer(
        IEnumerable<IReducer<TModel>> reducers,
        ILogger<RootReducer<TModel>> logger
    )
    {
        ArgumentNullException.ThrowIfNull(reducers);
        ArgumentNullException.ThrowIfNull(logger);
        Reducers = reducers.ToArray();
        Logger = logger;
    }

    private ILogger<RootReducer<TModel>> Logger { get; }

    private IReducer<TModel>[] Reducers { get; }

    /// <inheritdoc />
    public TModel Reduce(
        TModel model,
        object domainEvent
    )
    {
        TModel currentModel = model ?? new TModel();
        string modelTypeName = currentModel.GetType().FullName ?? typeof(TModel).FullName ?? typeof(TModel).Name;
        string domainEventTypeName = domainEvent?.GetType().FullName ?? "null";
        if (domainEvent is null)
        {
            Logger.ReducerReceivedNullEvent(modelTypeName, Reducers.Length);
            return currentModel;
        }

        int reducerCount = Reducers.Length;
        if (reducerCount == 0)
        {
            Logger.NoReducersRegistered(modelTypeName, domainEventTypeName);
            return currentModel;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        Logger.ReducerExecutionStarting(modelTypeName, domainEventTypeName, reducerCount);
        foreach (IReducer<TModel> reducer in Reducers)
        {
            string reducerTypeName = reducer.GetType().FullName ?? reducer.GetType().Name;
            try
            {
                Logger.ReducerInvocationStarting(modelTypeName, domainEventTypeName, reducerTypeName);
                currentModel = reducer.Reduce(currentModel, domainEvent);
                Logger.ReducerInvocationCompleted(modelTypeName, domainEventTypeName, reducerTypeName);
            }
            catch (Exception exception)
            {
                Logger.ReducerInvocationFailed(modelTypeName, domainEventTypeName, reducerTypeName, exception);
                throw;
            }
        }

        stopwatch.Stop();
        Logger.ReducerExecutionCompleted(
            modelTypeName,
            domainEventTypeName,
            reducerCount,
            stopwatch.ElapsedMilliseconds);
        return currentModel;
    }
}