using System;

using Mississippi.Inlet.Client.Abstractions.State;


namespace Mississippi.Inlet.Client.State;

/// <summary>
///     Represents the state of a server-synced projection.
/// </summary>
/// <typeparam name="T">The projection data type.</typeparam>
public sealed class ProjectionState<T> : IProjectionState<T>
    where T : class
{
    /// <summary>
    ///     A default empty state for a projection that has not been loaded.
    /// </summary>
    public static readonly ProjectionState<T> NotLoaded = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionState{T}" /> class.
    /// </summary>
    public ProjectionState()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionState{T}" /> class with data.
    /// </summary>
    /// <param name="data">The projection data.</param>
    /// <param name="version">The server version.</param>
    /// <param name="isLoading">Whether the projection is loading.</param>
    /// <param name="isConnected">Whether the projection is connected.</param>
    /// <param name="error">Any error that occurred.</param>
    public ProjectionState(
        T? data,
        long version,
        bool isLoading = false,
        bool isConnected = false,
        Exception? error = null
    )
    {
        Data = data;
        Version = version;
        IsLoading = isLoading;
        IsConnected = isConnected;
        ErrorException = error;
    }

    /// <summary>
    ///     Gets the projection data, if loaded.
    /// </summary>
    public T? Data { get; }

    /// <inheritdoc />
    public Exception? ErrorException { get; }

    /// <summary>
    ///     Gets a value indicating whether the projection is connected to the server.
    /// </summary>
    public bool IsConnected { get; }

    /// <summary>
    ///     Gets a value indicating whether the projection is currently loading.
    /// </summary>
    public bool IsLoading { get; }

    /// <summary>
    ///     Gets the server version of the projection.
    /// </summary>
    public long Version { get; } = -1;

    /// <summary>
    ///     Creates a new state with the specified connection state.
    /// </summary>
    /// <param name="isConnected">Whether connected.</param>
    /// <returns>A new projection state with the connection state.</returns>
    public ProjectionState<T> WithConnection(
        bool isConnected
    ) =>
        new(Data, Version, IsLoading, isConnected, ErrorException);

    /// <summary>
    ///     Creates a new state with the specified data.
    /// </summary>
    /// <param name="data">The new data.</param>
    /// <param name="version">The new version.</param>
    /// <returns>A new projection state with the data.</returns>
    public ProjectionState<T> WithData(
        T? data,
        long version
    ) =>
        new(data, version, false, IsConnected);

    /// <summary>
    ///     Creates a new state with the specified error.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>A new projection state with the error.</returns>
    public ProjectionState<T> WithError(
        Exception error
    ) =>
        new(Data, Version, false, IsConnected, error);

    /// <summary>
    ///     Creates a new state with loading set to true.
    /// </summary>
    /// <returns>A new projection state in loading state.</returns>
    public ProjectionState<T> WithLoading() => new(Data, Version, true, IsConnected, ErrorException);
}
