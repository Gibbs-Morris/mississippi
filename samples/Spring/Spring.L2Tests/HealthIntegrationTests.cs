using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;


namespace MississippiSamples.Spring.L2Tests;

/// <summary>
///     Integration tests covering the Spring gateway and runtime health endpoints.
/// </summary>
[Collection(SpringTestCollection.Name)]
public sealed class HealthIntegrationTests
{
    private static readonly Uri HealthEndpoint = new("/health", UriKind.Relative);

    private readonly SpringFixture fixture;

    /// <summary>
    ///     Initializes a new instance of the <see cref="HealthIntegrationTests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Spring fixture.</param>
    public HealthIntegrationTests(
        SpringFixture fixture
    ) =>
        this.fixture = fixture;

    /// <summary>
    ///     Verifies the gateway health endpoint reports healthy once saga access propagation is configured.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GatewayHealthEndpointShouldReportHealthy()
    {
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = fixture.CreateHttpClient();
        using HttpResponseMessage response = await client.GetAsync(HealthEndpoint);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement payload = (await response.Content.ReadFromJsonAsync<JsonElement>())!;
        payload.GetProperty("status").GetString().Should().Be("Healthy");
        payload.GetProperty("checks")
            .GetProperty("spring-saga-access")
            .GetProperty("status")
            .GetString()
            .Should()
            .Be("Healthy");
    }

    /// <summary>
    ///     Verifies the runtime health endpoint reports healthy once the reminder provider is configured.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task RuntimeHealthEndpointShouldReportHealthy()
    {
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = fixture.CreateRuntimeHttpClient();
        using HttpResponseMessage response = await client.GetAsync(HealthEndpoint);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement payload = (await response.Content.ReadFromJsonAsync<JsonElement>())!;
        payload.GetProperty("status").GetString().Should().Be("Healthy");
        payload.GetProperty("checks")
            .GetProperty("spring-saga-reminders")
            .GetProperty("status")
            .GetString()
            .Should()
            .Be("Healthy");
    }
}