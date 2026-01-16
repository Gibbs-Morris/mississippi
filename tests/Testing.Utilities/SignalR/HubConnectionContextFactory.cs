using System;
using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;


namespace Mississippi.Testing.Utilities.SignalR;

/// <summary>
///     Factory for creating <see cref="HubConnectionContext" /> instances in tests.
/// </summary>
/// <remarks>
///     <para>
///         This factory provides a way to create real <see cref="HubConnectionContext" />
///         instances without mocking, since the type cannot be proxied by NSubstitute
///         or Castle.DynamicProxy due to its constructor requirements.
///     </para>
/// </remarks>
public static class HubConnectionContextFactory
{
    /// <summary>
    ///     Creates a test <see cref="HubConnectionContext" /> with the specified connection ID.
    /// </summary>
    /// <param name="connectionId">The unique identifier for the connection.</param>
    /// <param name="keepAliveInterval">
    ///     Optional keep-alive interval. Defaults to 30 seconds.
    /// </param>
    /// <param name="clientTimeout">
    ///     Optional client timeout. Defaults to 1 minute.
    /// </param>
    /// <returns>A configured <see cref="HubConnectionContext" /> for testing.</returns>
    [SuppressMessage(
        "Microsoft.Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "HubConnectionContext manages its own lifetime; caller disposes via using")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test helper creates context that is disposed by caller via using statement")]
    public static HubConnectionContext Create(
        string connectionId,
        TimeSpan? keepAliveInterval = null,
        TimeSpan? clientTimeout = null
    )
    {
        TestConnectionContext connectionContext = new(connectionId);
        return new(
            connectionContext,
            new()
            {
                KeepAliveInterval = keepAliveInterval ?? TimeSpan.FromSeconds(30),
                ClientTimeoutInterval = clientTimeout ?? TimeSpan.FromMinutes(1),
            },
            NullLoggerFactory.Instance);
    }
}