using System;

using Mississippi.Ripples.Abstractions;
using Mississippi.Ripples.Abstractions.State;


namespace Mississippi.Ripples;

/// <summary>
///     Immutable state container for a projection entity.
/// </summary>
/// <typeparam name="T">The projection type.</typeparam>
internal sealed record ProjectionState<T> : IProjectionState<T>
    where T : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionState{T}" /> class.
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    public ProjectionState(
        string entityId
    )
    {
        ArgumentNullException.ThrowIfNull(entityId);
        EntityId = entityId;
    }

    /// <inheritdoc />
    public T? Data { get; init; }

    /// <inheritdoc />
    public string EntityId { get; }

    /// <inheritdoc />
    public bool IsConnected { get; init; }

    /// <inheritdoc />
    public bool IsLoaded { get; init; }

    /// <inheritdoc />
    public bool IsLoading { get; init; }

    /// <inheritdoc />
    public Exception? LastError { get; init; }

    /// <inheritdoc />
    public long? Version { get; init; }
}