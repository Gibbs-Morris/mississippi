using Microsoft.AspNetCore.SignalR;


namespace MississippiTests.Inlet.Client.L1Tests.ActionEffects;

/// <summary>
///     Provides a real hub handshake for the startup controls.
/// </summary>
internal sealed class StartupStatusHub : Hub
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="StartupStatusHub" /> class for SignalR activation.
    /// </summary>
    public StartupStatusHub()
    {
    }
}