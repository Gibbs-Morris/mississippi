using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Projection.Abstractions;


namespace Mississippi.Inlet.Blazor.WebAssembly.L0Tests;

/// <summary>
///     Tests for <see cref="AutoProjectionFetcher" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Blazor.WebAssembly")]
[AllureSuite("Action Effects")]
[AllureSubSuite("AutoProjectionFetcher")]
public sealed class AutoProjectionFetcherTests : IDisposable
{
    private readonly MockHttpHandler handler = new();

    private readonly HttpClient httpClient;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AutoProjectionFetcherTests" /> class.
    /// </summary>
    public AutoProjectionFetcherTests()
    {
#pragma warning disable IDISP014 // Test fixture manages HttpClient with mock handler - not using IHttpClientFactory
#pragma warning disable CA2000 // HttpClient is stored and disposed in Dispose method
        httpClient = new(handler, false)
        {
            BaseAddress = new("http://localhost"),
        };
#pragma warning restore CA2000
#pragma warning restore IDISP014
    }

    /// <summary>
    ///     Releases resources used by the test.
    /// </summary>
    public void Dispose()
    {
        httpClient.Dispose();
        handler.Dispose();
    }

    private static ProjectionDtoRegistry CreateRegistry() => new();

    private static ProjectionDtoRegistry CreateRegistryWithTestProjection()
    {
        ProjectionDtoRegistry registry = new();
        registry.Register("test-projections", typeof(TestProjection));
        return registry;
    }

    /// <summary>
    ///     Test projection DTO for testing.
    /// </summary>
    [ProjectionPath("test-projections")]
    internal sealed record TestProjection(string Name, int Value);

    /// <summary>
    ///     Mock HTTP message handler for testing.
    /// </summary>
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "HttpResponseMessage lifetime is managed by HttpClient")]
    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, (HttpStatusCode StatusCode, string? Content, string? ETag)> responses =
            new();

        /// <summary>
        ///     Gets the last requested URL.
        /// </summary>
        public string? LastRequestedUrl { get; private set; }

        /// <summary>
        ///     Sets a response for a specific URL path.
        /// </summary>
        /// <param name="path">The URL path to respond to.</param>
        /// <param name="statusCode">The HTTP status code to return.</param>
        /// <param name="content">Optional content to serialize as JSON.</param>
        /// <param name="etag">Optional ETag header value.</param>
        public void SetResponse(
            string path,
            HttpStatusCode statusCode,
            object? content = null,
            string? etag = null
        )
        {
            string? json = content is not null ? JsonSerializer.Serialize(content) : null;
            responses[path] = (statusCode, json, etag);
        }

        /// <inheritdoc />
#pragma warning disable CS1998 // Async method lacks 'await' operators - intentional for ownership transfer pattern
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
#pragma warning restore CS1998
        {
            LastRequestedUrl = request.RequestUri?.PathAndQuery;
            string path = LastRequestedUrl ?? string.Empty;

            // Create response with appropriate status code - default to NotFound if not configured
            HttpStatusCode statusCode = HttpStatusCode.NotFound;
            string? content = null;
            string? etag = null;
            if (responses.TryGetValue(path, out (HttpStatusCode StatusCode, string? Content, string? ETag) response))
            {
                statusCode = response.StatusCode;
                content = response.Content;
                etag = response.ETag;
            }

            HttpResponseMessage httpResponse = new(statusCode);
            if (content is not null)
            {
                httpResponse.Content = new StringContent(content, Encoding.UTF8, "application/json");
            }

            if (etag is not null)
            {
                httpResponse.Headers.ETag = new(etag);
            }

            return httpResponse;
        }
    }

    /// <summary>
    ///     Constructor should throw when httpClient is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void ConstructorThrowsWhenHttpClientIsNull()
    {
        // Arrange
        HttpClient nullClient = null!;
        ProjectionDtoRegistry registry = CreateRegistry();

        // Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => new AutoProjectionFetcher(nullClient, registry));
        Assert.Equal("httpClient", exception.ParamName);
    }

    /// <summary>
    ///     Constructor should throw when registry is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void ConstructorThrowsWhenRegistryIsNull()
    {
        // Arrange
        IProjectionDtoRegistry registry = null!;

        // Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => new AutoProjectionFetcher(httpClient, registry));
        Assert.Equal("registry", exception.ParamName);
    }

    /// <summary>
    ///     FetchAsync should return NotFound sentinel when server returns NotFound.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Fetch")]
    public async Task FetchAsyncReturnsNotFoundWhenServerReturnsNotFoundAsync()
    {
        // Arrange
        handler.SetResponse("/api/projections/test-projections/entity-123", HttpStatusCode.NotFound);
        ProjectionDtoRegistry registry = CreateRegistryWithTestProjection();
        AutoProjectionFetcher fetcher = new(httpClient, registry);

        // Act
        ProjectionFetchResult? result = await fetcher.FetchAsync(
            typeof(TestProjection),
            "entity-123",
            CancellationToken.None);

        // Assert - should return NotFound sentinel (not null)
        Assert.NotNull(result);
        Assert.True(result.IsNotFound);
        Assert.Null(result.Data);
        Assert.Equal(0, result.Version);
    }

    /// <summary>
    ///     FetchAsync should return null when projection type is not registered.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Fetch")]
    public async Task FetchAsyncReturnsNullWhenProjectionTypeNotRegisteredAsync()
    {
        // Arrange
        ProjectionDtoRegistry registry = CreateRegistry();
        AutoProjectionFetcher fetcher = new(httpClient, registry);

        // Act
        ProjectionFetchResult? result = await fetcher.FetchAsync(
            typeof(TestProjection),
            "entity-123",
            CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     FetchAsync should return projection data with version from ETag.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Fetch")]
    public async Task FetchAsyncReturnsProjectionWithVersionFromETagAsync()
    {
        // Arrange
        TestProjection expected = new("Test Name", 42);
        handler.SetResponse("/api/projections/test-projections/entity-123", HttpStatusCode.OK, expected, "\"5\"");
        ProjectionDtoRegistry registry = CreateRegistryWithTestProjection();
        AutoProjectionFetcher fetcher = new(httpClient, registry);

        // Act
        ProjectionFetchResult? result = await fetcher.FetchAsync(
            typeof(TestProjection),
            "entity-123",
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Version);
        TestProjection actual = Assert.IsType<TestProjection>(result.Data);
        Assert.Equal("Test Name", actual.Name);
        Assert.Equal(42, actual.Value);
    }

    /// <summary>
    ///     FetchAsync should throw when entityId is empty or whitespace.
    /// </summary>
    /// <param name="entityId">The entity ID to test.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [AllureFeature("Argument Validation")]
    public async Task FetchAsyncThrowsWhenEntityIdIsEmptyOrWhitespaceAsync(
        string entityId
    )
    {
        // Arrange
        ProjectionDtoRegistry registry = CreateRegistry();
        AutoProjectionFetcher fetcher = new(httpClient, registry);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => fetcher.FetchAsync(
            typeof(TestProjection),
            entityId,
            CancellationToken.None));
    }

    /// <summary>
    ///     FetchAsync should throw when entityId is null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Argument Validation")]
    public async Task FetchAsyncThrowsWhenEntityIdIsNullAsync()
    {
        // Arrange
        ProjectionDtoRegistry registry = CreateRegistry();
        AutoProjectionFetcher fetcher = new(httpClient, registry);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => fetcher.FetchAsync(
            typeof(TestProjection),
            null!,
            CancellationToken.None));
    }

    /// <summary>
    ///     FetchAsync should throw when projectionType is null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Argument Validation")]
    public async Task FetchAsyncThrowsWhenProjectionTypeIsNullAsync()
    {
        // Arrange
        ProjectionDtoRegistry registry = CreateRegistry();
        AutoProjectionFetcher fetcher = new(httpClient, registry);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => fetcher.FetchAsync(
            null!,
            "entity-123",
            CancellationToken.None));
    }

    /// <summary>
    ///     FetchAtVersionAsync should construct versioned URL and return projection data.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Fetch At Version")]
    public async Task FetchAtVersionAsyncConstructsVersionedUrlAsync()
    {
        // Arrange
        TestProjection expected = new("Versioned Data", 100);
        handler.SetResponse(
            "/api/projections/test-projections/entity-123/at/42",
            HttpStatusCode.OK,
            expected,
            "\"42\"");
        ProjectionDtoRegistry registry = CreateRegistryWithTestProjection();
        AutoProjectionFetcher fetcher = new(httpClient, registry);

        // Act
        ProjectionFetchResult? result = await fetcher.FetchAtVersionAsync(
            typeof(TestProjection),
            "entity-123",
            42,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Version);
        TestProjection actual = Assert.IsType<TestProjection>(result.Data);
        Assert.Equal("Versioned Data", actual.Name);
        Assert.Equal(100, actual.Value);

        // Verify the correct URL was called
        Assert.Equal("/api/projections/test-projections/entity-123/at/42", handler.LastRequestedUrl);
    }

    /// <summary>
    ///     FetchAtVersionAsync should properly escape entityId in URL.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Fetch At Version")]
    public async Task FetchAtVersionAsyncEscapesEntityIdInUrlAsync()
    {
        // Arrange
        TestProjection expected = new("Escaped", 1);

        // Entity ID with special characters
        handler.SetResponse(
            "/api/projections/test-projections/entity%20with%20spaces/at/5",
            HttpStatusCode.OK,
            expected,
            "\"5\"");
        ProjectionDtoRegistry registry = CreateRegistryWithTestProjection();
        AutoProjectionFetcher fetcher = new(httpClient, registry);

        // Act
        ProjectionFetchResult? result = await fetcher.FetchAtVersionAsync(
            typeof(TestProjection),
            "entity with spaces",
            5,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("entity%20with%20spaces", handler.LastRequestedUrl, StringComparison.Ordinal);
    }

    /// <summary>
    ///     FetchAtVersionAsync should return NotFound sentinel when version not found.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Fetch At Version")]
    public async Task FetchAtVersionAsyncReturnsNotFoundWhenVersionNotFoundAsync()
    {
        // Arrange
        handler.SetResponse("/api/projections/test-projections/entity-123/at/999", HttpStatusCode.NotFound);
        ProjectionDtoRegistry registry = CreateRegistryWithTestProjection();
        AutoProjectionFetcher fetcher = new(httpClient, registry);

        // Act
        ProjectionFetchResult? result = await fetcher.FetchAtVersionAsync(
            typeof(TestProjection),
            "entity-123",
            999,
            CancellationToken.None);

        // Assert - should return NotFound sentinel (not null)
        Assert.NotNull(result);
        Assert.True(result.IsNotFound);
        Assert.Null(result.Data);
        Assert.Equal(0, result.Version);
    }

    /// <summary>
    ///     FetchAtVersionAsync should return null when projection type is not registered.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Fetch At Version")]
    public async Task FetchAtVersionAsyncReturnsNullWhenProjectionTypeNotRegisteredAsync()
    {
        // Arrange
        ProjectionDtoRegistry registry = CreateRegistry();
        AutoProjectionFetcher fetcher = new(httpClient, registry);

        // Act
        ProjectionFetchResult? result = await fetcher.FetchAtVersionAsync(
            typeof(TestProjection),
            "entity-123",
            5,
            CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     FetchAtVersionAsync should throw when entityId is empty or whitespace.
    /// </summary>
    /// <param name="entityId">The entity ID to test.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [AllureFeature("Argument Validation")]
    public async Task FetchAtVersionAsyncThrowsWhenEntityIdIsEmptyOrWhitespaceAsync(
        string entityId
    )
    {
        // Arrange
        ProjectionDtoRegistry registry = CreateRegistry();
        AutoProjectionFetcher fetcher = new(httpClient, registry);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => fetcher.FetchAtVersionAsync(
            typeof(TestProjection),
            entityId,
            5,
            CancellationToken.None));
    }

    /// <summary>
    ///     FetchAtVersionAsync should throw when entityId is null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Argument Validation")]
    public async Task FetchAtVersionAsyncThrowsWhenEntityIdIsNullAsync()
    {
        // Arrange
        ProjectionDtoRegistry registry = CreateRegistry();
        AutoProjectionFetcher fetcher = new(httpClient, registry);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            fetcher.FetchAtVersionAsync(typeof(TestProjection), null!, 5, CancellationToken.None));
    }

    /// <summary>
    ///     FetchAtVersionAsync should throw when projectionType is null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Argument Validation")]
    public async Task FetchAtVersionAsyncThrowsWhenProjectionTypeIsNullAsync()
    {
        // Arrange
        ProjectionDtoRegistry registry = CreateRegistry();
        AutoProjectionFetcher fetcher = new(httpClient, registry);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            fetcher.FetchAtVersionAsync(null!, "entity-123", 5, CancellationToken.None));
    }

    /// <summary>
    ///     FetchAtVersionAsync should use custom route prefix.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Fetch At Version")]
    public async Task FetchAtVersionAsyncUsesCustomRoutePrefixAsync()
    {
        // Arrange
        TestProjection expected = new("Custom Prefix", 200);
        handler.SetResponse("/custom/prefix/test-projections/entity-456/at/10", HttpStatusCode.OK, expected, "\"10\"");
        ProjectionDtoRegistry registry = CreateRegistryWithTestProjection();
        AutoProjectionFetcher fetcher = new(httpClient, registry, "/custom/prefix");

        // Act
        ProjectionFetchResult? result = await fetcher.FetchAtVersionAsync(
            typeof(TestProjection),
            "entity-456",
            10,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/custom/prefix/test-projections/entity-456/at/10", handler.LastRequestedUrl);
    }
}