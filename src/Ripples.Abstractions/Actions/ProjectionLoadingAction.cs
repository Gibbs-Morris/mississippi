using System;


namespace Mississippi.Ripples.Abstractions.Actions;

/// <summary>
///     Action dispatched when a projection starts loading.
/// </summary>
/// <typeparam name="T">The projection type used for type-safe dispatch.</typeparam>
public sealed record ProjectionLoadingAction<T> : IRippleAction
    where T : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionLoadingAction{T}" /> class.
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    public ProjectionLoadingAction(
        string entityId
    )
    {
        ArgumentNullException.ThrowIfNull(entityId);
        EntityId = entityId;
    }

    /// <summary>
    ///     Gets the entity identifier.
    /// </summary>
    public string EntityId { get; }

    /// <summary>
    ///     Gets the projection type for reducer dispatch.
    /// </summary>
    public Type ProjectionType => typeof(T);
}