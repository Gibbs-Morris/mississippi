using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;


namespace MississippiTests.Inlet.Client.L0Tests.ActionEffects.Commands;

/// <summary>
///     Handles command requests in memory without opening a network connection.
/// </summary>
/// <param name="send">The response behavior for this test.</param>
internal sealed class CommandEffectHttpHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send
) : HttpMessageHandler
{
    private Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> SendResponse { get; } = send;

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    ) =>
        SendResponse(request, cancellationToken);
}