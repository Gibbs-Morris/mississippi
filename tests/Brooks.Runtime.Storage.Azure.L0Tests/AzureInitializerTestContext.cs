using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;

using Azure.Core.Pipeline;
using Azure.Storage;
using Azure.Storage.Blobs;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace Mississippi.Brooks.Runtime.Storage.Azure.L0Tests;

/// <summary>
///     Owns a deterministic Brooks Azure initializer, its service provider, and the in-memory HTTP transport used by tests.
/// </summary>
internal sealed class AzureInitializerTestContext : IDisposable
{
    private readonly ServiceProvider serviceProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AzureInitializerTestContext" /> class.
    /// </summary>
    /// <param name="responder">The callback that produces Azure HTTP responses for each request.</param>
    /// <param name="configureOptions">The Brooks Azure options configuration for the initializer under test.</param>
    internal AzureInitializerTestContext(
        Func<HttpRequestMessage, HttpResponseMessage> responder,
        Action<BrookStorageOptions> configureOptions
    )
    {
        ArgumentNullException.ThrowIfNull(responder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        Handler = new AzureTestHttpMessageHandler(responder);

        BlobClientOptions blobClientOptions = new()
        {
            Transport = new HttpClientTransport(Handler),
        };
        blobClientOptions.Retry.MaxRetries = 0;

        BlobServiceClient blobServiceClient = new(
            new Uri("https://testaccount.blob.core.windows.net/"),
            new StorageSharedKeyCredential("testaccount", Convert.ToBase64String(new byte[32])),
            blobClientOptions);

        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton("shared-account", (_, _) => blobServiceClient);
        services.AddAzureBrookStorageProvider(configureOptions);

        serviceProvider = services.BuildServiceProvider();
        Initializer = serviceProvider.GetServices<IHostedService>().OfType<AzureBrookStorageInitializer>().Single();
    }

    /// <summary>
    ///     Gets the request recorder and response producer backing the Azure test transport.
    /// </summary>
    internal AzureTestHttpMessageHandler Handler { get; }

    /// <summary>
    ///     Gets the Brooks Azure initializer under test.
    /// </summary>
    internal AzureBrookStorageInitializer Initializer { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        serviceProvider.Dispose();
        Handler.Dispose();
    }

    /// <summary>
    ///     Creates a minimal successful Azure Blob HTTP response for the fake transport.
    /// </summary>
    /// <param name="statusCode">The status code to return.</param>
    /// <returns>The response message.</returns>
    internal static HttpResponseMessage CreateResponse(
        HttpStatusCode statusCode
    )
    {
        HttpResponseMessage response = new(statusCode);
        response.Headers.Add("x-ms-request-id", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        response.Headers.Add("x-ms-version", "2025-11-05");
        response.Headers.Date = DateTimeOffset.UtcNow;
        response.Content = new ByteArrayContent([]);
        response.Content.Headers.ContentLength = 0;
        return response;
    }
}
