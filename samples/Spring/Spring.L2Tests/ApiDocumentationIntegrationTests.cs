using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;


namespace MississippiSamples.Spring.L2Tests;

/// <summary>
///     Verifies that the gateway serves its generated API document and reference UI.
/// </summary>
[Collection(SpringTestCollection.Name)]
public sealed class ApiDocumentationIntegrationTests
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ApiDocumentationIntegrationTests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Spring fixture.</param>
    public ApiDocumentationIntegrationTests(
        SpringFixture fixture
    ) =>
        Fixture = fixture;

    private SpringFixture Fixture { get; }

    /// <summary>
    ///     Verifies that OpenAPI generation includes the configured metadata and API paths.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task OpenApiDocumentShouldContainGeneratedEndpoints()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        HttpClient client = Fixture.CreateHttpClient();
        using HttpResponseMessage response = await client.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative),
            timeout.Token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string content = await response.Content.ReadAsStringAsync(timeout.Token);
        using JsonDocument document = JsonDocument.Parse(content);
        document.RootElement.GetProperty("info").GetProperty("title").GetString().Should().Be("Spring Bank API");
        document.RootElement.GetProperty("info").GetProperty("version").GetString().Should().Be("v1");
        JsonElement paths = document.RootElement.GetProperty("paths");
        paths.TryGetProperty("/api/aggregates/bank-account/{entityId}/open", out JsonElement openEndpoint)
            .Should()
            .BeTrue();
        openEndpoint.TryGetProperty("post", out var _).Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that Scalar loads the generated OpenAPI document in the browser.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task ScalarReferenceShouldLoadTheApiDocument()
    {
        IPage page = await Fixture.CreatePageAsync();
        try
        {
            IResponse response = await page.RunAndWaitForResponseAsync(
                () => page.GotoAsync(new Uri(Fixture.GatewayBaseUri, "/scalar/v1").AbsoluteUri),
                candidate => new Uri(candidate.Url).AbsolutePath == "/openapi/v1.json");
            Assert.Equal(200, response.Status);
            Assert.Equal("Spring Bank API", await page.TitleAsync());
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}