using System;


namespace Mississippi.Ripples.Abstractions.State;

/// <summary>
///     Represents the loading/connection state of a projection.
/// </summary>
/// <typeparam name="T">The projection data type.</typeparam>
public interface IProjectionState<T>
    where T : class
{
    /// <summary>
    ///     Gets the current projection data.
    /// </summary>
    T? Data { get; }

    /// <summary>
    ///     Gets the entity identifier.
    /// </summary>
    string EntityId { get; }

    /// <summary>
    ///     Gets a value indicating whether connected to real-time updates.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    ///     Gets a value indicating whether the first successful load has completed.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    ///     Gets a value indicating whether data is being fetched.
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    ///     Gets the last error that occurred, if any.
    /// </summary>
    Exception? LastError { get; }

    /// <summary>
    ///     Gets the server version.
    /// </summary>
    long? Version { get; }
}