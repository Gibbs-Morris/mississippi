using System;


namespace Mississippi.Inlet.Abstractions.Actions;

/// <summary>
///     Action dispatched when the connection state of a projection changes.
/// </summary>
/// <typeparam name="T">The projection type.</typeparam>
public sealed record ProjectionConnectionChangedAction<T> : IInletAction
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
    ///     Gets the projection type.
    /// </summary>
    public Type ProjectionType => typeof(T);
}