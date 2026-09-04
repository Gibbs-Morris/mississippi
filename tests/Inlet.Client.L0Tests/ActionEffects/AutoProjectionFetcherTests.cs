using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.L0Tests.Helpers;

using Moq;
using Moq.Protected;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects;

/// <summary>
///     Verifies HTTP projection routes, version metadata, and empty/error response semantics.
/// </summary>
public sealed class AutoProjectionFetcherTests
{
    /// <summary>
    ///     Caller cancellation is propagated through HTTP without being converted to missing projection data.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CancelledRequestPropagatesCancellation()
    {
        using CancellationTokenSource cancellation = new();
        Mock<HttpMessageHandler> handler = new();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (_, token) =>
            {
                await cancellation.CancelAsync();
                return await Task.FromCanceled<HttpResponseMessage>(token);
            });
        using HttpClient client = new(handler.Object)
        {
            BaseAddress = new("https://localhost"),
        };
        ProjectionDtoRegistry registry = new();
        registry.Register("accounts", typeof(TestProjection));
        AutoProjectionFetcher fetcher = new(client, registry);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fetcher.FetchAsync(
            typeof(TestProjection),
            "account-1",
            cancellation.Token));
    }

    /// <summary>
    ///     Server failures retain the HTTP status for effect-level error handling.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task FailedResponseThrowsWithStatusCode()
    {
        using HttpResponseMessage response = new(HttpStatusCode.ServiceUnavailable);
        Mock<HttpMessageHandler> handler = new();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        using HttpClient client = new(handler.Object)
        {
            BaseAddress = new("https://localhost"),
        };
        ProjectionDtoRegistry registry = new();
        registry.Register("accounts", typeof(TestProjection));
        AutoProjectionFetcher fetcher = new(client, registry);
        HttpRequestException failure = await Assert.ThrowsAsync<HttpRequestException>(() =>
            fetcher.FetchAsync(typeof(TestProjection), "account-1", CancellationToken.None));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failure.StatusCode);
    }

    /// <summary>
    ///     Latest and immutable-version requests escape entity IDs and deserialize the registered DTO.
    /// </summary>
    /// <param name="versioned">Whether to request an immutable projection version.</param>
    /// <param name="expectedPath">The expected escaped HTTP request path.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false, "/api/projections/accounts/account%2Fone%20two")]
    [InlineData(true, "/custom/accounts/account%2Fone%20two/at/7")]
    public async Task FetchUsesEscapedRouteAndResponseVersion(
        bool versioned,
        string expectedPath
    )
    {
        using HttpResponseMessage response = new(HttpStatusCode.OK);
        response.Content = new StringContent("{\"name\":\"account projection\"}");
        response.Headers.ETag = new("\"42\"");
        Mock<HttpMessageHandler> handler = new();
        string? requestedPath = null;
        HttpMethod? requestedMethod = null;
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
        using HttpClient client = new(handler.Object)
        {
            BaseAddress = new("https://localhost"),
        };
        ProjectionDtoRegistry registry = new();
        registry.Register("accounts", typeof(TestProjection));
        AutoProjectionFetcher fetcher = new(client, registry, versioned ? "/custom" : null);
        ProjectionFetchResult? result = versioned
            ? await fetcher.FetchAtVersionAsync(typeof(TestProjection), "account/one two", 7, CancellationToken.None)
            : await fetcher.FetchAsync(typeof(TestProjection), "account/one two", CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(expectedPath, requestedPath);
        Assert.Equal(HttpMethod.Get, requestedMethod);
        Assert.Equal("account projection", Assert.IsType<TestProjection>(result.Data).Name);
        Assert.Equal(42, result.Version);
        Assert.False(result.IsNotFound);
    }

    /// <summary>
    ///     A missing or nonnumeric ETag preserves data with an unknown version.
    /// </summary>
    /// <param name="etag">The optional entity tag returned by the server.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData("\"not-a-version\"")]
    public async Task MissingOrInvalidEtagProducesVersionZero(
        string? etag
    )
    {
        using HttpResponseMessage response = new(HttpStatusCode.OK);
        response.Content = new StringContent("{\"name\":\"unversioned\"}");
        response.Headers.ETag = etag is null ? null : new EntityTagHeaderValue(etag);
        Mock<HttpMessageHandler> handler = new();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        using HttpClient client = new(handler.Object)
        {
            BaseAddress = new("https://localhost"),
        };
        ProjectionDtoRegistry registry = new();
        registry.Register("accounts", typeof(TestProjection));
        AutoProjectionFetcher fetcher = new(client, registry);
        ProjectionFetchResult? result = await fetcher.FetchAsync(
            typeof(TestProjection),
            "account-1",
            CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal("unversioned", Assert.IsType<TestProjection>(result.Data).Name);
        Assert.Equal(0, result.Version);
    }

    /// <summary>
    ///     Not-found responses are valid empty projections for both route variants.
    /// </summary>
    /// <param name="versioned">Whether to request an immutable projection version.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NotFoundResponseProducesSentinel(
        bool versioned
    )
    {
        using HttpResponseMessage response = new(HttpStatusCode.NotFound);
        Mock<HttpMessageHandler> handler = new();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        using HttpClient client = new(handler.Object)
        {
            BaseAddress = new("https://localhost"),
        };
        ProjectionDtoRegistry registry = new();
        registry.Register("accounts", typeof(TestProjection));
        AutoProjectionFetcher fetcher = new(client, registry);
        ProjectionFetchResult? result = versioned
            ? await fetcher.FetchAtVersionAsync(typeof(TestProjection), "account-1", 7, CancellationToken.None)
            : await fetcher.FetchAsync(typeof(TestProjection), "account-1", CancellationToken.None);
        Assert.Same(ProjectionFetchResult.NotFound, result);
    }

    /// <summary>
    ///     A JSON null response is unsupported data rather than a not-found sentinel.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task NullJsonPayloadReturnsNull()
    {
        using HttpResponseMessage response = new(HttpStatusCode.OK);
        response.Content = new StringContent("null");
        Mock<HttpMessageHandler> handler = new();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        using HttpClient client = new(handler.Object)
        {
            BaseAddress = new("https://localhost"),
        };
        ProjectionDtoRegistry registry = new();
        registry.Register("accounts", typeof(TestProjection));
        AutoProjectionFetcher fetcher = new(client, registry);
        Assert.Null(await fetcher.FetchAsync(typeof(TestProjection), "account-1", CancellationToken.None));
    }

    /// <summary>
    ///     Unsupported DTO types do not issue network requests.
    /// </summary>
    /// <param name="versioned">Whether to request an immutable projection version.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnregisteredProjectionDoesNotSendRequest(
        bool versioned
    )
    {
        Mock<HttpMessageHandler> handler = new(MockBehavior.Strict);
        using HttpClient client = new(handler.Object, false);
        AutoProjectionFetcher fetcher = new(client, new ProjectionDtoRegistry());
        ProjectionFetchResult? result = versioned
            ? await fetcher.FetchAtVersionAsync(typeof(TestProjection), "account-1", 7, CancellationToken.None)
            : await fetcher.FetchAsync(typeof(TestProjection), "account-1", CancellationToken.None);
        Assert.Null(result);
        handler.Protected()
            .Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }
}