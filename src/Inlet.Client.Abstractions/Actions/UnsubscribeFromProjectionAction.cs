using System;


namespace Mississippi.Inlet.Client.Abstractions.Actions;

/// <summary>
///     Action to unsubscribe from a projection for a specific entity.
/// </summary>
/// <typeparam name="T">The projection type.</typeparam>
public sealed record UnsubscribeFromProjectionAction<T> : IInletAction
    where T : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UnsubscribeFromProjectionAction{T}" /> class.
    /// </summary>
    /// <param name="entityId">The entity identifier to unsubscribe from.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    public UnsubscribeFromProjectionAction(
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