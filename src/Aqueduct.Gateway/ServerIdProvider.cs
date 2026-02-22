using System;

using Mississippi.Aqueduct.Abstractions;


namespace Mississippi.Aqueduct;

/// <summary>
///     Default implementation of <see cref="IServerIdProvider" /> that generates
///     a unique server ID at construction time.
/// </summary>
/// <remarks>
///     <para>
///         This implementation generates a GUID-based server ID when instantiated.
///         When registered as a singleton in DI, all components will share the same
///         server identity throughout the lifetime of the application.
///     </para>
/// </remarks>
internal sealed class ServerIdProvider : IServerIdProvider
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ServerIdProvider" /> class.
    /// </summary>
    public ServerIdProvider() => ServerId = Guid.NewGuid().ToString("N");

    /// <inheritdoc />
    public string ServerId { get; }
}