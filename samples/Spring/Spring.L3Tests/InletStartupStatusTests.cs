using System.IO;
using System.Text.Json;

using MississippiSamples.Spring.L3Tests.Pages;

using static Microsoft.Playwright.Assertions;


namespace MississippiSamples.Spring.L3Tests;

/// <summary>
///     Verifies that a failed initial negotiation remains visible and can be retried.
/// </summary>
[Collection(SpringBrowserCollectionDefinition.Name)]
public sealed class InletStartupStatusTests
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="InletStartupStatusTests" /> class.
    /// </summary>
    /// <param name="fixture">The shared browser and Spring application fixture.</param>
    public InletStartupStatusTests(
        SpringBrowserFixture fixture
    ) =>
        Fixture = fixture;

    private SpringBrowserFixture Fixture { get; }

    private static async Task SaveStateEvidenceAsync(
        IPage page,
        string state
    )
    {
        string? directory = Environment.GetEnvironmentVariable("SPRING_TEST_ARTIFACTS");
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
        await page.SetViewportSizeAsync(1440, 900);
        await page.ScreenshotAsync(new() { Path = Path.Join(directory, $"inlet-startup-{state}-desktop.png"), FullPage = true });
        await page.SetViewportSizeAsync(390, 844);
        await page.ScreenshotAsync(new() { Path = Path.Join(directory, $"inlet-startup-{state}-mobile.png"), FullPage = true });
        await File.WriteAllTextAsync(
            Path.Join(directory, $"inlet-startup-{state}.json"),
            JsonSerializer.Serialize(new
            {
                Route = page.Url,
                State = state,
                FailedNegotiationStatus = 503,
                Browser = "Chromium",
                BrowserVersion = page.Context.Browser?.Version,
                Desktop = new { Width = 1440, Height = 900 },
                Mobile = new { Width = 390, Height = 844 },
            }),
            TestContext.Current.CancellationToken);
        await page.SetViewportSizeAsync(1440, 900);
    }

    /// <summary>
    ///     A 503 startup failure shows Disconnected and the real error before a manual retry connects.
    /// </summary>
    /// <returns>A task representing the browser journey.</returns>
    [Fact]
    public async Task FailedInitialNegotiationShowsDisconnectedAndAllowsRetry()
    {
        Assert.True(Fixture.IsInitialized, "The Spring application and browser must be initialized.");
        IPage page = await Fixture.CreatePageAsync();
        await page.Context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });
        try
        {
            const string negotiationPattern = "**/hubs/inlet/negotiate**";
            await page.RouteAsync(
                negotiationPattern,
                route => route.FulfillAsync(new() { Status = 503, ContentType = "text/plain", Body = "Controlled initial negotiation failure" }));
            AccountsPage accounts = new(page);
            await accounts.NavigateAsync(Fixture.GatewayBaseUri);
            await accounts.WaitForConnectionStatusAsync("Disconnected", 120_000);
            ILocator lostConnection = page.GetByRole(AriaRole.Dialog).Filter(new() { Has = page.GetByRole(AriaRole.Heading, new() { Name = "Connection Lost", Exact = true }) });
            await Expect(lostConnection).ToBeVisibleAsync();
            await Expect(lostConnection).ToContainTextAsync("503");
            await SaveStateEvidenceAsync(page, "disconnected");

            await page.UnrouteAsync(negotiationPattern);
            await lostConnection.GetByRole(AriaRole.Button, new() { Name = "Reconnect", Exact = true }).ClickAsync();
            await accounts.WaitForConnectionStatusAsync("Connected", 120_000);
            await Expect(lostConnection).ToHaveCountAsync(0);
            await SaveStateEvidenceAsync(page, "connected");
        }
        finally
        {
            await SpringBrowserFixture.SaveBrowserArtifactsAsync(page);
        }
    }
}
