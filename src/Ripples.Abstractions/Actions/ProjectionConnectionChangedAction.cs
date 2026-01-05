using System;


namespace Mississippi.Ripples.Abstractions.Actions;

/// <summary>
///     Action dispatched when a projection's connection state changes.
/// </summary>
/// <typeparam name="T">The projection type used for type-safe dispatch.</typeparam>
public sealed record ProjectionConnectionChangedAction<T> : IRippleAction
    where T : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionConnectionChangedAction{T}" /> class.
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="isConnected">Whether the projection is connected.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    public ProjectionConnectionChangedAction(
        string entityId,
        bool isConnected
    )
    {
        ArgumentNullException.ThrowIfNull(entityId);
        EntityId = entityId;
        IsConnected = isConnected;
    }

    /// <summary>
    ///     Gets the entity identifier.
    /// </summary>
    public string EntityId { get; }

    /// <summary>
    ///     Gets a value indicating whether the projection is connected.
    /// </summary>
    public bool IsConnected { get; }

    /// <summary>
    ///     Gets the projection type for reducer dispatch.
    /// </summary>
    public Type ProjectionType => typeof(T);
}