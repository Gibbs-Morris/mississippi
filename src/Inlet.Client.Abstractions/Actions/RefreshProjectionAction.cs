using System;


namespace Mississippi.Inlet.Client.Abstractions.Actions;

/// <summary>
///     Action to refresh a projection for a specific entity.
/// </summary>
/// <typeparam name="T">The projection type.</typeparam>
public sealed record RefreshProjectionAction<T> : IInletAction
    where T : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RefreshProjectionAction{T}" /> class.
    /// </summary>
    /// <param name="entityId">The entity identifier to refresh.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    public RefreshProjectionAction(
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