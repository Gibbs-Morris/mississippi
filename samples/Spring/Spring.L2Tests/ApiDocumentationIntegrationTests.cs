using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;


namespace MississippiSamples.Spring.L2Tests;

/// <summary>
///     Verifies the generated OpenAPI document through the gateway HTTP contract.
/// </summary>
[Collection(SpringApiCollectionDefinition.Name)]
public sealed class ApiDocumentationIntegrationTests
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ApiDocumentationIntegrationTests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Spring fixture.</param>
    public ApiDocumentationIntegrationTests(
        SpringApplicationFixture fixture
    ) =>
        Fixture = fixture;

    private SpringApplicationFixture Fixture { get; }

    /// <summary>
    ///     Verifies that OpenAPI generation includes the configured metadata and API paths.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task OpenApiDocumentShouldContainGeneratedEndpoints()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        HttpClient client = Fixture.GatewayClient;
        using HttpResponseMessage response = await client.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative),
            timeout.Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync(timeout.Token);
        using JsonDocument document = JsonDocument.Parse(content);
        Assert.Equal("Spring Bank API", document.RootElement.GetProperty("info").GetProperty("title").GetString());
        Assert.Equal("v1", document.RootElement.GetProperty("info").GetProperty("version").GetString());
        JsonElement paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/aggregates/bank-account/{entityId}/open", out JsonElement openEndpoint));
        Assert.True(openEndpoint.TryGetProperty("post", out JsonElement _));
    }
}