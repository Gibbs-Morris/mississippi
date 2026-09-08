namespace MississippiSamples.Spring.L3Tests;

/// <summary>Verifies the API documentation through its real browser interface.</summary>
[Collection(SpringBrowserCollectionDefinition.Name)]
public sealed class ApiDocumentationE2ETests
{
    /// <summary>Initializes a new instance of the <see cref="ApiDocumentationE2ETests" /> class.</summary>
    /// <param name="fixture">The shared L3 browser fixture.</param>
    public ApiDocumentationE2ETests(
        SpringBrowserFixture fixture
    ) =>
        Fixture = fixture;

    private SpringBrowserFixture Fixture { get; }

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
                candidate => Uri.TryCreate(candidate.Url, UriKind.Absolute, out Uri? responseUri) &&
                             (responseUri.AbsolutePath == "/openapi/v1.json"));
            response.Status.Should().Be(200);
            string title = await page.TitleAsync();
            title.Should().Be("Spring Bank API");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}