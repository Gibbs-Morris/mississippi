using System;


namespace Mississippi.Inlet.Client.ActionEffects;

/// <summary>
///     Registry that maps projection paths to client-side DTO types.
/// </summary>
/// <remarks>
///     <para>
///         This registry is populated at startup by generated registration code.
///         The <see cref="AutoProjectionFetcher" /> uses this registry to determine
///         which DTO type to deserialize based on the path received in SignalR notifications.
///     </para>
/// </remarks>
public interface IProjectionDtoRegistry
{
    /// <summary>
    ///     Gets the DTO type for the specified path.
    /// </summary>
    /// <param name="path">The projection path (e.g., "chat/channels").</param>
    /// <returns>The DTO type if registered; otherwise, <c>null</c>.</returns>
    Type? GetDtoType(
        string path
    );

    /// <summary>
    ///     Gets the path for the specified DTO type.
    /// </summary>
    /// <param name="dtoType">The DTO type.</param>
    /// <returns>The projection path if registered; otherwise, <c>null</c>.</returns>
    string? GetPath(
        Type dtoType
    );

    /// <summary>
    ///     Registers a DTO type for the specified path.
    /// </summary>
    /// <param name="path">The projection path.</param>
    /// <param name="dtoType">The DTO type.</param>
    void Register(
        string path,
        Type dtoType
    );

    /// <summary>
    ///     Tries to get the DTO type for the specified path.
    /// </summary>
    /// <param name="path">The projection path.</param>
    /// <param name="dtoType">When this method returns, contains the DTO type if found.</param>
    /// <returns><c>true</c> if the path was found; otherwise, <c>false</c>.</returns>
    bool TryGetDtoType(
        string path,
        out Type? dtoType
    );

    /// <summary>
    ///     Tries to get the path for the specified DTO type.
    /// </summary>
    /// <param name="dtoType">The DTO type.</param>
    /// <param name="path">When this method returns, contains the path if found.</param>
    /// <returns><c>true</c> if the DTO type was found; otherwise, <c>false</c>.</returns>
    bool TryGetPath(
        Type dtoType,
        out string? path
    );
}