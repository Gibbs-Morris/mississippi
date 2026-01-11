using System;


namespace Mississippi.Inlet.Abstractions.Actions;

/// <summary>
///     Action dispatched when a projection load or update fails.
/// </summary>
/// <typeparam name="T">The projection type.</typeparam>
public sealed record ProjectionErrorAction<T> : IInletAction
    where T : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionErrorAction{T}" /> class.
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="error">The error that occurred.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="entityId" /> or <paramref name="error" /> is null.
    /// </exception>
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
    ///     Gets the projection type.
    /// </summary>
    public Type ProjectionType => typeof(T);
}