using System.Globalization;

using static Microsoft.Playwright.Assertions;


namespace MississippiSamples.Spring.L3Tests;

/// <summary>Verifies task navigation by touch and records the rendered phone and desktop routes.</summary>
/// <remarks>The type is public so xUnit can discover and execute these L3 cases.</remarks>
[Collection(SpringBrowserCollectionDefinition.Name)]
public sealed class FlagshipRouteEvidenceTests
{
    /// <summary>Initializes a new instance of the <see cref="FlagshipRouteEvidenceTests" /> class.</summary>
    /// <param name="fixture">The shared Spring browser fixture.</param>
    public FlagshipRouteEvidenceTests(
        SpringBrowserFixture fixture
    ) =>
        Fixture = fixture;

    private SpringBrowserFixture Fixture { get; }

    /// <summary>Verify that each task can be reached by touch, identifies the active route and focuses its heading.</summary>
    /// <param name="width">The desktop or phone viewport width.</param>
    /// <param name="height">The corresponding viewport height.</param>
    /// <returns>The asynchronous real-app navigation and screenshot journey.</returns>
    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    [InlineData(320, 740)]
    public async Task TaskNavigationFocusAndLayoutRemainUsableAsync(
        int width,
        int height
    )
    {
        Assert.True(Fixture.IsInitialized, "fixture must be initialized");
        IPage page = await Fixture.CreatePageAsync(
            new()
            {
                HasTouch = true,
                ViewportSize = new()
                {
                    Width = width,
                    Height = height,
                },
            });
        try
        {
            await page.GotoAsync(new Uri(Fixture.GatewayBaseUri, "/auth-proof").ToString());
            foreach ((string route, string label, string artifact) in new[]
                     {
                         ("/", "Start here", "home"),
                         ("/accounts", "Prepare accounts", "accounts"),
                         ("/operations", "Move money", "money"),
                         ("/investigations", "Investigate", "investigations"),
                         ("/auth-proof", "Test access", "access"),
                     })
            {
                ILocator navigation = page.GetByRole(
                    AriaRole.Navigation,
                    new()
                    {
                        Name = "Main navigation",
                        Exact = true,
                    });
                ILocator task = navigation.GetByRole(
                    AriaRole.Link,
                    new()
                    {
                        Name = label,
                        Exact = true,
                    });
                await task.TapAsync();
                await Expect(page.Locator("h1")).ToBeVisibleAsync();
                await Expect(page.Locator("h1")).ToBeFocusedAsync();
                await Expect(task).ToHaveAttributeAsync("class", "active");
                Assert.Equal(route, new Uri(page.Url).AbsolutePath);
                Assert.True(
                    await page.EvaluateAsync<bool>(
                        "document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
                if (route == "/")
                {
                    await Expect(
                            page.GetByRole(
                                AriaRole.Link,
                                new()
                                {
                                    Name = "Start with two accounts",
                                    Exact = true,
                                }))
                        .ToHaveAttributeAsync("href", "/accounts");
                }

                await SpringScreenshotEvidence.SaveAsync(
                    page,
                    artifact + "-" + width.ToString(CultureInfo.InvariantCulture));
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}