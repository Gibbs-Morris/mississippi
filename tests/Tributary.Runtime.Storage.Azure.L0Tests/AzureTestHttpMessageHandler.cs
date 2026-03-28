using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MississippiTests.Tributary.Runtime.Storage.Azure.L0Tests
{
    /// <summary>
    ///     Records Azure Blob HTTP requests and returns canned responses for deterministic Tributary Azure tests.
    /// </summary>
    internal sealed class AzureTestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responder;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AzureTestHttpMessageHandler" /> class.
        /// </summary>
        /// <param name="responder">The callback that produces a response for each outbound request.</param>
        internal AzureTestHttpMessageHandler(
            Func<HttpRequestMessage, HttpResponseMessage> responder
        )
        {
            this.responder = responder ?? throw new ArgumentNullException(nameof(responder));
        }

        /// <summary>
        ///     Gets the ordered request log emitted by the fake transport.
        /// </summary>
        internal List<string> Requests { get; } = [];

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            ArgumentNullException.ThrowIfNull(request);

            string query = request.RequestUri?.Query ?? string.Empty;
            Requests.Add($"{request.Method.Method} {request.RequestUri?.AbsolutePath}{query}");
            return Task.FromResult(responder(request));
        }
    }
}
