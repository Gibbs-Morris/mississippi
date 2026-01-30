using System;


namespace Mississippi.Inlet.Client.Abstractions.State;

/// <summary>
///     Represents a single projection entry in the feature state.
/// </summary>
/// <typeparam name="T">The projection data type.</typeparam>
/// <param name="Data">The projection data, if loaded.</param>
/// <param name="Version">The server version of the projection.</param>
/// <param name="IsLoading">Whether the projection is currently loading.</param>
/// <param name="IsConnected">Whether the projection is connected to the server.</param>
/// <param name="Error">Any error that occurred.</param>
public sealed record ProjectionEntry<T>(T? Data, long Version, bool IsLoading, bool IsConnected, Exception? Error)
    where T : class
{
    /// <summary>
    ///     An empty entry representing a projection that has not been loaded.
    /// </summary>
    public static readonly ProjectionEntry<T> Empty = new(null, -1, false, false, null);
}