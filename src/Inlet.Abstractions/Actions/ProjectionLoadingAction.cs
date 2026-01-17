using System;


namespace Mississippi.Inlet.Abstractions.Actions;

/// <summary>
///     Action dispatched when a projection is being loaded.
/// </summary>
/// <typeparam name="T">The projection type.</typeparam>
public sealed record ProjectionLoadingAction<T> : IInletAction
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
    ///     Gets the projection type.
    /// </summary>
    public Type ProjectionType => typeof(T);
}