using System;


namespace Mississippi.Ripples.Abstractions.Actions;

/// <summary>
///     Action dispatched when a projection receives an update from the server.
/// </summary>
/// <typeparam name="T">The projection type.</typeparam>
public sealed record ProjectionUpdatedAction<T> : IRippleAction
    where T : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionUpdatedAction{T}" /> class.
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="data">The updated projection data.</param>
    /// <param name="version">The new server version.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    public ProjectionUpdatedAction(
        string entityId,
        T? data,
        long version
    )
    {
        ArgumentNullException.ThrowIfNull(entityId);
        EntityId = entityId;
        Data = data;
        Version = version;
    }

    /// <summary>
    ///     Gets the updated projection data.
    /// </summary>
    public T? Data { get; }

    /// <summary>
    ///     Gets the entity identifier.
    /// </summary>
    public string EntityId { get; }

    /// <summary>
    ///     Gets the new server version.
    /// </summary>
    public long Version { get; }
}