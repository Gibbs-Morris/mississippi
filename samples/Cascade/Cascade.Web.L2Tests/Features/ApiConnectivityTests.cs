using Allure.Xunit.Attributes;

using Microsoft.Playwright;


namespace Cascade.Web.L2Tests.Features;

/// <summary>
///     Tests for API connectivity from the Blazor WASM client.
/// </summary>
[AllureParentSuite("Cascade.Web")]
[AllureSuite("Connectivity")]
[AllureSubSuite("API")]
#pragma warning disable CA1515 // Types can be made internal - xUnit test class must be public
public sealed class ApiConnectivityTests : TestBase
#pragma warning restore CA1515
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ApiConnectivityTests" /> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public ApiConnectivityTests(
        PlaywrightFixture fixture
    )
        : base(fixture)
    {
    }

    /// <summary>
    ///     Verifies the home page loads and displays the app title.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    [AllureId("web-api-001")]
    public async Task HomePageLoadsSuccessfully()
    {
        // Arrange
        IPage page = await CreatePageAsync();

        // Act
        await page.GotoAsync(
            Fixture.BaseUrl,
            new()
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 60000,
            });

        // Assert
        ILocator title = page.Locator("h1");
        await Assertions.Expect(title).ToContainTextAsync("Cascade Web");
    }

    /// <summary>
    ///     Verifies the API health endpoint returns healthy status.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    [AllureId("web-api-002")]
    public async Task CallApiReturnsHealthyStatus()
    {
        // Arrange
        IPage page = await CreatePageAsync();
        await page.GotoAsync(
            Fixture.BaseUrl,
            new()
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 60000,
            });

        // Act
        await page.ClickAsync("button:has-text('Call API')");

        // Wait for response
        await page.WaitForSelectorAsync("text=Healthy", new() { Timeout = 30000 });

        // Assert
        ILocator status = page.Locator("text=Healthy");
        await Assertions.Expect(status).ToBeVisibleAsync();
    }
}
