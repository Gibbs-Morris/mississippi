using System;


namespace MississippiTests.Inlet.Gateway.L0Tests.Authentication;

/// <summary>
///     Identifies an existing host scope and records handler disposal within it.
/// </summary>
internal sealed class ConnectionHandlerScope
{
    /// <summary>
    ///     Gets or sets the number of disposed handlers using this scope.
    /// </summary>
    public int Disposals { get; set; }

    /// <summary>
    ///     Gets the unique host service scope identity.
    /// </summary>
    public string Id { get; } = Guid.NewGuid().ToString("N");
}