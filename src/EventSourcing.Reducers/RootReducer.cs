using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Mississippi.EventSourcing.Reducers;

/// <summary>
///     Root-level reducer that composes one or more <see cref="IReducer{TProjection}" /> instances.
/// </summary>
/// <typeparam name="TProjection">The projection state type.</typeparam>
public sealed class RootReducer<TProjection> : IRootReducer<TProjection>
{
    private readonly string reducerHash;

    private readonly IReducer<TProjection>[] reducers;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RootReducer{TProjection}" /> class.
    /// </summary>
    /// <param name="reducers">The reducers that can update the projection state.</param>
    /// <param name="logger">The logger used for reducer diagnostics.</param>
    public RootReducer(
        IEnumerable<IReducer<TProjection>> reducers,
        ILogger<RootReducer<TProjection>>? logger = null
    )
    {
        ArgumentNullException.ThrowIfNull(reducers);
        this.reducers = reducers.ToArray();
        Logger = logger ?? NullLogger<RootReducer<TProjection>>.Instance;
        reducerHash = ComputeReducerHash(this.reducers);
    }

    private ILogger<RootReducer<TProjection>> Logger { get; }

    private static string ComputeReducerHash(
        IReadOnlyList<IReducer<TProjection>> reducers
    )
    {
        string[] typeNames = reducers.Select(x => x.GetType().FullName ?? x.GetType().Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        string input = string.Join("|", typeNames);
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    /// <inheritdoc />
    public string GetReducerHash() => reducerHash;

    /// <inheritdoc />
    public TProjection Reduce(
        TProjection state,
        object eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        string projectionType = typeof(TProjection).Name;
        string eventType = eventData.GetType().Name;
        Logger.RootReducerReducing(projectionType, eventType);
        for (int i = 0; i < reducers.Length; i++)
        {
            IReducer<TProjection> reducer = reducers[i];
            if (reducer.TryReduce(state, eventData, out TProjection projection))
            {
                if (!typeof(TProjection).IsValueType && ReferenceEquals(state, projection))
                {
                    Logger.RootReducerProjectionInstanceReused(reducer.GetType().Name, projectionType, eventType);
                    throw new InvalidOperationException(
                        "Reducers must return a new projection instance. Use a copy/with expression instead of mutating state.");
                }

                Logger.RootReducerReducerMatched(reducer.GetType().Name, eventType);
                return projection;
            }
        }

        Logger.RootReducerNoReducerMatched(projectionType, eventType);
        return state;
    }
}