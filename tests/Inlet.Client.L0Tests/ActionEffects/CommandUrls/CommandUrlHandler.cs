using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Inlet.Client.Abstractions.Commands;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects.CommandUrls;

/// <summary>Captures the submitted URI without network access.</summary>
internal sealed class CommandUrlHandler : HttpMessageHandler
{
    /// <summary>Gets the number of submitted requests.</summary>
    public int CallCount { get; private set; }

    /// <summary>Gets the URI observed by the transport.</summary>
    public Uri? RequestUri { get; private set; }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount++;
        RequestUri = request.RequestUri;
        return Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new OperationResultDto(true, null, null)),
            });
    }
}