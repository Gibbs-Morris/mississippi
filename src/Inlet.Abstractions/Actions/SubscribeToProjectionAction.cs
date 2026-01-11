using System;


namespace Mississippi.Inlet.Abstractions.Actions;

/// <summary>
///     Action to subscribe to a projection for a specific entity.
/// </summary>
/// <typeparam name="T">The projection type.</typeparam>
public sealed record SubscribeToProjectionAction<T> : IInletAction
    where T : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SubscribeToProjectionAction{T}" /> class.
    /// </summary>
    /// <param name="entityId">The entity identifier to subscribe to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    public SubscribeToProjectionAction(
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