using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;


namespace Mississippi.Testing.Utilities.SignalR;

/// <summary>
///     Minimal <see cref="ConnectionContext" /> implementation for testing.
/// </summary>
/// <remarks>
///     <para>
///         This implementation provides the minimum required functionality to
///         construct a HubConnectionContext. It creates in-memory
///         pipes for transport simulation.
///     </para>
/// </remarks>
[SuppressMessage(
    "IDisposableAnalyzers.Correctness",
    "IDISP001:Dispose created",
    Justification = "Test helper pipes are short-lived and do not need disposal in tests")]
internal sealed class TestConnectionContext
    : ConnectionContext,
      IDisposable
{
    private readonly IDuplexPipe transport;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TestConnectionContext" /> class.
    /// </summary>
    /// <param name="connectionId">The connection identifier.</param>
    public TestConnectionContext(
        string connectionId
    )
    {
        ConnectionId = connectionId;
        Pipe applicationPipe = new();
        Pipe transportPipe = new();
        transport = new TestDuplexPipe(transportPipe.Reader, applicationPipe.Writer);
        Items = new Dictionary<object, object?>();
    }

    /// <inheritdoc />
    public override string ConnectionId { get; set; }

    /// <inheritdoc />
    public override IFeatureCollection Features { get; } = new FeatureCollection();

    /// <inheritdoc />
    public override IDictionary<object, object?> Items { get; set; }

    /// <inheritdoc />
    public override IDuplexPipe Transport
    {
        get => transport;
        set => throw new NotSupportedException();
    }

    /// <summary>
    ///     Disposes resources used by the connection context.
    /// </summary>
    public void Dispose()
    {
        // Clean up pipes if needed
    }
}