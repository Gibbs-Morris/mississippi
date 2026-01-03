using System;


namespace Mississippi.Ripples.Abstractions;

/// <summary>
///     Action dispatched when a projection has loaded successfully.
/// </summary>
/// <typeparam name="T">The projection type.</typeparam>
public sealed record ProjectionLoadedAction<T> : IRippleAction
    where T : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionLoadedAction{T}" /> class.
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="data">The loaded data.</param>
    /// <param name="version">The server version.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    public ProjectionLoadedAction(
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
    ///     Gets the loaded projection data.
    /// </summary>
    public T? Data { get; }

    /// <summary>
    ///     Gets the entity identifier.
    /// </summary>
    public string EntityId { get; }

    /// <summary>
    ///     Gets the server version.
    /// </summary>
    public long Version { get; }
}