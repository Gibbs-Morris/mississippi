using System;


namespace Mississippi.Inlet.Abstractions.State;

/// <summary>
///     Represents the state of a server-synced projection.
/// </summary>
/// <typeparam name="T">The projection data type.</typeparam>
public interface IProjectionState<out T>
    where T : class
{
    /// <summary>
    ///     Gets the projection data, if loaded.
    /// </summary>
    T? Data { get; }

    /// <summary>
    ///     Gets the last error that occurred, if any.
    /// </summary>
    Exception? ErrorException { get; }

    /// <summary>
    ///     Gets a value indicating whether the projection is connected to the server.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    ///     Gets a value indicating whether the projection is currently loading.
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    ///     Gets the server version of the projection.
    /// </summary>
    long Version { get; }
}