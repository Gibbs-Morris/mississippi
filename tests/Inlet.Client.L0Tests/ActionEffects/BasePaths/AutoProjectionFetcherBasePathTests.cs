using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.L0Tests.Helpers;

using Moq;
using Moq.Protected;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects.BasePaths;

/// <summary>
///     Verifies projection requests against configured HTTP base paths.
/// </summary>
public sealed class AutoProjectionFetcherBasePathTests
{
    /// <summary>
    ///     Default latest and versioned routes preserve the configured base.
    /// </summary>
    /// <param name="baseAddress">The HTTP base address.</param>
    /// <param name="atVersion">Whether to request a specific version.</param>
    /// <param name="expectedAddress">The expected request URI.</param>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Theory]
    [InlineData("https://example.test/", false, "https://example.test/api/projections/balance/account")]
    [InlineData("https://example.test/", true, "https://example.test/api/projections/balance/account/at/7")]
    [InlineData("https://example.test/bank/", false, "https://example.test/bank/api/projections/balance/account")]
    [InlineData("https://example.test/bank/", true, "https://example.test/bank/api/projections/balance/account/at/7")]
    public async Task DefaultRoutesPreserveHttpBasePathAsync(
        string baseAddress,
        bool atVersion,
        string expectedAddress
    )
    {
        Uri? requestUri = null;
        Mock<HttpMessageHandler> handler = new();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => requestUri = request.RequestUri)
            .ReturnsAsync(() => new(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(
                    new TestProjection
                    {
                        Name = "account",
                    }),
            });
        using HttpClient http = new(handler.Object)
        {
            BaseAddress = new(baseAddress),
        };
        ProjectionDtoRegistry registry = new();
        registry.Register("balance", typeof(TestProjection));
        AutoProjectionFetcher fetcher = new(http, registry);
        ProjectionFetchResult? result = atVersion
            ? await fetcher.FetchAtVersionAsync(
                typeof(TestProjection),
                "account",
                7,
                TestContext.Current.CancellationToken)
            : await fetcher.FetchAsync(typeof(TestProjection), "account", TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal("account", Assert.IsType<TestProjection>(result.Data).Name);
        Assert.Equal(new(expectedAddress), requestUri);
    }

    /// <summary>
    ///     Explicit relative, root-relative, and absolute routes retain their URI semantics.
    /// </summary>
    /// <param name="prefix">The explicit route prefix.</param>
    /// <param name="expectedAddress">The expected request URI.</param>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Theory]
    [InlineData("custom/projections", "https://example.test/bank/custom/projections/balance/account")]
    [InlineData("/api/projections", "https://example.test/api/projections/balance/account")]
    [InlineData("https://gateway.test/api/projections", "https://gateway.test/api/projections/balance/account")]
    public async Task ExplicitRoutesRetainUriSemanticsAsync(
        string prefix,
        string expectedAddress
    )
    {
        Uri? requestUri = null;
        Mock<HttpMessageHandler> handler = new();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => requestUri = request.RequestUri)
            .ReturnsAsync(() => new(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TestProjection()),
            });
        using HttpClient http = new(handler.Object)
        {
            BaseAddress = new("https://example.test/bank/"),
        };
        ProjectionDtoRegistry registry = new();
        registry.Register("balance", typeof(TestProjection));
        AutoProjectionFetcher fetcher = new(http, registry, prefix);
        await fetcher.FetchAsync(typeof(TestProjection), "account", TestContext.Current.CancellationToken);
        Assert.Equal(new(expectedAddress), requestUri);
    }
}