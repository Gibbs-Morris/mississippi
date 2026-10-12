using System;
using System.Collections.Generic;

using Mississippi.Aqueduct.Runtime.Grains;


namespace Mississippi.Aqueduct.Runtime.L0Tests;

/// <summary>
///     Owns the simultaneously active client grains used by a shared-lookup test.
/// </summary>
internal sealed class SignalRClientLifetimes : IDisposable
{
    private List<SignalRClientGrain> Clients { get; } = [];

    /// <summary>
    ///     Transfers a constructed client's disposal ownership to this scope.
    /// </summary>
    /// <param name="client">The client grain to dispose when the test scope ends.</param>
    public void Add(
        SignalRClientGrain client
    )
    {
        ArgumentNullException.ThrowIfNull(client);
        Clients.Add(client);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (SignalRClientGrain client in Clients)
        {
            client.Dispose();
        }

        Clients.Clear();
    }
}