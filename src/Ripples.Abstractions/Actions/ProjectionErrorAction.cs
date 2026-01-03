using System;


namespace Mississippi.Ripples.Abstractions;

/// <summary>
///     Action dispatched when a projection load fails.
/// </summary>
/// <typeparam name="T">The projection type used for type-safe dispatch.</typeparam>
public sealed record ProjectionErrorAction<T> : IRippleAction
    where T : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionErrorAction{T}" /> class.
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="error">The error that occurred.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public ProjectionErrorAction(
        string entityId,
        Exception error
    )
    {
        ArgumentNullException.ThrowIfNull(entityId);
        ArgumentNullException.ThrowIfNull(error);
        EntityId = entityId;
        Error = error;
    }

    /// <summary>
    ///     Gets the entity identifier.
    /// </summary>
    public string EntityId { get; }

    /// <summary>
    ///     Gets the error that occurred.
    /// </summary>
    public Exception Error { get; }

    /// <summary>
    ///     Gets the projection type for reducer dispatch.
    /// </summary>
    public Type ProjectionType => typeof(T);
}