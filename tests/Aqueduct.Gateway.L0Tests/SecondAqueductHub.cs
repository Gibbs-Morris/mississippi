using Microsoft.AspNetCore.SignalR;


namespace Mississippi.Aqueduct.Gateway.L0Tests;

/// <summary>
///     Provides a distinct hub identity for production registration and routing isolation tests.
/// </summary>
internal sealed class SecondAqueductHub : Hub;