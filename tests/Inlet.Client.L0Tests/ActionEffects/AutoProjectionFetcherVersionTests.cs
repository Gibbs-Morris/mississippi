using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.L0Tests.Helpers;

using Moq;
using Moq.Protected;


namespace MississippiTests.Inlet.Client.L0Tests.ActionEffects;

/// <summary>
///     Verifies the built-in HTTP fetcher's exact-route fallback and authoritative version headers.
///     This class is public so xUnit can discover its tests.
/// </summary>
public sealed class AutoProjectionFetcherVersionTests
{
    /// <summary>
    ///     Retains the exact request version when no response version can be read.
    /// </summary>
    /// <param name="exactVersion">Whether to request the exact-version endpoint.</param>
    /// <param name="etag">The response's optional ETag.</param>
    /// <param name="expectedVersion">The version associated with the returned data.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(true, null, 10L)]
    [InlineData(true, "\"12\"", 12L)]
    [InlineData(true, "\"0\"", 0L)]
    [InlineData(true, "\"not-a-version\"", 10L)]
    [InlineData(false, null, 0L)]
    [InlineData(false, "\"not-a-version\"", 0L)]
    public async Task FetchKeepsAvailableVersionMetadata(
        bool exactVersion,
        string? etag,
        long expectedVersion
    )
    {
        const string EntityId = "entity-1";
        const string ProjectionPath = "test/projection";
        const long RequestedVersion = 10;
        using HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(
                new TestProjection
                {
                    Name = "returned-data",
                }),
        };
        if (etag is not null)
        {
            response.Headers.ETag = new(etag);
        }

        string? requestedPath = null;
        HttpMethod? requestedMethod = null;
        Mock<HttpMessageHandler> handler = new();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
            {
                requestedPath = request.RequestUri?.AbsolutePath;
                requestedMethod = request.Method;
            })
            .ReturnsAsync(response);
        using HttpClient http = new(handler.Object)
        {
            BaseAddress = new("https://localhost"),
        };
        ProjectionDtoRegistry registry = new();
        registry.Register(ProjectionPath, typeof(TestProjection));
        AutoProjectionFetcher fetcher = new(http, registry);
        ProjectionFetchResult? result = exactVersion
            ? await fetcher.FetchAtVersionAsync(
                typeof(TestProjection),
                EntityId,
                RequestedVersion,
                CancellationToken.None)
            : await fetcher.FetchAsync(typeof(TestProjection), EntityId, CancellationToken.None);
        Assert.NotNull(result);
        TestProjection data = Assert.IsType<TestProjection>(result.Data);
        Assert.Equal("returned-data", data.Name);
        Assert.Equal(expectedVersion, result.Version);
        Assert.Equal(HttpMethod.Get, requestedMethod);
        string expectedPath = $"/api/projections/{ProjectionPath}/{EntityId}";
        if (exactVersion)
        {
            expectedPath += $"/at/{RequestedVersion}";
        }

        Assert.Equal(expectedPath, requestedPath);
        handler.Protected()
            .Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }
}